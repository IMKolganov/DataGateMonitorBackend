using System;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

public class VpnClientInfoDto
{
	public int Id { get; set; }

	public int VpnServerId { get; set; }

	public string ExternalId { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public string? AvatarUrl { get; set; }

	public Guid SessionId { get; set; }

	public string CommonName { get; set; } = string.Empty;

	public string RemoteIp { get; set; } = string.Empty;

	public string? ProxyRealIp { get; set; }

	public string LocalIp { get; set; } = string.Empty;

	public long BytesReceived { get; set; }

	public long BytesSent { get; set; }

	public DateTimeOffset ConnectedSince { get; set; }

	public string Username { get; set; } = string.Empty;

	public string? Country { get; set; }

	public string? Region { get; set; }

	public string? City { get; set; }

	public double? Latitude { get; set; }

	public double? Longitude { get; set; }

	public bool IsConnected { get; set; }
}
