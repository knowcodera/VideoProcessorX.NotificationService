using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Services;
using NotificationService.Domain.Interfaces;
using NotificationService.Infraestructure.Configuration;
using NotificationService.Infraestructure.Email;
using NotificationService.Infraestructure.Messaging;
using NotificationService.Infraestructure.Persistence;
using System.Net;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();
builder.Logging.AddDebug();

var smtpHost = builder.Configuration["Smtp:Host"]!;
var smtpPort = int.Parse(builder.Configuration["Smtp:Port"]!);
var smtpUser = builder.Configuration["Smtp:Username"]!;
var smtpPass = builder.Configuration["Smtp:Password"]!;
var fromEmail = builder.Configuration["Smtp:From"]!;

builder.Services
    .AddFluentEmail(fromEmail)
    .AddRazorRenderer()
    .AddSmtpSender(new SmtpClient(smtpHost, smtpPort)
    {
        Credentials = new NetworkCredential(smtpUser, smtpPass),
        EnableSsl = true
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()
    ));

var resilienceConfig = new ResiliencePolicyConfig
{
    RetryCount = builder.Configuration.GetValue<int>("Resilience:RetryCount"),
    RetryBaseDelaySeconds = builder.Configuration.GetValue<int>("Resilience:RetryBaseDelaySeconds"),
    CircuitBreakerThreshold = builder.Configuration.GetValue<int>("Resilience:CircuitBreakerThreshold"),
    CircuitBreakerDurationSeconds = builder.Configuration.GetValue<int>("Resilience:CircuitBreakerDurationSeconds")
};

builder.Services.AddHttpClient("EmailSender")
    .AddPolicyHandler((services, request) =>
        HttpClientPolicyExtensions.GetRetryPolicy(
            resilienceConfig,
            services.GetRequiredService<ILogger<Program>>()))
    .AddPolicyHandler((services, request) =>
        HttpClientPolicyExtensions.GetCircuitBreakerPolicy(
            resilienceConfig,
            services.GetRequiredService<ILogger<Program>>()));

builder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IEmailSender, FluentEmailSender>();
builder.Services.AddScoped<INotificationEmailService, NotificationEmailService>();
builder.Services.AddHostedService<RabbitMqNotificationListener>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.UseSwagger();

app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.Run(); 
}
else
{
    app.Run("http://*:6565");
}
