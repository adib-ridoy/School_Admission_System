namespace SchoolAdmission.Api.Models;

public sealed class SeatHold
{
    public string Id { get; set; } = string.Empty;

    public string ClassId { get; set; } = string.Empty;

    public string StudentId { get; set; } = string.Empty;

    public HoldStatus Status { get; set; } = HoldStatus.Held;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public AdmissionClass Class { get; set; } = null!;
}
