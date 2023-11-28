namespace TeamsMediaBot.Services;

using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Skype.Bots.Media;

public class TeamsMediaBotService : ITeamsMediaBotService
{
    private static readonly IList<VideoFormat> SupportedSendVideoFormats = ImmutableList.Create(VideoFormat.NV12_1920x1080_30Fps);

    private readonly IJoinUrlParser _joinUrlParser;
    private readonly ICommunicationsClient _communicationsClient;

    public TeamsMediaBotService(IConfiguration config, IJoinUrlParser joinUrlParser, IRequestAuthenticationProvider requestAuthenticationProvider,
        IMediaPlatformLogger mediaPlatformLogger, IGraphLogger graphLogger)
    {
        var publicMediaUrl = new Uri(config["PublicMediaUrl"]);
        var mediaPlatformSettings = new MediaPlatformSettings
        {
            MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings
            {
                ServiceFqdn = publicMediaUrl.Host,
                CertificateThumbprint = config["CertificateThumbprint"],
                InstanceInternalPort = int.Parse(config["MediaProcessorEndpointInternalPort"], CultureInfo.InvariantCulture),
                InstancePublicIPAddress = Dns.GetHostEntry(publicMediaUrl.Host).AddressList[0],
                InstancePublicPort = publicMediaUrl.Port
            },
            ApplicationId = config["AppId"],
            MediaPlatformLogger = mediaPlatformLogger
        };
        _joinUrlParser = joinUrlParser;
        _communicationsClient = new CommunicationsClientBuilder(config["AppName"], config["AppId"], graphLogger)
            .SetNotificationUrl(new Uri(config["NotificationUrl"]))
            .SetServiceBaseUrl(new Uri(config["ServiceBaseUrl"]))
            .SetAuthenticationProvider(requestAuthenticationProvider)
            .SetMediaPlatformSettings(mediaPlatformSettings)
            .Build();
    }

    public async Task<IVideoSocket> JoinCall(Uri joinUrl)
    {
        var chatInfo = _joinUrlParser.ExtractChatInfo(joinUrl);
        var meetingInfo = _joinUrlParser.ExtractMeetingInfo(joinUrl);
        var audioSocketSettings = new AudioSocketSettings
        {
            StreamDirections = StreamDirection.Sendonly,
            SupportedAudioFormat = AudioFormat.Pcm16K
        };
        var videoSocketSettings = new VideoSocketSettings
        {
            StreamDirections = StreamDirection.Sendonly,
            ReceiveColorFormat = VideoColorFormat.H264,
            SupportedSendVideoFormats = SupportedSendVideoFormats
        };
        var mediaSession = _communicationsClient.CreateMediaSession(audioSocketSettings, videoSocketSettings);
        var joinParams = new JoinMeetingParameters(chatInfo, meetingInfo, mediaSession);
        await _communicationsClient.Calls().AddAsync(joinParams, Guid.NewGuid());
        return mediaSession.VideoSockets[0];
    }

    public async Task<HttpResponseMessage> ProcessCallNotification(HttpRequestMessage notification) =>
        await _communicationsClient.ProcessNotificationAsync(notification);
}
