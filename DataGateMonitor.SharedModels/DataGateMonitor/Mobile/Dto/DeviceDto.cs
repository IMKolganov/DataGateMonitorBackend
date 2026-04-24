using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.SharedModels.DataGateMonitor.Mobile.Dto;

public class DeviceDto
{
    public int Id { get; set; }
    [Required]
    public int UserId { get; set; }
    [Required, MaxLength(255)]
    public string InstallationId { get; set; } = null!;
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
}