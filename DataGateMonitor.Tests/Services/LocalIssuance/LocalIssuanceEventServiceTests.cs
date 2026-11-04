using DataGateMonitor.SharedModels.DataGateMonitor.LocalIssuance.Requests;
using System.Security.Cryptography;
using System.Text;
using DataGateMonitor.Models;
using DataGateMonitor.Services.LocalIssuance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace DataGateMonitor.Tests.Services.LocalIssuance;

public class LocalIssuanceEventServiceTests
{
    private static LocalIssuanceEventService CreateSut(DataGateMonitor.DataBase.Contexts.ApplicationDbContext db) =>
        new(db, TimeProvider.System, NullLogger<LocalIssuanceEventService>.Instance);

    private static LocalIssuanceEventRequest Parse(byte[] body) =>
        System.Text.Json.JsonSerializer.Deserialize<LocalIssuanceEventRequest>(body,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    [Theory]
    [InlineData("10.50.28.201")]
    [InlineData("retired-field-is-ignored")]
    public async Task OldSignedOutboxEvent_WithStaticIp_IsAccepted(string oldStaticIp)
    {
        using var node = new FakeNode();
        await using var db = LocalIssuanceDb.Create();
        LocalIssuanceDb.AddServer(db, 7, node);
        var body = FakeNode.EventBody(staticIp: oldStaticIp);
        var (timestamp, signature) = node.Sign(body, DateTimeOffset.UtcNow);
        var sut = CreateSut(db);
        var server = await sut.AuthenticateAsync(node.Fingerprint, timestamp, signature,
            "POST", LocalIssuanceEventService.SignedPath, body, default);
        Assert.NotNull(server);
        var (outcome, _) = await sut.ApplyAsync(server!, Parse(body), default);
        Assert.Equal(LocalIssuanceEventOutcome.Created, outcome);
        Assert.True(Assert.Single(db.IssuedOvpnFiles).IsServiceProfile);
    }

    [Fact]
    public void CanonicalBytes_MatchNodeFormat()
    {
        // Same vector is asserted in DataGateOpenVpnManager.Tests (NodeRequestSignatureTests).
        var canonical = Encoding.UTF8.GetString(
            NodeRequestSignature.CanonicalBytes(1700000000, "post", "/api/open-vpn-servers/local-issuance-events", "{}"u8.ToArray()));

        Assert.Equal("DGN1\n1700000000\nPOST\n/api/open-vpn-servers/local-issuance-events\n"
                     + "44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a", canonical);
    }

    [Fact]
    public async Task Authenticate_ValidSignatureFromTrustedKey_ReturnsServer()
    {
        using var node = new FakeNode();
        await using var db = LocalIssuanceDb.Create();
        LocalIssuanceDb.AddServer(db, 7, node);
        var body = FakeNode.EventBody();
        var (ts, sig) = node.Sign(body, DateTimeOffset.UtcNow);

        var server = await CreateSut(db).AuthenticateAsync(node.Fingerprint, ts, sig, "POST", LocalIssuanceEventService.SignedPath, body, default);

        Assert.Equal(7, server?.Id);
    }

    [Fact]
    public async Task Authenticate_RejectsUnknownKey_TamperedBody_WrongPath_StaleTimestamp_MissingHeaders()
    {
        using var trusted = new FakeNode();
        using var stranger = new FakeNode();
        await using var db = LocalIssuanceDb.Create();
        LocalIssuanceDb.AddServer(db, 7, trusted);
        var sut = CreateSut(db);
        var body = FakeNode.EventBody();
        const string path = LocalIssuanceEventService.SignedPath;

        var (ts, sig) = stranger.Sign(body, DateTimeOffset.UtcNow);
        Assert.Null(await sut.AuthenticateAsync(stranger.Fingerprint, ts, sig, "POST", path, body, default));
        // Valid key id, but signed by somebody else.
        Assert.Null(await sut.AuthenticateAsync(trusted.Fingerprint, ts, sig, "POST", path, body, default));

        (ts, sig) = trusted.Sign(body, DateTimeOffset.UtcNow);
        var tampered = FakeNode.EventBody(serial: "BADBAD");
        Assert.Null(await sut.AuthenticateAsync(trusted.Fingerprint, ts, sig, "POST", path, tampered, default));
        Assert.Null(await sut.AuthenticateAsync(trusted.Fingerprint, ts, sig, "POST", "/api/other", body, default));

        (ts, sig) = trusted.Sign(body, DateTimeOffset.UtcNow.AddMinutes(-30));
        Assert.Null(await sut.AuthenticateAsync(trusted.Fingerprint, ts, sig, "POST", path, body, default));

        Assert.Null(await sut.AuthenticateAsync(null, null, null, "POST", path, body, default));
        Assert.Null(await sut.AuthenticateAsync(trusted.Fingerprint, "", "", "POST", path, body, default));
    }

    [Fact]
    public async Task Authenticate_DeletedServerOrUntrustedPendingKey_IsRejected()
    {
        using var node = new FakeNode();
        await using var db = LocalIssuanceDb.Create();
        var server = LocalIssuanceDb.AddServer(db, 7);
        server.NodePendingPublicKeyPem = node.PublicKeyPem;
        server.NodePendingKeyFingerprint = node.Fingerprint;
        await db.SaveChangesAsync();
        var body = FakeNode.EventBody();
        var (ts, sig) = node.Sign(body, DateTimeOffset.UtcNow);

        Assert.Null(await CreateSut(db).AuthenticateAsync(node.Fingerprint, ts, sig, "POST", LocalIssuanceEventService.SignedPath, body, default));
    }

    [Fact]
    public async Task Apply_Issued_CreatesLocalServiceRow_ThenRepeatIsIdempotent()
    {
        await using var db = LocalIssuanceDb.Create();
        var server = LocalIssuanceDb.AddServer(db, 7);
        var sut = CreateSut(db);
        var evt = Parse(FakeNode.EventBody());

        var (first, id) = await sut.ApplyAsync(server, evt, default);
        var (second, sameId) = await sut.ApplyAsync(server, Parse(FakeNode.EventBody()), default);

        Assert.Equal((LocalIssuanceEventOutcome.Created, LocalIssuanceEventOutcome.Duplicate), (first, second));
        Assert.Equal(id, sameId);
        var row = Assert.Single(db.IssuedOvpnFiles);
        Assert.Equal((IssuedOvpnFileOrigin.Local, true, "5E71A1", false),
            (row.Origin, row.IsServiceProfile, row.CertSerialNumber, row.IsRevoked));
        Assert.Equal(("db1", "db1.ovpn", "/openvpn-udp/easy-rsa/pki/ovpn_files/db1.ovpn", LocalIssuanceEventService.LocalExternalId),
            (row.CommonName, row.FileName, row.FilePath, row.ExternalId));
    }

    [Fact]
    public async Task Apply_Revoked_MarksRow_AndRepeatIsIdempotent()
    {
        await using var db = LocalIssuanceDb.Create();
        var server = LocalIssuanceDb.AddServer(db, 7);
        var sut = CreateSut(db);
        await sut.ApplyAsync(server, Parse(FakeNode.EventBody()), default);

        var (revoked, _) = await sut.ApplyAsync(server, Parse(FakeNode.EventBody(type: "revoked")), default);
        var (again, _) = await sut.ApplyAsync(server, Parse(FakeNode.EventBody(type: "revoked")), default);

        Assert.Equal((LocalIssuanceEventOutcome.Revoked, LocalIssuanceEventOutcome.AlreadyRevoked), (revoked, again));
        Assert.True(Assert.Single(db.IssuedOvpnFiles).IsRevoked);
    }

    [Fact]
    public async Task Apply_RevokeBeforeIssue_KeepsRevokedRecord_LateIssueIsDuplicate()
    {
        await using var db = LocalIssuanceDb.Create();
        var server = LocalIssuanceDb.AddServer(db, 7);
        var sut = CreateSut(db);

        await sut.ApplyAsync(server, Parse(FakeNode.EventBody(type: "revoked")), default);
        var (late, _) = await sut.ApplyAsync(server, Parse(FakeNode.EventBody()), default);

        Assert.Equal(LocalIssuanceEventOutcome.Duplicate, late);
        Assert.True(Assert.Single(db.IssuedOvpnFiles).IsRevoked);
    }

    [Fact]
    public async Task Apply_SameSerialOnAnotherServer_IsSeparateRecord()
    {
        await using var db = LocalIssuanceDb.Create();
        var sut = CreateSut(db);

        await sut.ApplyAsync(LocalIssuanceDb.AddServer(db, 7), Parse(FakeNode.EventBody()), default);
        var (outcome, _) = await sut.ApplyAsync(LocalIssuanceDb.AddServer(db, 8), Parse(FakeNode.EventBody()), default);

        Assert.Equal(LocalIssuanceEventOutcome.Created, outcome);
        Assert.Equal(2, db.IssuedOvpnFiles.Count());
    }

    [Theory]
    [InlineData("deleted", "local", "01", "10.50.28.2")]
    [InlineData("issued", "dashboard", "01", "10.50.28.2")]
    [InlineData("issued", "local", "not-hex", "10.50.28.2")]
    public async Task Apply_InvalidEvent_Throws(string type, string origin, string serial, string ip)
    {
        await using var db = LocalIssuanceDb.Create();
        var server = LocalIssuanceDb.AddServer(db, 7);
        var evt = Parse(FakeNode.EventBody(type: type, serial: serial, staticIp: ip));
        evt.Origin = origin;

        await Assert.ThrowsAsync<ArgumentException>(() => CreateSut(db).ApplyAsync(server, evt, default));
        Assert.Empty(db.IssuedOvpnFiles);
    }

    /// <summary>Fails every async save (the service's writes); the sync seed in AddServer still goes through.</summary>
    private sealed class FailingAsyncSaves(Func<Exception> failure) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) => throw failure();
    }

    [Fact]
    public async Task Apply_NonUniqueWriteFailure_Propagates_SoTheNodeRetries()
    {
        await using var db = LocalIssuanceDb.Create(new FailingAsyncSaves(() => new DbUpdateException("disk full")));
        var server = LocalIssuanceDb.AddServer(db, 7);

        // A "Duplicate" answer here would be a 200 and the node would drop an event that was never stored.
        await Assert.ThrowsAsync<DbUpdateException>(() => CreateSut(db).ApplyAsync(server, Parse(FakeNode.EventBody()), default));
    }

    [Fact]
    public async Task Apply_UniqueViolationOnInsert_IsDuplicate()
    {
        await using var db = LocalIssuanceDb.Create(new FailingAsyncSaves(() => new DbUpdateException("race",
            new PostgresException("duplicate key", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation))));
        var server = LocalIssuanceDb.AddServer(db, 7);

        var (outcome, _) = await CreateSut(db).ApplyAsync(server, Parse(FakeNode.EventBody()), default);

        Assert.Equal(LocalIssuanceEventOutcome.Duplicate, outcome);
    }

    [Theory]
    [InlineData("9223372036854775807")]
    [InlineData("253402300800")] // first second after 9999-12-31
    public async Task Authenticate_TimestampOutsideDateRange_IsRejectedNotThrown(string timestamp)
    {
        using var node = new FakeNode();
        await using var db = LocalIssuanceDb.Create();
        LocalIssuanceDb.AddServer(db, 7, node);
        var body = FakeNode.EventBody();
        var (_, signature) = node.Sign(body, DateTimeOffset.UtcNow);

        Assert.False(NodeRequestSignature.Verify(node.PublicKeyPem, timestamp, signature, "POST",
            LocalIssuanceEventService.SignedPath, body, DateTimeOffset.UtcNow, out var error));
        Assert.Equal("missing or invalid timestamp", error);
        Assert.Null(await CreateSut(db).AuthenticateAsync(node.Fingerprint, timestamp, signature,
            "POST", LocalIssuanceEventService.SignedPath, body, default));
    }
}
