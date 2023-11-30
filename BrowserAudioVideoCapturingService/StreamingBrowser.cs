namespace BrowserAudioVideoCapturingService;

using System.Reflection;
using Demuxer;
using PuppeteerSharp;

public class StreamingBrowser : IStreamingBrowser
{
    private const string ExtensionId = "jjndjgheafjngoipoacpjgeicjeomjli";

    private const string YouTubeVideoId = "IMyqasy2Lco";

    private const string ChromeExecutablePath = "C:/Program Files/Google/Chrome/Application/chrome.exe";

    public async Task<IBrowser> LaunchInstance(IBlockingStream stream)
    {
        Console.WriteLine("Starting...");

        var browser = await Puppeteer.LaunchAsync(ChromeLaunchOptions(ChromeExecutablePath));
        var extensionTarget = await browser.WaitForTargetAsync(IsExtensionBackgroundPage);
        var extensionPage = await extensionTarget.PageAsync();

        var pages = await browser.PagesAsync();
        var page = pages[0];
        await page.GoToAsync($"https://www.youtube.com/embed/{YouTubeVideoId}?autoplay=1&loop=1&playlist={YouTubeVideoId}");
        await page.SetViewportAsync(new ViewPortOptions { Width = Constants.Width, Height = Constants.Height });

        var capturingService = new CapturingService(extensionPage);

        await extensionPage.ExposeFunctionAsync<string, Task>("sendData", async data =>
        {
            Console.WriteLine($"Captured {data.Length / (double)1024:0.00} KB of media from the browser");
            stream.Write(ToByteArray(data));
            await Task.CompletedTask;
        });

        await capturingService.StartCapturing();

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
            $"--allowlisted-extension-id={ExtensionId}",
            // "--headless=new",
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

    private static byte[] ToByteArray(string buffer) => buffer.Select(c => (byte)c).ToArray();

    private static bool IsExtensionBackgroundPage(ITarget target) =>
        target.Type == TargetType.BackgroundPage && target.Url.StartsWith($"chrome-extension://{ExtensionId}");
}
