using Amazon.CloudWatchLogs;
using Amazon.S3;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using Tender_Tool_Logs_Lambda.Data;
using Tender_Tool_Logs_Lambda.Interfaces;
using Tender_Tool_Logs_Lambda.Services;

namespace Tender_Tool_Logs_Lambda;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            // Clear any default plain-text loggers
            builder.ClearProviders();

            // Add the JSON console logger
            builder.AddJsonConsole(options =>
            {
                options.IncludeScopes = false;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fffZ";
                options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
                {
                    Indented = false // Compact logs for CloudWatch
                };
            });

            // This is important: it re-adds the ability to read log levels 
            // from your appsettings.json (e.g., "Default": "Information")
            builder.AddConfiguration(Configuration.GetSection("Logging"));
        });

        services.AddCors(options =>
        {
            // Use AddDefaultPolicy
            options.AddDefaultPolicy(builder =>
            {
                builder
                .WithOrigins("*")
                .AllowAnyMethod()
                .AllowAnyHeader();
            });
        });

        services.AddControllers();

        // 1. Register the Database Context
        // Get the connection string from the Lambda's environment variable
        // instead of appsettings.json to avoid hardcoding secrets.
        var connectionString = Configuration["DB_CONNECTION_STRING"];

        // Fail fast if the environment variable is not set
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string 'DB_CONNECTION_STRING' is not set in the environment variables.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // 2. Register the AWS SDK Clients
        // This allows them to be injected into our services
        // (Requires AWSSDK.Extensions.NETCore.Setup NuGet package)
        services.AddAWSService<IAmazonCloudWatchLogs>();
        services.AddAWSService<IAmazonS3>();

        // 3. Register all our custom services (Interface -> Implementation)
        // We use AddScoped, which is standard for services in an web request.
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILogMapperService, LogMapperService>();
        services.AddScoped<ICloudWatchService, CloudWatchService>();
        services.AddScoped<ILogFormatterService, LogFormatterService>();
        services.AddScoped<IS3Service, S3Service>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseCors();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Welcome to the Tender Tool Logging Lambda");
            });
        });
    }
}