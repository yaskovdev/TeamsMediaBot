namespace TeamsMediaBot.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using Services;

[ApiController]
public class JoinCallController : ControllerBase
{
    private readonly ITeamsMediaBotService _service;

    public JoinCallController(ITeamsMediaBotService service)
    {
        _service = service;
    }

    [HttpPost("/api/join-call-requests")]
    public async Task Get([FromBody] JoinCallRequest request)
    {
        await _service.JoinCall(new Uri(request.JoinUrl));
    }

    [HttpPost("/api/calls")]
    public async Task<HttpResponseMessage> ProcessCallNotification()
    {
        var httpRequestMessageFeature = new HttpRequestMessageFeature(Request.HttpContext);
        return await _service.ProcessCallNotification(httpRequestMessageFeature.HttpRequestMessage);
    }
}
