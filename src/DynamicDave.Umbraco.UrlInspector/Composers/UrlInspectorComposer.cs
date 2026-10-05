using DynamicDave.Umbraco.UrlInspector.Services;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace DynamicDave.Umbraco.UrlInspector.Composers;

public class UrlInspectorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<UrlInspectorService>();
        // Redirects are never followed: the tester reports the first hop only.
        builder.Services.AddHttpClient(Constants.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
    }
}
