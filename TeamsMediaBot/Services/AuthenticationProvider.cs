namespace TeamsMediaBot.Services;

using System.Collections.Immutable;
using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

public class AuthenticationProvider : IRequestAuthenticationProvider
{
    private const string Scheme = "Bearer";
    private const string Authority = "https://login.microsoftonline.com/{0}";

    private readonly string _tenantId;
    private readonly string _appId;
    private readonly string _appSecret;
    private readonly string _appResource;

    public AuthenticationProvider(IConfiguration config)
    {
        _tenantId = config["TenantId"] ?? "";
        _appId = config["AppId"] ?? "";
        _appSecret = config["AppSecret"] ?? "";
        _appResource = config["AuthResource"] ?? "";
    }

    public async Task AuthenticateOutboundRequestAsync(HttpRequestMessage request, string tenant)
    {
        var authority = ImmutableList.Create(_tenantId, tenant, "common")
            .Where(it => !string.IsNullOrWhiteSpace(it))
            .Select(it => string.Format(CultureInfo.InvariantCulture, Authority, it))
            .First();
        var credential = new ClientCredential(_appId, _appSecret);
        var result = await AcquireToken(authority, _appResource, credential);
        request.Headers.Authorization = new AuthenticationHeaderValue(Scheme, result.AccessToken);
    }

    // TODO: implement properly
    public async Task<RequestValidationResult> ValidateInboundRequestAsync(HttpRequestMessage request)
    {
        request.Options.Set(new HttpRequestOptionsKey<string>(HttpConstants.HeaderNames.Tenant), _tenantId);
        return await Task.FromResult(new RequestValidationResult { IsValid = true, TenantId = _tenantId });
    }

    private static async Task<AuthenticationResult> AcquireToken(string authority, string resource, ClientCredential credential)
    {
        var context = new AuthenticationContext(authority);
        return await context.AcquireTokenAsync(resource, credential);
    }
}
