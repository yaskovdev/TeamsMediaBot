namespace BrowserAudioVideoCapturingService;

using System.Reflection;
using PuppeteerSharp;

public class BrowserLauncher
{
    private const string YouTubeVideoId = "8gR6uWsDaCI";

    private const string ChromeExecutablePath = "C:/Program Files/Google/Chrome/Application/chrome.exe";

    public async Task<IBrowser> LaunchInstance(int width, int height, int frameRate, Action<string> onMediaChunkReceived)
    {
        Console.WriteLine("Starting...");

        var browser = await Puppeteer.LaunchAsync(ChromeLaunchOptions(ChromeExecutablePath));

        var pages = await browser.PagesAsync();
        var page = pages[0];
        await page.GoToAsync($"https://www.youtube.com/embed/{YouTubeVideoId}?autoplay=1&loop=1&playlist={YouTubeVideoId}");
        await page.SetViewportAsync(new ViewPortOptions { Width = width, Height = height });

        var extensionPage = await browser.ExtensionPage();
        await extensionPage.ExposeFunctionAsync<string, Task>("sendData", async data =>
        {
            onMediaChunkReceived(data);
            await Task.CompletedTask;
        });

        await browser.StartCapturing(new StartRecordingSettings(width, height, frameRate));

        return browser;
    }

    private static LaunchOptions ChromeLaunchOptions(string chromeExecutablePath)
    {
        var extensionPath = GetResourcePath("Extension");
        var browserArgs = new[]
        {
            "--no-sandbox",
            "--autoplay-policy=no-user-gesture-required",
            $"--load-extension={extensionPath}",
            $"--disable-extensions-except={extensionPath}",
            $"--allowlisted-extension-id={ExtensionConstants.ExtensionId}",
            "--headless=new",
            "--hide-scrollbars"
        };
        return new LaunchOptions { Headless = false, Args = browserArgs, ExecutablePath = chromeExecutablePath };
    }

    private static string GetResourcePath(string name)
    {
        var location = Assembly.GetExecutingAssembly().Location;
        var uriBuilder = new UriBuilder(location);
        var path = Uri.UnescapeDataString(uriBuilder.Path);
        return Path.Combine(Path.GetDirectoryName(path) ?? "", name);
    }
}
