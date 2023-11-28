namespace TeamsMediaBot.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Microsoft.Skype.Bots.Media;
using Services;

[ApiController]
public class JoinCallController : ControllerBase
{
    private readonly ITeamsMediaBotService _service;
    private readonly ILogger<JoinCallController> _logger;

    // TODO: implement streaming properly
    private static bool _ready;

    public JoinCallController(ITeamsMediaBotService service, ILogger<JoinCallController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("/api/join-call-requests")]
    public async Task Get([FromBody] JoinCallRequest request)
    {
        var videoSocket = await _service.JoinCall(new Uri(request.JoinUrl));
        videoSocket.VideoSendStatusChanged += OnVideoSendStatusChanged;
        long timestamp = 0;
        while (true)
        {
            if (_ready)
            {
                for (var i = 0;; i++)
                {
                    try
                    {
                        var packet = await System.IO.File.ReadAllBytesAsync($@"c:\dev\experiment\{i + 1:D7}.raw");
                        videoSocket.Send(new VideoBuffer(packet, timestamp));
                        timestamp += 1000;
                    }
                    catch (FileNotFoundException)
                    {
                        var isFirstFrameMissing = i == 0;
                        if (isFirstFrameMissing)
                        {
                            throw;
                        }
                        break;
                    }
                }
            }
        }
    }

    [HttpPost("/api/calls")]
    public async Task<HttpResponseMessage> ProcessCallNotification()
    {
        var httpRequestMessageFeature = new HttpRequestMessageFeature(Request.HttpContext);
        return await _service.ProcessCallNotification(httpRequestMessageFeature.HttpRequestMessage);
    }

    private void OnVideoSendStatusChanged(object? sender, VideoSendStatusChangedEventArgs args)
    {
        _logger.LogInformation("New socket status: {Status}", args.MediaSendStatus);
        _ready = args.MediaSendStatus == MediaSendStatus.Active;
    }
}
