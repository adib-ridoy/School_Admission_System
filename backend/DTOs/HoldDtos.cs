namespace SchoolAdmission.Api.DTOs;

public sealed record HoldResponse(
    string HoldId,
    string ClassId,
    string StudentId,
    string Status,
    DateTimeOffset ExpiresAt);
