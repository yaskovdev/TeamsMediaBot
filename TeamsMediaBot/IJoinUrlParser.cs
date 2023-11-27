namespace TeamsMediaBot;

using Microsoft.Graph;

public interface IJoinUrlParser
{
    ChatInfo ExtractChatInfo(Uri joinUrl);

    MeetingInfo ExtractMeetingInfo(Uri joinUrl);
}
