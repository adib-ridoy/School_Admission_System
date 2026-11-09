using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public sealed class ClassesController(SchoolAdmissionDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ClassResponse>> Create(
        CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            return BadRequest(new { error = "Name is required and must be at most 200 characters." });
        }

        if (request.TotalSeats <= 0)
        {
            return BadRequest(new { error = "TotalSeats must be greater than zero." });
        }

        if (await db.Classes.AnyAsync(item => item.Name == name, cancellationToken))
        {
            return Conflict(new { error = "A class with this name already exists." });
        }

        var admissionClass = new AdmissionClass
        {
            Id = $"c_{Guid.NewGuid():N}"[..22],
            Name = name,
            TotalSeats = request.TotalSeats
        };

        db.Classes.Add(admissionClass);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { classId = admissionClass.Id },
            ToResponse(admissionClass, 0, 0));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClassResponse>>> List(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var classes = await db.Classes
            .AsNoTracking()
            .Include(item => item.Holds)
            .ToListAsync(cancellationToken);

        foreach (var admissionClass in classes)
        {
            ExpireHolds(admissionClass, now);
        }

        return Ok(classes.Select(item => ToResponse(item)).ToList());
    }

    [HttpGet("{classId}")]
    public async Task<ActionResult<ClassResponse>> GetById(
        string classId,
        CancellationToken cancellationToken)
    {
        var admissionClass = await db.Classes
            .AsNoTracking()
            .Include(item => item.Holds)
            .SingleOrDefaultAsync(item => item.Id == classId, cancellationToken);

        if (admissionClass is null)
        {
            return NotFound(new { error = "Class was not found." });
        }

        ExpireHolds(admissionClass, DateTimeOffset.UtcNow);
        return Ok(ToResponse(admissionClass));
    }

    private static ClassResponse ToResponse(AdmissionClass admissionClass, int held = -1, int confirmed = -1)
    {
        held = held < 0
            ? admissionClass.Holds.Count(item =>
                item.Status == HoldStatus.Held && item.ExpiresAt > DateTimeOffset.UtcNow)
            : held;
        confirmed = confirmed < 0
            ? admissionClass.Holds.Count(item => item.Status == HoldStatus.Confirmed)
            : confirmed;

        return new ClassResponse(
            admissionClass.Id,
            admissionClass.Name,
            admissionClass.TotalSeats,
            held,
            confirmed,
            admissionClass.TotalSeats - held - confirmed);
    }

    private static void ExpireHolds(AdmissionClass admissionClass, DateTimeOffset now)
    {
        foreach (var hold in admissionClass.Holds.Where(item =>
                     item.Status == HoldStatus.Held && item.ExpiresAt <= now))
        {
            hold.Status = HoldStatus.Expired;
        }
    }
}
