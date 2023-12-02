namespace TeamsMediaBot;

public static class WebApplicationExtensions
{
    public static void InstantiateService(this WebApplication app, Type serviceType)
    {
        using var serviceScope = app.Services.CreateScope();
        serviceScope.ServiceProvider.GetRequiredService(serviceType);
    }
}
