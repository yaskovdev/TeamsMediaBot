namespace TeamsMediaBot.Services;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Skype.Bots.Media;

public class TeamsMediaBotService : ITeamsMediaBotService, IAsyncDisposable
{
    private static readonly VideoFormat SupportedSendVideoFormat = VideoFormat.NV12_1280x720_30Fps; // TODO: try switching to H.264-encoded frames

    private readonly IJoinUrlParser _joinUrlParser;
    private readonly ICommunicationsClient _communicationsClient;
    private readonly ILogger<TeamsMediaBotService> _logger;

    private readonly ConcurrentDictionary<string, StreamingSession> _callIdToStreamingSession = new();

    public TeamsMediaBotService(IConfiguration config, IJoinUrlParser joinUrlParser, ILogger<TeamsMediaBotService> logger,
        IRequestAuthenticationProvider requestAuthenticationProvider, IMediaPlatformLogger mediaPlatformLogger, IGraphLogger graphLogger)
    {
        logger.LogInformation("Creating a new instance");
        var publicMediaUrl = new Uri(config["PublicMediaUrl"] ?? "");
        var mediaPlatformSettings = new MediaPlatformSettings
        {
            MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings
            {
                ServiceFqdn = publicMediaUrl.Host,
                CertificateThumbprint = config["CertificateThumbprint"],
                InstanceInternalPort = int.Parse(config["MediaProcessorEndpointInternalPort"] ?? "", CultureInfo.InvariantCulture),
                InstancePublicIPAddress = Dns.GetHostEntry(publicMediaUrl.Host).AddressList[0],
                InstancePublicPort = publicMediaUrl.Port
            },
            ApplicationId = config["AppId"],
            MediaPlatformLogger = mediaPlatformLogger
        };
        _joinUrlParser = joinUrlParser;
        _communicationsClient = new CommunicationsClientBuilder(config["AppName"], config["AppId"], graphLogger)
            .SetNotificationUrl(new Uri(config["NotificationUrl"] ?? ""))
            .SetServiceBaseUrl(new Uri(config["ServiceBaseUrl"] ?? ""))
            .SetAuthenticationProvider(requestAuthenticationProvider)
            .SetMediaPlatformSettings(mediaPlatformSettings)
            .Build();
        _communicationsClient.Calls().OnUpdated += OnUpdated;
        _logger = logger;
    }

    private async void OnUpdated(ICallCollection sender, CollectionEventArgs<ICall> e) => await DisposeOfSessions(e.RemovedResources.Select(it => it.Id));

    public async Task<ICall> JoinCall(Uri joinUrl)
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
            ReceiveColorFormat = VideoColorFormat.NV12,
            SupportedSendVideoFormats = ImmutableList.Create(SupportedSendVideoFormat)
        };
        var mediaSession = _communicationsClient.CreateMediaSession(audioSocketSettings, videoSocketSettings);
        var joinParams = new JoinMeetingParameters(chatInfo, meetingInfo, mediaSession);
        var streamingSession = new StreamingSession(mediaSession, SupportedSendVideoFormat);
        var call = await _communicationsClient.Calls().AddAsync(joinParams, Guid.NewGuid());
        _callIdToStreamingSession[call.Id] = streamingSession;
        return call;
    }

    public async Task<HttpResponseMessage> ProcessCallNotification(HttpRequestMessage notification) =>
        await _communicationsClient.ProcessNotificationAsync(notification);

    public async ValueTask DisposeAsync()
    {
        await DisposeOfSessions(_callIdToStreamingSession.Keys);
        _logger.LogInformation("Terminating Media Platform and disposing of communications client, it may take some time");
        await _communicationsClient.TerminateAsync(false); // TODO: MediaPlatform initialized in SetMediaPlatformSettings prevents app shutdown otherwise
        _communicationsClient.Dispose();
    }

    private async Task DisposeOfSessions(IEnumerable<string> callIds)
    {
        foreach (var callId in callIds)
        {
            if (_callIdToStreamingSession.TryRemove(callId, out var session))
            {
                _logger.LogInformation("Disposing of session with call ID {Id}", callId);
                await session.DisposeAsync(); // TODO: the problem now is that it does not wait for this to return when called from TeamsMediaBotService.OnUpdated and disposes of the sockets...
            }
        }
    }
}
