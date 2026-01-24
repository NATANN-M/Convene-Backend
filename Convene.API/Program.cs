using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Convene.Application.Interfaces;
using Convene.Application.Interfaces.Convene.Application.Interfaces;
using Convene.Application.Settings;
using Convene.Infrastructure.BackgroundServices;
using Convene.Infrastructure.Helpers;
using Convene.Infrastructure.Hubs;
using Convene.Infrastructure.Persistence;
using Convene.Infrastructure.Seeders;
using Convene.Infrastructure.Services;
using Convene.Infrastructure.Services.Recommendation;
using System.Security.Claims;
using System.Text;
using YourNamespace.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------
//  Serilog Global Configuration
// ------------------------------------------------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/log-.json",
        rollingInterval: RollingInterval.Day,
        formatter: new JsonFormatter()
    )
    .CreateLogger();

//  Attach Serilog to ASP.NET Core
builder.Host.UseSerilog();



// ------------------------------------------------------------
//  Database Context
// ------------------------------------------------------------
// ------------------------------------------------------------
//  Database Context
// ------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                      ?? Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(connectionString))
{
    connectionString = connectionString.Trim();
    
    // Handle Render's URI format (postgres:// or postgresql://)
    if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || 
        connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var user = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        var host = uri.Host;
        var dbPort = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        connectionString = $"Host={host};Port={dbPort};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}

builder.Services.AddDbContext<ConveneDbContext>(options =>
{
    options.UseNpgsql(connectionString);

    // Log only warnings or errors from SQL commands
    // options.LogTo(Log.Warning, new[] { "Microsoft.EntityFrameworkCore.Database.Command" }, LogLevel.Warning);
    options.LogTo(_ => { }, LogLevel.None); // Do nothing, ignore all logs

    options.EnableSensitiveDataLogging(false);
});



// ------------------------------------------------------------
//  Application Services
// ------------------------------------------------------------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventCategoryService, EventCategoryService>();
builder.Services.AddScoped<ITicketTypeService, TicketTypeService>();
builder.Services.AddScoped<IPricingRuleService, PricingRuleService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IEventFeedbackService, EventFeedbackService>();
builder.Services.AddScoped<IOrganizerDashboardService, OrganizerDashboardService>();

builder.Services.AddScoped<INotificationService, NotificationService>();   
builder.Services.AddScoped<IEventBrowsingService, EventBrowsingService>();


builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

builder.Services.AddScoped<ITrackingService, TrackingService>();
builder.Services.AddScoped<IRuleScoringService, RuleScoringService>();
builder.Services.AddScoped<IMLService, MLService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IAttendeeProfileService, AttendeeProfileService>(); 
builder.Services.AddScoped<IOrganizerProfileService, OrganizerProfileService>();
builder.Services.AddScoped<IOrganizerAnalyticsService, OrganizerAnalyticsService>();
builder.Services.AddScoped<IGatePersonService, GatePersonService>();
builder.Services.AddScoped<ITicketScanService, TicketScanService>();
builder.Services.AddScoped<ITelegramService, TelegramService>();

builder.Services.AddScoped<IBoostService, BoostService>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<IPlatformSettingsService, PlatformSettingsService>();

builder.Services.AddScoped<IAdminNotificationService, AdminNotificationService>();
builder.Services.AddScoped<IOrganizerNotificationService, OrganizerNotificationService>();
builder.Services.AddScoped<HealthService>();

// Background Services and singletons
//builder.Services.AddHostedService<SimpleNotificationService>();
builder.Services.AddHostedService<PaymentReminderService>();
builder.Services.AddHostedService<UnpaidBookingCleanupService>();
builder.Services.AddHostedService<FeedbackReminderService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddHostedService<PaymentVerificationJob>();
builder.Services.AddHostedService<MLTrainingBackgroundService>();
builder.Services.AddHostedService<EventReminderBackgroundService>();

// ------------------------------------------------------------
//  Helpers
// ------------------------------------------------------------
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddHttpClient();

builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));



// Register service
builder.Services.AddScoped<ICoverGenerationService, CoverGenerationService>();


// ------------------------------------------------------------
//  CORS (Allow All Frontends - Development Mode)
// ------------------------------------------------------------
builder.Services.AddCors(options =>
{
    var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:5173";
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://localhost:3000", frontendUrl) // Allow local and configured frontend URL
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Required for SignalR
    });
});

// ------------------------------------------------------------
//  JWT Authentication
// ------------------------------------------------------------
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrEmpty(jwtSecret))
    throw new Exception("JWT secret key is missing in configuration.");

var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        NameClaimType = ClaimTypes.Email,
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault();
            
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            // If the request is for our hub...
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs/notifications")))
            {
                // Read the token out of the query string
                token = accessToken;
            }

            if (!string.IsNullOrEmpty(token))
            {
                if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                     // If it came from query string it might not have Bearer prefix
                     token = "Bearer " + token;
                }   
                
                context.Token = token.Substring("Bearer ".Length).Trim();
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ------------------------------------------------------------
//  Controllers & Swagger
// ------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Convene API", Version = "v1" });

    c.AddSecurityDefinition("Token", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Description = "Paste your JWT token directly (no 'Bearer ' prefix needed)"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Token"
                }
            },
            Array.Empty<string>()
        }
    });
});

//Adding signalIR for realtime notification
builder.Services.AddSignalR();
//api rate limiter
builder.Services.AddAppRateLimiting();

var app = builder.Build();

//DI for the notification hub SignalIR
app.MapHub<NotificationHub>("/hubs/notifications");

//for rate limiter 
app.UseRateLimiter();

app.UseMiddleware<Convene.Infrastructure.Middleware.ResponseTimingMiddleware>();


// ------------------------------------------------------------
//  Serilog Request Logging
// ------------------------------------------------------------
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (ctx, http) =>
    {
        ctx.Set("RequestMethod", http.Request.Method);
        ctx.Set("RequestPath", http.Request.Path);
        ctx.Set("QueryString", http.Request.QueryString.Value);
        ctx.Set("UserAgent", http.Request.Headers["User-Agent"].ToString());
        ctx.Set("StatusCode", http.Response.StatusCode);
    };
});


// ------------------------------------------------------------
//  Middleware Pipeline
// ------------------------------------------------------------

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<EndpointTrackingMiddleware>();

// Custom error handler middleware
app.UseErrorHandling();

//app.UseHttpsRedirection();

//  Enable unrestricted CORS for all frontends
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();



// ------------------------------------------------------------
//  Seed Initial Data (Idempotent - Safe to run every time)
// ------------------------------------------------------------


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ConveneDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var errors = new List<string>();

    // ------------------------------------------------------------
    // Apply database migrations
    // ------------------------------------------------------------
    try
    {
        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        errors.Add("Database migrations failed");
        logger.LogError(ex, "Database migrations failed");
    }

    // ------------------------------------------------------------
    // Seed Super Admin
    // ------------------------------------------------------------
    try
    {
        logger.LogInformation("Seeding Super Admin...");
        await DbSeeder.SeedSuperAdminAsync(dbContext, hasher, builder.Configuration);
        logger.LogInformation("Super Admin seeded successfully.");
    }
    catch (Exception ex)
    {
        errors.Add("Super Admin seeding failed");
        logger.LogError(ex, "Super Admin seeding failed");
    }

    // ------------------------------------------------------------
    // Seed Event Categories
    // ------------------------------------------------------------
    try
    {
        logger.LogInformation("Seeding Event Categories...");
        await EventCategorySeeder.SeedAsync(dbContext);
        logger.LogInformation("Event Categories seeded successfully.");
    }
    catch (Exception ex)
    {
        errors.Add("Event Categories seeding failed");
        logger.LogError(ex, "Event Categories seeding failed");
    }

    // ------------------------------------------------------------
    // Seed Boost Levels
    // ------------------------------------------------------------
    try
    {
        logger.LogInformation("Seeding Boost Levels...");
        await BoostLevelSeeder.SeedAsync(dbContext);
        logger.LogInformation("Boost Levels seeded successfully.");
    }
    catch (Exception ex)
    {
        errors.Add("Boost Levels seeding failed");
        logger.LogError(ex, "Boost Levels seeding failed");
    }

    // ------------------------------------------------------------
    // Seed Platform Settings
    // ------------------------------------------------------------
    try
    {
        logger.LogInformation("Seeding Platform Settings...");
        await PlatformSettingsSeeder.SeedAsync(dbContext);
        logger.LogInformation("Platform Settings seeded successfully.");
    }
    catch (Exception ex)
    {
        errors.Add("Platform Settings seeding failed");
        logger.LogError(ex, "Platform Settings seeding failed");
    }

    // ------------------------------------------------------------
    // Summary
    // ------------------------------------------------------------
    if (errors.Count > 0)
    {
        logger.LogWarning(
            "Application started with {ErrorCount} seeding warning(s)",
            errors.Count
        );

        foreach (var error in errors)
        {
            logger.LogWarning("Seeding issue: {Error}", error);
        }
    }
    else
    {
        logger.LogInformation("All database migrations and seeders completed successfully.");
    }
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
