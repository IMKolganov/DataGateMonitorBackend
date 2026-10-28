using DataGateMonitor.Controllers;
using DataGateMonitor.Services.LocalIssuance;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DataGateMonitor.Tests.Services.LocalIssuance;

public class VpnNodeLocalIssuanceControllerTests
{
    private static VpnNodeLocalIssuanceController CreateController(
        DataGateMonitor.DataBase.Contexts.ApplicationDbContext db, byte[] body, IDictionary<string, string>? headers = null)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Post;
        http.Request.Body = new MemoryStream(body);
        foreach (var (k, v) in headers ?? new Dictionary<string, string>())
            http.Request.Headers[k] = v;

        return new VpnNodeLocalIssuanceController(
            new LocalIssuanceEventService(db, TimeProvider.System, NullLogger<LocalIssuanceEventService>.Instance),
            Mock.Of<INodeKeyBindingService>(),
            NullLogger<VpnNodeLocalIssuanceController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    private static Dictionary<string, string> Signed(FakeNode node, byte[] body)
    {
        var (ts, sig) = node.Sign(body, DateTimeOffset.UtcNow);
        return new()
        {
            [NodeRequestSignature.KeyIdHeader] = node.Fingerprint,
            [NodeRequestSignature.TimestampHeader] = ts,
            [NodeRequestSignature.SignatureHeader] = sig,
        };
    }

    [Fact]
    public async Task Post_WithoutSignature_Is401_AndNothingIsStored()
    {
        using var node = new FakeNode();
        await using var db = LocalIssuanceDb.Create();
        LocalIssuanceDb.AddServer(db, 7, node);

        var result = await CreateController(db, FakeNode.EventBody()).PostLocalIssuanceEvent(default);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Empty(db.IssuedOvpnFiles);
    }

    [Fact]
    public async Task Post_SignedByKeyNobodyApproved_Is401_AndNothingIsStored()
    {
        using var trusted = new FakeNode();
        using var attacker = new FakeNode();
        await using var db = LocalIssuanceDb.Create();
        LocalIssuanceDb.AddServer(db, 7, trusted);
        var body = FakeNode.EventBody();

        var result = await CreateController(db, body, Signed(attacker, body)).PostLocalIssuanceEvent(default);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Empty(db.IssuedOvpnFiles);
    }

    [Fact]
    public async Task Post_SignedByTrustedNode_StoresLocalProfile_RetryIsDuplicate()
    {
        using var node = new FakeNode();
        await using var db = LocalIssuanceDb.Create();
        LocalIssuanceDb.AddServer(db, 7, node);
        var body = FakeNode.EventBody();

        var first = await CreateController(db, body, Signed(node, body)).PostLocalIssuanceEvent(default);
        var retry = await CreateController(db, body, Signed(node, body)).PostLocalIssuanceEvent(default);

        var ok1 = Assert.IsType<OkObjectResult>(first.Result);
        var ok2 = Assert.IsType<OkObjectResult>(retry.Result);
        Assert.Equal("Created", ((ApiResponse<LocalIssuanceEventResponse>)ok1.Value!).Data!.Outcome);
        Assert.Equal("Duplicate", ((ApiResponse<LocalIssuanceEventResponse>)ok2.Value!).Data!.Outcome);
        Assert.Single(db.IssuedOvpnFiles);
    }

    [Fact]
    public async Task Post_SignedButMalformed_Is400()
    {
        using var node = new FakeNode();
        await using var db = LocalIssuanceDb.Create();
        LocalIssuanceDb.AddServer(db, 7, node);
        var body = "{\"EventType\":\"issued\",\"Origin\":\"local\",\"CommonName\":\"db1\",\"SerialNumber\":\"zz\"}"u8.ToArray();

        var result = await CreateController(db, body, Signed(node, body)).PostLocalIssuanceEvent(default);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
