using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Skype.Bots.Media;
using TeamsMediaBot;
using TeamsMediaBot.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<IJoinUrlParser, JoinUrlParser>();
builder.Services.AddSingleton<IRequestAuthenticationProvider, AuthenticationProvider>();
builder.Services.AddSingleton<IMediaPlatformLogger, MediaPlatformLogger>();
builder.Services.AddSingleton<IGraphLogger, GraphLogger>();
builder.Services.AddSingleton<ITeamsMediaBotService, TeamsMediaBotService>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsEnvironment("Local") || app.Environment.IsEnvironment(Environments.Development))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/api/health");

app.InstantiateService(typeof(ITeamsMediaBotService));

app.Run();
