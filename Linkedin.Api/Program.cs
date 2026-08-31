using CloudinaryDotNet;
using Linkedin.Api.BackgroundServices;
using Linkedin.Api.Helpers;
using Linkedin.Api.Hubs;
using Linkedin.Api.Notifications;
using Linkedin.Api.Identity;
using Linkedin.Business.Services.Concrete;
using Linkedin.Business.Services.Interface;
using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.DataAccess.Repositories.Concrete;
using Linkedin.DataAccess.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var cloudName = builder.Configuration["Cloudinary:CloudName"];
var apiKey = builder.Configuration["Cloudinary:ApiKey"];
var apiSecret = builder.Configuration["Cloudinary:ApiSecret"];

if (string.IsNullOrWhiteSpace(cloudName) ||
    string.IsNullOrWhiteSpace(apiKey) ||
    string.IsNullOrWhiteSpace(apiSecret))
{
    throw new InvalidOperationException(
        "Cloudinary configuration is missing. Check User Secrets.");
}

var cloudinaryAccount = new Account(cloudName, apiKey, apiSecret);

var cloudinary = new Cloudinary(cloudinaryAccount)
{
    Api = { Secure = true }
};

builder.Services.AddSingleton(cloudinary);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IUploadImage, UploadImage>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddHttpClient<IAiService, AiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(25);
});
builder.Services.AddScoped<IAiRateLimiterService, AiRateLimiterService>();
builder.Services.AddScoped<IJobPostService, JobPostService>();
builder.Services.AddScoped<IEducationRepository, EducationRepository>();
builder.Services.AddScoped<IExperienceRepository, ExperienceRepository>();
builder.Services.AddScoped<IUserSkillRepository, UserSkillRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IEmailCooldownService, EmailCooldownService>();
builder.Services.AddHttpClient<IEmailService, MailjetEmailService>(client =>
{
    client.BaseAddress = new Uri("https://api.mailjet.com/v3.1/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHostedService<RefreshTokenCleanupService>();
builder.Services.AddScoped<ICommentRepository,CommentRepository>();
builder.Services.AddScoped<ILikeRepository,LikeRepository>();
builder.Services.AddScoped<IConnectionRequestRepository, ConnectionRequestRepository>();
builder.Services.AddScoped<IConnectionRepository, ConnectionRepository>();
builder.Services.AddScoped<ICompanyFollowService, CompanyFollowService>();
builder.Services.AddScoped<INotficationsService, NotificationService>();
builder.Services.AddScoped<INotificationsRepository, NotificationsRepositor>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IJobPostRepository, JobPostRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierProvider>();
builder.Services.AddScoped<ILikeService, LikeService>();
builder.Services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();
builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IEventNotificationService, EventNotificationService>();
builder.Services.AddSignalR();

//builder.Services.AddControllers()
//    .AddJsonOptions(x =>
//        x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);


builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Tokens.EmailConfirmationTokenProvider = "NexoraEmailConfirmation";
    options.Tokens.PasswordResetTokenProvider = "NexoraPasswordReset";
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddTokenProvider<EmailConfirmationTokenProvider>("NexoraEmailConfirmation")
.AddTokenProvider<PasswordResetTokenProvider>("NexoraPasswordReset");

builder.Services.Configure<EmailConfirmationTokenProviderOptions>(options =>
{
    options.Name = "NexoraEmailConfirmation";
    options.TokenLifespan = TimeSpan.FromMinutes(15);
});

builder.Services.Configure<PasswordResetTokenProviderOptions>(options =>
{
    options.Name = "NexoraPasswordReset";
    options.TokenLifespan = TimeSpan.FromMinutes(10);
});


var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?.Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray()
    ?? Array.Empty<string>();

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "CORS allowed origins are missing. Configure Cors:AllowedOrigins.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});



var key = Encoding.ASCII.GetBytes(builder.Configuration["AppSettings:Token"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["AppSettings:Token"])),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType=ClaimTypes.Role,
        
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/notificationhub") ||
                 path.StartsWithSegments("/chathub") ||
                 path.StartsWithSegments("/likehub") ||
                 path.StartsWithSegments("/commenthub")||
                 path.StartsWithSegments("/connectionhub")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("StaffLogin", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Linkedin.Api", Version = "v1" });


    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please insert JWT with Bearer into field. Example: Bearer {your token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });


    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});


var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowClient");
app.UseRateLimiter();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            using var scope = context.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var isBlocked = await db.Users
                .AnyAsync(u => u.Id == userId && u.IsBlocked);

            if (isBlocked)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Your account has been blocked."
                });
                return;
            }
        }
    }

    await next();
});

app.UseAuthorization();

app.MapHub<NotificationHub>("/notificationhub");
app.MapHub<ChatHub>("/chathub");
app.MapHub<LikeHub>("/likehub");
app.MapHub<CommentHub>("/commenthub");
app.MapHub<ConnectionHub>("/connectionhub");

app.MapControllers();
app.MapGet("/", () => "LinkSphere API is running");


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var staffRole in new[] { "Admin", "Moderator" })
    {
        if (await roleManager.RoleExistsAsync(staffRole))
            continue;

        await roleManager.CreateAsync(new ApplicationRole
        {
            Name = staffRole,
            NormalizedName = staffRole.ToUpperInvariant()
        });
    }

    var adminEmail = builder.Configuration["AdminSeed:Email"];
    var adminPassword = builder.Configuration["AdminSeed:Password"];
    var adminUserName = builder.Configuration["AdminSeed:UserName"] ?? "admin";

    if (!string.IsNullOrWhiteSpace(adminEmail) &&
        !string.IsNullOrWhiteSpace(adminPassword))
    {
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminUserName,
                Email = adminEmail,
                FullName = "System Admin",
                EmailConfirmed = true,
                UserType = UserType.Staff,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    createResult.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Admin seed user could not be created: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    roleResult.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Admin role could not be assigned: {errors}");
            }
        }

        if (adminUser.UserType != UserType.Staff ||
            !adminUser.TwoFactorEnabled ||
            !adminUser.LockoutEnabled ||
            !adminUser.EmailConfirmed)
        {
            adminUser.UserType = UserType.Staff;
            adminUser.TwoFactorEnabled = true;
            adminUser.LockoutEnabled = true;
            adminUser.EmailConfirmed = true;
            var updateResult = await userManager.UpdateAsync(adminUser);

            if (!updateResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    updateResult.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Admin security settings could not be updated: {errors}");
            }
        }
    }

    var moderatorEmail = builder.Configuration["ModeratorSeed:Email"];
    var moderatorPassword = builder.Configuration["ModeratorSeed:Password"];
    var moderatorUserName = builder.Configuration["ModeratorSeed:UserName"] ?? "moderator";

    if (!string.IsNullOrWhiteSpace(moderatorEmail) &&
        !string.IsNullOrWhiteSpace(moderatorPassword))
    {
        var moderatorUser = await userManager.FindByEmailAsync(moderatorEmail);

        if (moderatorUser == null)
        {
            moderatorUser = new ApplicationUser
            {
                UserName = moderatorUserName,
                Email = moderatorEmail,
                FullName = "Content Moderator",
                EmailConfirmed = true,
                UserType = UserType.Staff,
                TwoFactorEnabled = true,
                LockoutEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(
                moderatorUser,
                moderatorPassword);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    createResult.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Moderator seed user could not be created: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(moderatorUser, "Moderator"))
        {
            var roleResult = await userManager.AddToRoleAsync(
                moderatorUser,
                "Moderator");

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    roleResult.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Moderator role could not be assigned: {errors}");
            }
        }

        if (moderatorUser.UserType != UserType.Staff ||
            !moderatorUser.TwoFactorEnabled ||
            !moderatorUser.LockoutEnabled ||
            !moderatorUser.EmailConfirmed)
        {
            moderatorUser.UserType = UserType.Staff;
            moderatorUser.TwoFactorEnabled = true;
            moderatorUser.LockoutEnabled = true;
            moderatorUser.EmailConfirmed = true;
            var updateResult = await userManager.UpdateAsync(moderatorUser);

            if (!updateResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    updateResult.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Moderator security settings could not be updated: {errors}");
            }
        }
    }
}
app.Run();

