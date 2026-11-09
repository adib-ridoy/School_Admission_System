namespace SchoolAdmission.Api.DTOs;

public sealed record CreateClassRequest(string? Name, int TotalSeats);

public sealed record ClassResponse(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);
