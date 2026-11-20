using System.Text;
using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Infrastructure.Auditing;
using ClinicBook.Infrastructure.Caching;
using ClinicBook.Infrastructure.Events;
using ClinicBook.Infrastructure.Identity;
using ClinicBook.Infrastructure.Persistence;
using ClinicBook.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClinicBook.Infrastructure;

/// <summary>
/// Dependency injection registration for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds Infrastructure layer services.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var useInMemoryDatabase = configuration.GetValue<bool>("UseInMemoryDatabase")
                                  || string.Equals(configuration["UseInMemoryDatabase"], "true", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(configuration.GetConnectionString("Default"), "InMemory", StringComparison.OrdinalIgnoreCase);

        services.AddDbContext<AppDbContext>(options =>
        {
            if (useInMemoryDatabase)
            {
                var dbName = configuration["InMemoryDatabaseName"];
                if (string.IsNullOrWhiteSpace(dbName))
                {
                    dbName = "ClinicBookTests";
                }

                options.UseInMemoryDatabase(dbName);
                return;
            }

            var connectionString = configuration.GetConnectionString("Default");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                });
            }
        });

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AuthCookieOptions>(configuration.GetSection(AuthCookieOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthCookieManager, AuthCookieManager>();
        services.AddScoped<IHostEnvironmentAdapter, HostEnvironmentAdapter>();
        services.AddScoped<IAuthService, AuthService>();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false; // email verification required before first booking (enforced later)

                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredUniqueChars = 4;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // Harden PBKDF2 iteration count for password hashing (Identity default varies by version; enforce strong baseline).
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 600_000);

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromMinutes(15); // password reset tokens single-use and short-lived
        });

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
            {
                var jwtOptions = jwtOptionsAccessor.Value;
                var signingKey = BuildSigningKey(jwtOptions);

                options.RequireHttpsMetadata = true;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var cookieOptions = context.HttpContext.RequestServices
                            .GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthCookieOptions>>().Value;
                        if (context.Request.Cookies.TryGetValue(cookieOptions.AccessTokenName, out var token) &&
                            !string.IsNullOrWhiteSpace(token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        var redisConnection = configuration.GetSection("Redis")["Connection"];
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "clinicbook:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, DistributedCacheService>();

        return services;
    }

    private static SymmetricSecurityKey BuildSigningKey(JwtOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.KeyPath) && File.Exists(options.KeyPath))
        {
            var keyText = File.ReadAllText(options.KeyPath).Trim();
            // If PEM contains an RSA/EC key, JwtTokenService currently uses HMAC bytes; for validation we support raw secret bytes as symmetric key here.
            // Production should supply proper asymmetric key material and matching token service logic.
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
        }

        if (!string.IsNullOrWhiteSpace(options.SigningKey))
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        }

        // Fallback for design-time/build scenarios without configuration; runtime should configure a key.
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes("INSECURE_FALLBACK_KEY_CHANGE_ME_32_CHARS_MINIMUM"));
    }
}
