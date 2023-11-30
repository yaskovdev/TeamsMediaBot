namespace BrowserAudioVideoCapturingService;

using PuppeteerSharp;

public class CapturingService
{
    private readonly IPage _extensionPage;

    public CapturingService(IPage extensionPage)
    {
        _extensionPage = extensionPage;
    }

    public async Task StartCapturing() => await _extensionPage.EvaluateFunctionAsync("START_RECORDING", new StartRecordingSettings());

    public async Task StopCapturing() => await _extensionPage.EvaluateFunctionAsync("STOP_RECORDING");
}
