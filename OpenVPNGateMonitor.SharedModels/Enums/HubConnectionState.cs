// Decompiled with JetBrains decompiler
// Type: Microsoft.AspNetCore.SignalR.Client.HubConnectionState
// Assembly: Microsoft.AspNetCore.SignalR.Client.Core, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
// MVID: 43468FCA-83AD-4EEA-A1DF-02009F0E50C5
// Assembly location: /home/imkolganov/.nuget/packages/microsoft.aspnetcore.signalr.client.core/10.0.0/lib/net10.0/Microsoft.AspNetCore.SignalR.Client.Core.dll
// XML documentation location: /home/imkolganov/.nuget/packages/microsoft.aspnetcore.signalr.client.core/10.0.0/lib/net10.0/Microsoft.AspNetCore.SignalR.Client.Core.xml

#nullable disable
namespace Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// Describes the current state of the <see cref="T:Microsoft.AspNetCore.SignalR.Client.HubConnection" /> to the server.
/// </summary>
public enum HubConnectionState
{
    /// <summary>The hub connection is disconnected.</summary>
    Disconnected,
    /// <summary>The hub connection is connected.</summary>
    Connected,
    /// <summary>The hub connection is connecting.</summary>
    Connecting,
    /// <summary>The hub connection is reconnecting.</summary>
    Reconnecting,
}