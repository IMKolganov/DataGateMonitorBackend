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

	/// <summary>
	/// Distinct non-empty <c>CommonName</c> among currently connected clients on this server
	/// (device / installation CN; same notion as overview Devices (CN)).
	/// </summary>
	public int CountDevices { get; set; }

	public int CountSessions { get; set; }

	public int TotalBytesIn { get; set; }

	public int TotalBytesOut { get; set; }

	/// <summary>
	/// Effective dashboard Online badge: manager poll (<c>VpnServer.IsOnline</c>) and external probe
	/// (<c>VpnServer.IsAvailableByExternalProbe</c>), false when soft-deleted.
	/// Pushed on SignalR <c>StatusUpdated</c>.
	/// </summary>
	public bool IsOnline { get; set; }

	/// <summary>
	/// Raw external-probe flag (not composed). Optional for clients that want a separate badge.
	/// </summary>
	public bool IsAvailableByExternalProbe { get; set; } = true;
}
