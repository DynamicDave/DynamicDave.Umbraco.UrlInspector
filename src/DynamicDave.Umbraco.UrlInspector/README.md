# DynamicDave.Umbraco.UrlInspector

Adds a URLs & Redirects view to document workspaces: all URLs per culture, incoming and outgoing redirects, the canonical URL, and a tester that requests a URL and reports the status code and redirect target.

## Install

    dotnet add package DynamicDave.Umbraco.UrlInspector

Supported Umbraco version: **17.x** (net10.0). The backoffice UI is available in English, Dutch, German, French and Danish.

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

Without it, relative URLs report "No base URL" until the application URL is known.

Set it explicitly. With `Umbraco:CMS:WebRouting:ApplicationUrlDetection` set to `EveryRequest` or `FirstRequest` the application host can be taken from the request Host header, which a client can influence (host header poisoning); that host feeds the tester's allow-list.

## Security note

The inspect, test and node-id endpoints require Content-section access but do not check per-document (start-node or permission) access. Any backoffice user with Content access can inspect or test any document key they know.

## Test behaviour and limitations

- The tester only contacts an allow-list of authorities (host and effective port, so `https://x` equals `https://x:443`): those of the document's own absolute URLs plus the application URL. Anything else, including another port or scheme on the same host, is refused.
- Only `http` and `https` are allowed. The tester sends a HEAD request, with a short timeout, and never follows redirects: you see the redirect status and `Location` instead.
- Redirect sources stored with a domain-id prefix are not detected for the outgoing redirect check.
- Read-only: no content or redirects are changed.

## License

MIT
