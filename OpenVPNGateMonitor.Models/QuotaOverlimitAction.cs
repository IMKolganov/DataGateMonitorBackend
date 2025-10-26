namespace OpenVPNGateMonitor.Models;

public enum QuotaOverlimitAction //todo: move to shared models
{
    /// <summary>Continue normally, no restrictions.</summary>
    AllowContinue,

    /// <summary>Reduce connection speed when limit is reached.</summary>
    LimitSpeed,

    /// <summary>Allow access only to internal portal or billing page.</summary>
    PortalOnly,

    /// <summary>Disconnect and block further VPN access.</summary>
    Disconnect
}
