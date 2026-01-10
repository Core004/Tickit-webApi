using Microsoft.EntityFrameworkCore;
using Serilog;
using TicketSystem.API.Hubs;
using TicketSystem.API.Middleware;
using TicketSystem.Application;
using TicketSystem.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TicketSystem API",
        Version = "v1",
        Description = "Production-ready Ticket Management System API"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR()
    .AddMessagePackProtocol();

// Register Hub Services
builder.Services.AddScoped<INotificationHubService, NotificationHubService>();
builder.Services.AddScoped<ITeamChatHubService, TeamChatHubService>();
builder.Services.AddScoped<IChatbotHubService, ChatbotHubService>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TicketSystem API v1");
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SignalR Hub endpoints
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<TeamChatHub>("/hubs/teamchat");
app.MapHub<ChatbotHub>("/hubs/chatbot");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

try
{
    Log.Information("Starting TicketSystem API");

    // Seed database
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<TicketSystem.Infrastructure.Persistence.ApplicationDbContext>();
            await context.Database.MigrateAsync();

            await TicketSystem.Infrastructure.Persistence.Seeding.RoleSeeder.SeedRolesAsync(services);
            await TicketSystem.Infrastructure.Persistence.Seeding.AdminUserSeeder.SeedAdminUserAsync(services);
            await TicketSystem.Infrastructure.Persistence.Seeding.StatusSeeder.SeedStatusesAsync(context);
            await TicketSystem.Infrastructure.Persistence.Seeding.StatusSeeder.SeedPrioritiesAsync(context);
            await TicketSystem.Infrastructure.Persistence.Seeding.StatusSeeder.SeedCategoriesAsync(context);
            await TicketSystem.Infrastructure.Persistence.Seeding.PermissionSeeder.SeedPermissionsAsync(context);
            await TicketSystem.Infrastructure.Persistence.Seeding.AvatarSeeder.SeedAvatarsAsync(context);

            Log.Information("Database seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while seeding the database");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
