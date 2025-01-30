using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Services;
using NotificationService.Domain.Interfaces;
using NotificationService.Infraestructure.Email;
using NotificationService.Infraestructure.Messaging;
using NotificationService.Infraestructure.Persistence;
using System.Net;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);


var smtpHost = builder.Configuration["Smtp:Host"];
var smtpPort = int.Parse(builder.Configuration["Smtp:Port"]);
var smtpUser = builder.Configuration["Smtp:Username"];
var smtpPass = builder.Configuration["Smtp:Password"];
var fromEmail = builder.Configuration["Smtp:From"];

builder.Services
   .AddFluentEmail(fromEmail)
   .AddRazorRenderer()
   .AddSmtpSender(new SmtpClient(smtpHost, smtpPort)
   {
       Credentials = new NetworkCredential(smtpUser, smtpPass),
       EnableSsl = true
   });


builder.Services.AddSingleton<RabbitMqNotificationListener>();

builder.Services.AddScoped<IEmailSender, FluentEmailSender>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationEmailService, NotificationEmailService>();


builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
