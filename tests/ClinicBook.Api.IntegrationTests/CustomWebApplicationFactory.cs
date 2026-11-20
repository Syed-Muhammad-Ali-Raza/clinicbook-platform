using ClinicBook.Infrastructure.Identity;
using ClinicBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClinicBook.Api.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ClinicBookTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("UseInMemoryDatabase", "true");
        builder.UseSetting("ConnectionStrings:Default", string.Empty);

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                ["InMemoryDatabaseName"] = _databaseName,
                ["ConnectionStrings:Default"] = string.Empty,
                ["Redis:Connection"] = string.Empty,
                ["Jwt:Issuer"] = "https://localhost:8080",
                ["Jwt:Audience"] = "clinicbook-api-tests",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "14",
                ["Jwt:SigningKey"] = "TEST_ONLY_SIGNING_KEY_MUST_BE_AT_LEAST_32_CHARACTERS_LONG",
                ["AuthCookies:Secure"] = "false",
                ["Cors:AllowedOrigins:0"] = "https://localhost"
            };

            configBuilder.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtOptions>(options =>
            {
                options.Issuer = "https://localhost:8080";
                options.Audience = "clinicbook-api-tests";
                options.AccessTokenMinutes = 15;
                options.RefreshTokenDays = 14;
                options.SigningKey = "TEST_ONLY_SIGNING_KEY_MUST_BE_AT_LEAST_32_CHARACTERS_LONG";
            });

            services.PostConfigure<AuthCookieOptions>(options => options.Secure = false);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        return host;
    }
}
