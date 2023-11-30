namespace TeamsMediaBot.Services;

using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using BrowserAudioVideoCapturingService;
using Demuxer;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Skype.Bots.Media;

public class TeamsMediaBotService : ITeamsMediaBotService
{
    private static readonly IList<VideoFormat> SupportedSendVideoFormats = ImmutableList.Create(VideoFormat.NV12_1920x1080_15Fps);

    private readonly IJoinUrlParser _joinUrlParser;
    private readonly IDemuxer _demuxer;
    private readonly ICommunicationsClient _communicationsClient;
    private readonly Thread _browserThread;
    private readonly ILogger<TeamsMediaBotService> _logger;

    private static bool _videoSocketReady;
    private static bool _audioSocketReady;

    public TeamsMediaBotService(IConfiguration config, IJoinUrlParser joinUrlParser, IDemuxer demuxer, IRequestAuthenticationProvider requestAuthenticationProvider,
        IMediaPlatformLogger mediaPlatformLogger, IGraphLogger graphLogger, ILogger<TeamsMediaBotService> logger)
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
        _demuxer = demuxer;
        _logger = logger;
        _communicationsClient = new CommunicationsClientBuilder(config["AppName"], config["AppId"], graphLogger)
            .SetNotificationUrl(new Uri(config["NotificationUrl"]))
            .SetServiceBaseUrl(new Uri(config["ServiceBaseUrl"]))
            .SetAuthenticationProvider(requestAuthenticationProvider)
            .SetMediaPlatformSettings(mediaPlatformSettings)
            .Build();
        _browserThread = new Thread(async () =>
        {
            var simulator = new StreamingBrowser(_demuxer);
            await simulator.Start();
        });
    }

    public async Task JoinCall(Uri joinUrl)
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
            SupportedSendVideoFormats = SupportedSendVideoFormats
        };
        var mediaSession = _communicationsClient.CreateMediaSession(audioSocketSettings, videoSocketSettings);
        var videoSocket = mediaSession.VideoSockets[0];
        videoSocket.VideoSendStatusChanged += OnVideoSendStatusChanged;
        var audioSocket = mediaSession.AudioSocket;
        audioSocket.AudioSendStatusChanged += OnAudioSendStatusChanged;
        var joinParams = new JoinMeetingParameters(chatInfo, meetingInfo, mediaSession);
        await _communicationsClient.Calls().AddAsync(joinParams, Guid.NewGuid());
        while (!_videoSocketReady || !_audioSocketReady)
        {
            _logger.LogInformation("Waiting to start the browser");
            Thread.Sleep(500);
        }
        _browserThread.Start();
        while (true)
        {
            var frame = _demuxer.ReadFrame();
            if (frame.Data.Count == 0)
            {
                break;
            }
            _logger.LogInformation("Extracted frame of type {Type} with size {Size} and timestamp {Timestamp}", frame.Type, frame.Data.Count, frame.Timestamp);
            if (frame.Type == FrameType.Video)
            {
                videoSocket.Send(new VideoBuffer(frame.Data.ToArray(), frame.Timestamp.Ticks));
            }
        }
    }

    public async Task<HttpResponseMessage> ProcessCallNotification(HttpRequestMessage notification) =>
        await _communicationsClient.ProcessNotificationAsync(notification);

    private void OnVideoSendStatusChanged(object? sender, VideoSendStatusChangedEventArgs args)
    {
        _logger.LogInformation("New video socket status: {Status}", args.MediaSendStatus);
        _videoSocketReady = args.MediaSendStatus == MediaSendStatus.Active;
    }

    private void OnAudioSendStatusChanged(object? sender, AudioSendStatusChangedEventArgs args)
    {
        _logger.LogInformation("New audio socket status: {Status}", args.MediaSendStatus);
        _audioSocketReady = args.MediaSendStatus == MediaSendStatus.Active;
    }
}
