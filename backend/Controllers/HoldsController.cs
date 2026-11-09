using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("")]
public sealed class HoldsController(SchoolAdmissionDbContext db, IConfiguration configuration) : ControllerBase
{
    [HttpPost("classes/{classId}/holds")]
    public async Task<ActionResult<HoldResponse>> Create(
        string classId,
        CancellationToken cancellationToken)
    {
        var studentId = GetStudentId();
        if (studentId is null)
        {
            return BadRequest(new { error = "X-Student-Id header is required." });
        }

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        AdmissionClass? admissionClass;
        if (db.Database.IsRelational())
        {
            admissionClass = await db.Classes
                .FromSqlInterpolated($"SELECT * FROM classes WHERE \"Id\" = {classId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (admissionClass is not null)
            {
                await db.Entry(admissionClass).Collection(item => item.Holds).LoadAsync(cancellationToken);
            }
        }
        else
        {
            admissionClass = await db.Classes
                .Include(item => item.Holds)
                .SingleOrDefaultAsync(item => item.Id == classId, cancellationToken);
        }

        if (admissionClass is null)
        {
            return NotFound(new { error = "Class was not found." });
        }

        var now = DateTimeOffset.UtcNow;
        ExpireHolds(admissionClass, now);
        if (admissionClass.Holds.Any(item =>
                item.StudentId == studentId &&
                (item.Status == HoldStatus.Held || item.Status == HoldStatus.Confirmed)))
        {
            return Conflict(new { error = "The student already has an active application for this class." });
        }

        var heldSeats = admissionClass.Holds.Count(item =>
            item.Status == HoldStatus.Held && item.ExpiresAt > now);
        var confirmedSeats = admissionClass.Holds.Count(item => item.Status == HoldStatus.Confirmed);
        if (heldSeats + confirmedSeats >= admissionClass.TotalSeats)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { error = "No seats are available." });
        }

        var duration = configuration.GetValue("HOLD_DURATION_SECONDS", 600);
        if (duration <= 0)
        {
            return Problem("HOLD_DURATION_SECONDS must be greater than zero.", statusCode: 500);
        }

        var hold = new SeatHold
        {
            Id = $"h_{Guid.NewGuid():N}"[..22],
            ClassId = classId,
            StudentId = studentId,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(duration)
        };

        db.Holds.Add(hold);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return StatusCode(StatusCodes.Status201Created, ToResponse(hold));
    }

    [HttpGet("holds/{holdId}")]
    public async Task<ActionResult<HoldResponse>> Get(
        string holdId,
        CancellationToken cancellationToken)
    {
        var hold = await db.Holds.SingleOrDefaultAsync(item => item.Id == holdId, cancellationToken);
        if (hold is null)
        {
            return NotFound(new { error = "Hold was not found." });
        }

        ExpireHold(hold);
        if (hold.Status == HoldStatus.Held)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(ToResponse(hold));
    }

    [HttpPost("holds/{holdId}/confirm")]
    public async Task<ActionResult<HoldResponse>> Confirm(
        string holdId,
        CancellationToken cancellationToken)
    {
        var studentId = GetStudentId();
        if (studentId is null)
        {
            return BadRequest(new { error = "X-Student-Id header is required." });
        }

        var hold = await db.Holds.SingleOrDefaultAsync(item => item.Id == holdId, cancellationToken);
        if (hold is null)
        {
            return NotFound(new { error = "Hold was not found." });
        }

        if (hold.StudentId != studentId)
        {
            return Forbid();
        }

        ExpireHold(hold);
        if (hold.Status == HoldStatus.Expired || hold.Status == HoldStatus.Cancelled)
        {
            return Conflict(new { error = $"The hold is {hold.Status.ToString().ToUpperInvariant()}." });
        }

        if (hold.Status == HoldStatus.Held)
        {
            hold.Status = HoldStatus.Confirmed;
            hold.ConfirmedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(ToResponse(hold));
    }

    [HttpDelete("holds/{holdId}")]
    public async Task<IActionResult> Cancel(string holdId, CancellationToken cancellationToken)
    {
        var studentId = GetStudentId();
        if (studentId is null)
        {
            return BadRequest(new { error = "X-Student-Id header is required." });
        }

        var hold = await db.Holds.SingleOrDefaultAsync(item => item.Id == holdId, cancellationToken);
        if (hold is null)
        {
            return NotFound(new { error = "Hold was not found." });
        }

        if (hold.StudentId != studentId)
        {
            return Forbid();
        }

        if (hold.Status == HoldStatus.Held)
        {
            hold.Status = HoldStatus.Cancelled;
            hold.CancelledAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("me/holds")]
    public async Task<ActionResult<IReadOnlyList<HoldResponse>>> Mine(CancellationToken cancellationToken)
    {
        var studentId = GetStudentId();
        if (studentId is null)
        {
            return BadRequest(new { error = "X-Student-Id header is required." });
        }

        var holds = await db.Holds
            .AsNoTracking()
            .Where(item => item.StudentId == studentId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var hold in holds)
        {
            ExpireHold(hold);
        }

        return Ok(holds.Select(ToResponse).ToList());
    }

    private string? GetStudentId()
    {
        var value = Request.Headers["X-Student-Id"].FirstOrDefault()?.Trim();
        return string.IsNullOrWhiteSpace(value) || value.Length > 100 ? null : value;
    }

    private static void ExpireHold(SeatHold hold)
    {
        if (hold.Status == HoldStatus.Held && hold.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            hold.Status = HoldStatus.Expired;
        }
    }

    private static void ExpireHolds(AdmissionClass admissionClass, DateTimeOffset now)
    {
        foreach (var hold in admissionClass.Holds.Where(item =>
                     item.Status == HoldStatus.Held && item.ExpiresAt <= now))
        {
            hold.Status = HoldStatus.Expired;
        }
    }

    private static HoldResponse ToResponse(SeatHold hold) =>
        new(hold.Id, hold.ClassId, hold.StudentId, hold.Status.ToString().ToUpperInvariant(), hold.ExpiresAt);
}
