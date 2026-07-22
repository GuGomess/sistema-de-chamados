using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Chamados.Api.Data;

public class ChamadosDbContextFactory : IDesignTimeDbContextFactory<ChamadosDbContext>
{
    public ChamadosDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=chamados;Username=chamados;Password=chamados";
        }

        var optionsBuilder = new DbContextOptionsBuilder<ChamadosDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ChamadosDbContext(optionsBuilder.Options);
    }
}
