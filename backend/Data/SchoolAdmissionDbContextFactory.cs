using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SchoolAdmission.Api.Data;

public sealed class SchoolAdmissionDbContextFactory : IDesignTimeDbContextFactory<SchoolAdmissionDbContext>
{
    public SchoolAdmissionDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<SchoolAdmissionDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new SchoolAdmissionDbContext(optionsBuilder.Options);
    }
}
