using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using MyRunshaw.Application.Authentication;
using MyRunshaw.Application.Buses;
using MyRunshaw.Infrastructure.Repositories;
using MyRunshaw.Infrastructure.Database;
using MyRunshaw.Infrastructure.Buses;
using MyRunshaw.Api.Swagger;
using MyRunshaw.Application.Notifications;
using MyRunshaw.Application.Friends;
using MyRunshaw.Infrastructure.Notifications;
using MyRunshaw.Application.Buses.Services;
using MyRunshaw.Application.Timetables;
using MyRunshaw.Infrastructure.Services;
using MyRunshaw.Application.Payments;
using MyRunshaw.Application.Sync;
using MyRunshaw.Application.Common;
using MyRunshaw.Application.Storage;
using MyRunshaw.Application.Users;
using System.Reflection;
using MyRunshaw.Application.Notices;
using OpenTelemetry.Exporter;

var builder = WebApplication.CreateBuilder(args);
var resourceBuilder = ResourceBuilder.CreateDefault().AddService("MyRunshaw.Api");

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    c.OperationFilter<SecurityRequirementsOperationFilter>();
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "My Runshaw API",
        Description = "The next-generation API used by the backend of the My Runshaw app to manage friendships, timetables, push notifications, buses and more. To authenticate with this API, you must provide a token from Entra to /api/auth/login, obtaining a JWT",
        TermsOfService = new Uri("https://privacy.danieldb.uk/terms"),
        Contact = new OpenApiContact
        {
            Name = "Daniel Benge",
            Url = new Uri("https://danieldb.uk")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        options => options.ConfigureDataSource(dataSourceBuilder =>
        {
            dataSourceBuilder.EnableDynamicJson();
        })
    )
);

// scoped = one instance is created per HTTP request
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBusRepository, BusRepository>();
builder.Services.AddScoped<IFriendRepository, FriendRepository>();
builder.Services.AddScoped<ITimetableRepository, TimetableRepository>();
builder.Services.AddScoped<IInAppNoticeRepository, NoticeRepository>();
builder.Services.AddScoped<INotificationDeviceRepository, NotificationDeviceRepository>();

builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBusService, BusService>();
builder.Services.AddScoped<ITimetableService, TimetableService>();
builder.Services.AddScoped<INameService, NameService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IStorageService, S3StorageService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<OneSignalPushService>();
builder.Services.AddScoped<FirebasePushService>();
builder.Services.AddScoped<IPushNotificationService>(sp =>
    string.Equals(builder.Configuration["PushNotifications:Provider"], "Firebase", StringComparison.OrdinalIgnoreCase)
        ? sp.GetRequiredService<FirebasePushService>()
        : sp.GetRequiredService<OneSignalPushService>());
builder.Services.AddScoped<INoticeService, NoticeService>();
builder.Services.AddScoped<INotificationDeviceService, NotificationDeviceService>();

builder.Services.AddHttpClient<IBusRouteScraper, BusRouteScraper>();
builder.Services.AddScoped<BusRouteScraperService>();
builder.Services.AddHttpClient<BusRouteScraperService>();

builder.Services.AddHttpClient<IBusArrivalScraper, BusArrivalScraper>();
builder.Services.AddScoped<BusArrivalScraperService>();

builder.Services.AddHttpClient<IPaymentService, PaymentScraperService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        UseCookies = false
    });
builder.Services.AddHttpClient<ITimetableSyncService, TimetableSyncService>();

var jwtSecret = builder.Configuration["JwtSettings:Secret"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // Students are free to integrate the API into their own apps
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(resourceBuilder)
               .AddAspNetCoreInstrumentation(
                o => o.Filter = context => context.Request.Path != "/health"
                   )          // Trace inbound HTTP requests
                   .AddHttpClientInstrumentation()          // Trace outbound HTTP requests e.g. OneSignal
                   .AddEntityFrameworkCoreInstrumentation() // Trace SQL queries
                   .AddOtlpExporter(
                        o =>
                        {
                            o.Endpoint = new Uri(
                               builder.Configuration["Opentelemetry:OtlpTracingEndpoint"] ?? "http://localhost:4317"
                           );
                            o.Protocol = OtlpExportProtocol.HttpProtobuf;
                            o.Headers = builder.Configuration["Opentelemetry:OtlpHeaders"] ?? "";
                        }
                    );
        if (builder.Environment.IsDevelopment())
        {
            // trace everything in dev
            tracing.SetSampler(new AlwaysOnSampler());
        }
        else
        {
            // trace only a percentage of requests in prod, defaulting to 10%
            tracing.SetSampler(new TraceIdRatioBasedSampler(builder.Configuration.GetValue<double>("Opentelemetry:TraceSampleRate", 0.1)));
        }
    });

if (builder.Configuration["Opentelemetry:OtlpMetricsEndpoint"] is not null)
{
    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics.SetResourceBuilder(resourceBuilder)
                   .AddAspNetCoreInstrumentation()
                   .AddRuntimeInstrumentation()
                   .AddOtlpExporter(o =>
               {
                   o.Endpoint = new Uri(builder.Configuration["Opentelemetry:OtlpMetricsEndpoint"] ?? "http://localhost:4317");
                   o.Protocol = OtlpExportProtocol.HttpProtobuf;

                   var headers = builder.Configuration["Opentelemetry:OtlpHeaders"];
                   if (!string.IsNullOrEmpty(headers))
                   {
                       o.Headers = headers;
                   }
               });
        });
}

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.IncludeFormattedMessage = true;
    logging.SetResourceBuilder(resourceBuilder);
    logging.AddOtlpExporter(
        o =>
        {
            o.Endpoint = new Uri(
               builder.Configuration["Opentelemetry:OtlpLoggingEndpoint"] ?? "http://localhost:4317"
           );
            o.Protocol = OtlpExportProtocol.HttpProtobuf;
            o.Headers = builder.Configuration["Opentelemetry:OtlpHeaders"] ?? "";
        }
    );
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "MyRunshaw_";
});

var app = builder.Build();

// students are free to integrate the API into their own apps, so we allow Swagger in production too
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
