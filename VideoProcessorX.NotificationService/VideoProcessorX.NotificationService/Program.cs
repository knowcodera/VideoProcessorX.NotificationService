using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Services;
using NotificationService.Domain.Interfaces;
using NotificationService.Infraestructure.Configuration;
using NotificationService.Infraestructure.Email;
using NotificationService.Infraestructure.Messaging;
using NotificationService.Infraestructure.Persistence;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);

// Configuração do Logger
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configuração do SMTP
var smtpHost = builder.Configuration["Smtp:Host"]!;
var smtpPort = int.Parse(builder.Configuration["Smtp:Port"]!);
var smtpUser = builder.Configuration["Smtp:Username"]!;
var smtpPass = builder.Configuration["Smtp:Password"]!;
var fromEmail = builder.Configuration["Smtp:From"]!;

// Configuração do FluentEmail
builder.Services
    .AddFluentEmail(fromEmail)
    .AddRazorRenderer()
    .AddSmtpSender(new SmtpClient(smtpHost, smtpPort)
    {
        Credentials = new NetworkCredential(smtpUser, smtpPass),
        EnableSsl = true
    });

// Configuração do Banco de Dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));

// Configuração de Resiliência
var resilienceConfig = new ResiliencePolicyConfig
{
    RetryCount = builder.Configuration.GetValue<int>("Resilience:RetryCount"),
    RetryBaseDelaySeconds = builder.Configuration.GetValue<int>("Resilience:RetryBaseDelaySeconds"),
    CircuitBreakerThreshold = builder.Configuration.GetValue<int>("Resilience:CircuitBreakerThreshold"),
    CircuitBreakerDurationSeconds = builder.Configuration.GetValue<int>("Resilience:CircuitBreakerDurationSeconds")
};

// Configuração do HttpClient com Polly
builder.Services.AddHttpClient("EmailSender")
    .AddPolicyHandler((services, request) =>
        HttpClientPolicyExtensions.GetRetryPolicy(
            resilienceConfig,
            services.GetRequiredService<ILogger<Program>>()))
    .AddPolicyHandler((services, request) =>
        HttpClientPolicyExtensions.GetCircuitBreakerPolicy(
            resilienceConfig,
            services.GetRequiredService<ILogger<Program>>()));

// Registro de Serviços
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IEmailSender, FluentEmailSender>();
builder.Services.AddScoped<INotificationEmailService, NotificationEmailService>();
builder.Services.AddHostedService<RabbitMqNotificationListener>();

// Configuração do Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// 9. Implementação das Políticas de Resiliência
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (_, delay, retryCount, _) =>
            {
                Console.WriteLine($"Retentativa #{retryCount} em {delay.TotalSeconds}s");
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            5,
            TimeSpan.FromSeconds(30),
            onBreak: (_, breakDelay) =>
            {
                Console.WriteLine($"Circuito aberto por {breakDelay.TotalSeconds}s");
            },
            onReset: () => Console.WriteLine("Circuito fechado"),
            onHalfOpen: () => Console.WriteLine("Circuito meio-aberto"));
}