using Microsoft.EntityFrameworkCore;
using MyRunshaw.Application.Notifications;
using MyRunshaw.Application.Timetables;
using MyRunshaw.Infrastructure.Database;
using MyRunshaw.Infrastructure.Notifications;
using MyRunshaw.Infrastructure.Repositories;
using MyRunshaw.Infrastructure.Services;
using MyRunshaw.Workers.Jobs;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Quartz;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options =>
     options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        options => options.ConfigureDataSource(dataSourceBuilder =>
        {
            dataSourceBuilder.EnableDynamicJson();
        })
    ));

builder.Services.AddHttpClient<ITimetableSyncService, TimetableSyncService>();
builder.Services.AddScoped<ITimetableRepository, TimetableRepository>();
builder.Services.AddScoped<IPushNotificationService, OneSignalPushService>();

builder.Services.AddQuartz(q =>
{
    var timetableJobKey = new JobKey("TimetableDailySyncJob");

    q.AddJob<TimetableDailySyncJob>(opts => opts.WithIdentity(timetableJobKey));

    q.AddTrigger(opts => opts
        .ForJob(timetableJobKey)
        .WithIdentity("TimetableDailySyncTrigger")
        // 7:30 AM every day
        .WithCronSchedule("0 30 7 * * ?", x =>
            // Adjust automatically for Daylight Saving Time
            x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/London")))
    );

    var routeJobKey = new JobKey("BusRouteScraperJob");
    q.AddJob<BusRouteScraperJob>(opts => opts.WithIdentity(routeJobKey));

    // start the bus route scraper job every Sunday at 3:00 AM
    q.AddTrigger(opts => opts
        .ForJob(routeJobKey)
        .WithIdentity("BusRouteScraperWeeklyTrigger")
        .WithCronSchedule("0 0 3 ? * SUN", x =>
            x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/London")))
    );

    var busResetJobKey = new JobKey("BusBayResetJob");
    q.AddJob<BusBayResetJob>(opts => opts.WithIdentity(busResetJobKey));

    q.AddTrigger(opts => opts
        .ForJob(busResetJobKey)
        .WithIdentity("BusBayResetTrigger")
        // 12:00 AM every day
        .WithCronSchedule("0 0 0 * * ?", x =>
            // Adjust automatically for Daylight Saving Time
            x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/London")))
    );

    // run bus route scraper on startup, so we don't have to wait until Sunday for the first scrape
    q.AddTrigger(opts => opts
        .ForJob(routeJobKey)
        .WithIdentity("BusRouteScraperStartupTrigger")
        .StartNow()
    );
});

builder.Services.AddScoped<MyRunshaw.Application.Buses.Services.BusArrivalScraperService>();
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
builder.Services.AddHostedService<MyRunshaw.Workers.HostedServices.BusArrivalScraper.BusArrivalScraperWorker>();

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService("MyRunshaw.Workers", serviceVersion: "1.0.0");

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
    });


if (builder.Configuration["Opentelemetry:OtlpMetricsEndpoint"] is not null)
{
    builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.SetResourceBuilder(resourceBuilder)
               .AddAspNetCoreInstrumentation()
               .AddRuntimeInstrumentation()
               .AddOtlpExporter(
                 o =>
                 {
                     o.Endpoint = new Uri(
                        builder.Configuration["Opentelemetry:OtlpMetricsEndpoint"]!
                    );
                     o.Protocol = OtlpExportProtocol.HttpProtobuf;
                     o.Headers = builder.Configuration["Opentelemetry:OtlpHeaders"] ?? "";
                 }
               );
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

builder.Services.Configure<OpenTelemetryLoggerOptions>(opt =>
{
    opt.ParseStateValues = true; // 
});

var host = builder.Build();
host.Run();
