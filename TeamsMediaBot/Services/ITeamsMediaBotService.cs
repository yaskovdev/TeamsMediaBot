namespace TeamsMediaBot.Services;

using Microsoft.Graph.Communications.Calls;

public interface ITeamsMediaBotService
{
    Task<ICall> JoinCall(Uri joinUrl);

    Task<HttpResponseMessage> ProcessCallNotification(HttpRequestMessage notification);
}
