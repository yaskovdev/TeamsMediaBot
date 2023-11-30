namespace TeamsMediaBot.Services;

public interface ITeamsMediaBotService
{
    Task JoinCall(Uri joinUrl);

    Task<HttpResponseMessage> ProcessCallNotification(HttpRequestMessage notification);
}
