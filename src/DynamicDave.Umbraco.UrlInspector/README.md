# DynamicDave.Umbraco.UrlInspector

Adds a URLs & Redirects view to document workspaces: all URLs per culture, incoming and outgoing redirects, the canonical URL, and a tester that requests a URL and reports the status code and redirect target.

## Install

    dotnet add package DynamicDave.Umbraco.UrlInspector

Supported Umbraco versions: **17.x and 18.x** (net10.0), tested on 17.0.0 and 18.2.1. The backoffice UI is available in English, Dutch, German, French and Danish.

## Configuration

Testing a relative URL (for example `/en/services/`) needs a base URL. Set the application URL in `appsettings.json`:

```json
{
  "Umbraco": {
    "CMS": {
      "WebRouting": {
        "UmbracoApplicationUrl": "https://www.example.com"
      }
    }
  }
}
```

Without it, relative URLs report "No base URL" until the application URL is known. Umbraco 18 returns relative URLs more often than 17 (for URLs on the domain of the current request), so set it there in particular.

Set it explicitly. With `Umbraco:CMS:WebRouting:ApplicationUrlDetection` set to `EveryRequest` or `FirstRequest` the application host can be taken from the request Host header, which a client can influence (host header poisoning); that host feeds the tester's allow-list.

### Internal network targets

On a production server the tester refuses addresses on internal networks (localhost, `10.x`, `172.16-31.x`, `192.168.x`, link-local such as the cloud metadata address `169.254.169.254`, and their IPv6 equivalents). The check runs on the resolved IP address, so host names that point at internal addresses are refused too.

In the `Development` environment internal addresses are allowed, because a local site usually runs on `localhost`. Override this in either direction with:

```json
{
  "DynamicDave": {
    "UrlInspector": {
      "AllowPrivateNetworkTargets": true
    }
  }
}
```

## Security note

The inspect and test endpoints require Content-section access plus Browse access to the document, the same check Umbraco uses for its own document endpoints (start nodes and user group permissions).

## Test behaviour and limitations

- The tester only contacts an allow-list of authorities (host and effective port, so `https://x` equals `https://x:443`): those of the document's own absolute URLs plus the application URL. Anything else, including another port or scheme on the same host, is refused.
- Only `http` and `https` are allowed. The tester sends a HEAD request, with a short timeout, and never follows redirects: you see the redirect status and `Location` instead.
- Redirect sources stored with a domain-id prefix are not detected for the outgoing redirect check.
- Read-only: no content or redirects are changed.

## License

MIT
