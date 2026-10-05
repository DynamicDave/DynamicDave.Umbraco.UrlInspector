using System.Net;
using System.Net.Sockets;
using DynamicDave.Umbraco.UrlInspector.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace DynamicDave.Umbraco.UrlInspector.Composers;

public class UrlInspectorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<UrlInspectorService>();

        // Private network targets (localhost, 10.x, 192.168.x, 169.254.169.254, ...) are blocked unless configured,
        // or the site runs in the Development environment, where the site itself usually lives on localhost.
        var allowPrivate = builder.Config.GetValue<bool?>(Constants.AllowPrivateNetworkTargetsKey);

        // Redirects are never followed: the tester reports the first hop only.
        builder.Services.AddHttpClient(Constants.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var allow = allowPrivate ?? sp.GetRequiredService<IHostEnvironment>().IsDevelopment();
                return new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    ConnectCallback = allow ? null : ConnectToPublicAddressAsync,
                };
            });
    }

    // Resolves the host and connects only to addresses outside the blocked ranges. Checking the resolved address
    // (not the host name) also covers DNS names and DNS rebinding that point at internal addresses.
    private static async ValueTask<Stream> ConnectToPublicAddressAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        var allowed = addresses.Where(a => !PrivateNetwork.IsBlocked(a)).ToArray();
        if (allowed.Length == 0) throw new PrivateNetworkBlockedException(host);

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
