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
    public async Task<Dictionary<string, string>> Get([FromBody] JoinCallRequest request)
    {
        var call = await _service.JoinCall(new Uri(request.JoinUrl));
        return new Dictionary<string, string>
        {
            { "callId", call.Id },
            { "scenarioId", call.ScenarioId.ToString() }
        };
    }

    [HttpPost("/api/calls")]
    public async Task<HttpResponseMessage> ProcessCallNotification()
    {
        var httpRequestMessageFeature = new HttpRequestMessageFeature(Request.HttpContext);
        return await _service.ProcessCallNotification(httpRequestMessageFeature.HttpRequestMessage);
    }
}
