namespace TestService;

internal class AspNetCoreStartup
{
    public void Configure(IApplicationBuilder app) => app.Run(async (context) =>
    {
        await context.Response.WriteAsync("Hello World!");
    });
}
