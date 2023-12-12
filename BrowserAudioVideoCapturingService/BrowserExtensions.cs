namespace BrowserAudioVideoCapturingService;

using PuppeteerSharp;

public static class BrowserExtensions
{
    public static async Task StartCapturing(this IBrowser browser, StartRecordingSettings settings)
    {
        var extensionPage = await browser.ExtensionPage();
        await extensionPage.EvaluateFunctionAsync("START_RECORDING", settings);
    }

    public static async Task StopCapturing(this IBrowser browser)
    {
        var extensionPage = await browser.ExtensionPage();
        await extensionPage.EvaluateFunctionAsync("STOP_RECORDING");
    }

    public static async Task<IPage> ExtensionPage(this IBrowser browser)
    {
        var extensionTarget = await browser.WaitForTargetAsync(IsExtensionBackgroundPage);
        return await extensionTarget.PageAsync();
    }

    private static bool IsExtensionBackgroundPage(ITarget target) =>
        target.Type == TargetType.BackgroundPage && target.Url.StartsWith($"chrome-extension://{ExtensionConstants.ExtensionId}");
}
