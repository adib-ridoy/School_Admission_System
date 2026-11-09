using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SchoolAdmission.Api.Controllers;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Tests;

public sealed class AdmissionApiTests
{
    [Fact]
    public async Task CreateClass_ReturnsClassWithAvailableSeats()
    {
        await using var db = CreateDb();
        var controller = new ClassesController(db);

        var result = await controller.Create(
            new CreateClassRequest("Class 6 - Morning Shift", 30),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ClassResponse>(created.Value);
        Assert.Equal(30, response.AvailableSeats);
    }

    [Fact]
    public async Task StudentCanHoldAndConfirmASeat()
    {
        await using var db = CreateDb();
        db.Classes.Add(new AdmissionClass { Id = "c_test", Name = "Class 6", TotalSeats = 2 });
        await db.SaveChangesAsync();

        var controller = CreateHoldsController(db, "student-1");
        var holdResult = await controller.Create("c_test", CancellationToken.None);
        var hold = Assert.IsType<ObjectResult>(holdResult.Result);
        var holdResponse = Assert.IsType<HoldResponse>(hold.Value);
        Assert.Equal("HELD", holdResponse.Status);

        var confirmation = await controller.Confirm(holdResponse.HoldId, CancellationToken.None);
        var confirmed = Assert.IsType<OkObjectResult>(confirmation.Result);
        Assert.Equal("CONFIRMED", Assert.IsType<HoldResponse>(confirmed.Value).Status);
    }

    [Fact]
    public async Task StudentCannotCreateTwoActiveApplicationsForOneClass()
    {
        await using var db = CreateDb();
        db.Classes.Add(new AdmissionClass { Id = "c_test", Name = "Class 6", TotalSeats = 2 });
        await db.SaveChangesAsync();

        var controller = CreateHoldsController(db, "student-1");
        var first = await controller.Create("c_test", CancellationToken.None);
        var firstResponse = Assert.IsType<ObjectResult>(first.Result);
        Assert.Equal(201, firstResponse.StatusCode);

        var second = await controller.Create("c_test", CancellationToken.None);
        var conflict = Assert.IsType<ConflictObjectResult>(second.Result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task ConcurrentHoldsNeverExceedClassCapacity()
    {
        const int requestCount = 20;
        var databaseName = $"tests-{Guid.NewGuid():N}";
        await using var db = CreateDb(databaseName);
        db.Classes.Add(new AdmissionClass { Id = "c_test", Name = "Class 6", TotalSeats = 1 });
        await db.SaveChangesAsync();

        var results = await Task.WhenAll(
            Enumerable.Range(0, requestCount).Select(async index =>
            {
                await using var requestDb = CreateDb(databaseName);
                var controller = CreateHoldsController(requestDb, $"student-{index}");
                return await controller.Create("c_test", CancellationToken.None);
            }));

        var successful = results.Count(result =>
            result.Result is ObjectResult { StatusCode: StatusCodes.Status201Created });
        var activeHolds = await db.Holds.CountAsync(item => item.Status == HoldStatus.Held);

        Assert.True(successful <= 1, $"Expected at most one successful hold, but got {successful}.");
        Assert.True(activeHolds <= 1, $"Expected at most one active hold, but got {activeHolds}.");
    }

    private static SchoolAdmissionDbContext CreateDb(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<SchoolAdmissionDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"tests-{Guid.NewGuid():N}")
            .Options;
        return new SchoolAdmissionDbContext(options);
    }

    private static HoldsController CreateHoldsController(
        SchoolAdmissionDbContext db,
        string studentId)
    {
        var controller = new HoldsController(
            db,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["HOLD_DURATION_SECONDS"] = "600"
                })
                .Build());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Headers["X-Student-Id"] = studentId;
        return controller;
    }
}
