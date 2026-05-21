using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddInfrastructure(builder.Configuration);

    var jwtSection = builder.Configuration.GetSection("Jwt");
    var jwtSecret = jwtSection["Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret must be configured.");

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies["access_token"]
                        ?? context.Request.Headers.Authorization
                            .FirstOrDefault()?.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    builder.Services.AddCors(options =>
    {
        var corsOriginsEnv = Environment.GetEnvironmentVariable("CORS_ORIGINS");
        var origins = corsOriginsEnv is not null
            ? corsOriginsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(origins)
                  .WithHeaders("Content-Type", "Authorization")
                  .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                  .AllowCredentials());
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("api", o =>
        {
            o.Window = TimeSpan.FromMinutes(1);
            o.PermitLimit = 120;
        });
        // SSE connections are long-lived, so a global concurrency cap would
        // serialise users behind one another. Partition per authenticated
        // user so each user gets an independent pool of tab/connection slots.
        options.AddPolicy("notification-stream", context =>
        {
            var userId = context.User.FindFirstValue("sub")
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous";

            return RateLimitPartition.GetConcurrencyLimiter(userId, _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 5,
                QueueLimit = 0,
            });
        });
    });

    builder.Services.AddControllers()
        .AddJsonOptions(o =>
            o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Notifications API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new()
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header
        });
        c.AddSecurityRequirement(new()
        {
            {
                new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
                []
            }
        });
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<Infrastructure.Persistence.NotificationsDbContext>();

    var app = builder.Build();

    app.UseExceptionHandler(exceptionApp =>
    {
        exceptionApp.Run(async context =>
        {
            var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            var exception = feature?.Error;

            Log.Error(exception, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.StatusCode = exception switch
            {
                ArgumentException or ArgumentNullException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                InvalidOperationException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "about:blank",
                title = "An error occurred while processing the request.",
                status = context.Response.StatusCode,
                traceId = context.TraceIdentifier
            });
        });
    });

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    // Rate limiter must run after authentication so the stream policy can
    // partition by the authenticated user's claim.
    app.UseRateLimiter();
    app.MapControllers();
    app.MapHealthChecks("/health").AllowAnonymous();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.NotificationsDbContext>();
        await db.Database.MigrateAsync();
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Notifications API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
