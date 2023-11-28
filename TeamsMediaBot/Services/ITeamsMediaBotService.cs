namespace TeamsMediaBot.Services;

using Microsoft.Skype.Bots.Media;

public interface ITeamsMediaBotService
{
    Task<IVideoSocket> JoinCall(Uri joinUrl);

    Task<HttpResponseMessage> ProcessCallNotification(HttpRequestMessage notification);
}
