namespace TeamsMediaBot;

using System.Web;
using Microsoft.Graph;
using Newtonsoft.Json;

public class JoinUrlParser : IJoinUrlParser
{
    public ChatInfo ExtractChatInfo(Uri joinUrl)
    {
        var context = ExtractContext(joinUrl);
        return new ChatInfo { ThreadId = UrlSegment(joinUrl, 3), MessageId = UrlSegment(joinUrl, 4), ReplyChainMessageId = context.MessageId };
    }

    public MeetingInfo ExtractMeetingInfo(Uri joinUrl)
    {
        var context = ExtractContext(joinUrl);
        var meetingOrganizerIdentity = new Identity { Id = context.Oid };
        meetingOrganizerIdentity.SetTenantId(context.Tid);
        return new OrganizerMeetingInfo
        {
            Organizer = new IdentitySet { User = meetingOrganizerIdentity }
        };
    }

    private static Context ExtractContext(Uri joinUrl)
    {
        var queryParams = HttpUtility.ParseQueryString(joinUrl.Query);
        var contextAsJson = queryParams.Get("context") ?? throw new Exception("Join URL must have a context query param");
        var context = JsonConvert.DeserializeObject<Context>(contextAsJson) ?? throw new Exception("Cannot deserialize context from join URL");
        return context;
    }

    private static string UrlSegment(Uri joinUrl, int index) => HttpUtility.UrlDecode(joinUrl.Segments[index].TrimEnd('/'));
}
