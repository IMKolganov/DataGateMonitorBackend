using System;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

public class ServiceStatusDto
{
	public int VpnServerId { get; set; }

	public ServiceStatus Status { get; set; }

	public string? ErrorMessage { get; set; }

	public DateTimeOffset NextRunTime { get; set; }

	public int CountConnectedClients { get; set; }

	public int CountSessions { get; set; }

	public int TotalBytesIn { get; set; }

	public int TotalBytesOut { get; set; }

	/// <summary>
	/// Live reachability of the VPN node (DB <c>VpnServer.IsOnline</c>, false when soft-deleted).
	/// Pushed on SignalR <c>StatusUpdated</c> for dashboard Online/Offline badges.
	/// </summary>
	public bool IsOnline { get; set; }
}
