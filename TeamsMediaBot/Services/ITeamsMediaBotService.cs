namespace TeamsMediaBot.Services;

using Microsoft.Skype.Bots.Media;

public interface ITeamsMediaBotService
{
    Task<IVideoSocket> JoinCall(Uri joinUrl);
}
