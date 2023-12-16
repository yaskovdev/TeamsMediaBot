using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Web;

namespace TeamsMediaBot.Services;

using Microsoft.Identity.Client;

using System.Collections.Immutable;
using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common;

public class AuthenticationProvider : IRequestAuthenticationProvider
{
    private const string Scheme = "Bearer";
    private const string Authority = "https://login.microsoftonline.com/{0}";

    private readonly string _tenantId;
    private readonly string _authResource;
    private readonly IConfidentialClientApplication _app;

    public AuthenticationProvider(IConfiguration config)
    {
        _tenantId = config["TenantId"] ?? "";
        var appId = config["AppId"] ?? "";
        var appSecret = config["AppSecret"] ?? "";
        _authResource = config["AuthResource"] ?? "";
        var authority = ImmutableList.Create(_tenantId, "common")
            .Where(it => !string.IsNullOrWhiteSpace(it))
            .Select(it => string.Format(CultureInfo.InvariantCulture, Authority, it))
            .First();
        _app = ConfidentialClientApplicationBuilder.Create(appId)
            .WithClientSecret(appSecret)
            .WithAuthority(authority)
            .Build();
        AddTokenCache(_app);
    }

    public async Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenant)
    {
        var authResult = await AcquireToken(_app, _authResource);
        request.Headers.Authorization = new AuthenticationHeaderValue(Scheme, authResult.AccessToken);
    }

    private static void AddTokenCache(IConfidentialClientApplication app) =>
        app.AddInMemoryTokenCache(services => services.Configure<MemoryCacheOptions>(options => options.SizeLimit = 1024 * 1024 * 1024));

    // TODO: implement properly
    public async Task<RequestValidationResult> ValidateInboundRequestAsync(HttpRequestMessage request)
    {
        request.Options.Set(new HttpRequestOptionsKey<string>(HttpConstants.HeaderNames.Tenant), _tenantId);
        return await Task.FromResult(new RequestValidationResult { IsValid = true, TenantId = _tenantId });
    }

    private static async Task<AuthenticationResult> AcquireToken(IConfidentialClientApplication app, string resource) =>
        await app.AcquireTokenForClient(new[] { $"{resource}/.default" })
            .ExecuteAsync()
            .ConfigureAwait(false);
}
