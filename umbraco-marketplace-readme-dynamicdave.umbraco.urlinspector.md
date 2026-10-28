# Url Inspector

Everything about a page's URLs in one place. Url Inspector adds a **URLs & Redirects** view to every document.

## Features

- All URLs of the page, per language
- Incoming redirects (old URLs that point here) and the outgoing redirect
- The canonical URL
- A URL tester that requests a URL and reports the status code and redirect target, without following redirects
- Safe by default: the tester refuses internal network addresses on production servers

## Compatibility

- Umbraco **17 and 18** (.NET 10)
- Backoffice UI in English, Dutch, German, French and Danish
- Free and open source (MIT)

## Getting started

```
dotnet add package DynamicDave.Umbraco.UrlInspector
```

Set `Umbraco:CMS:WebRouting:UmbracoApplicationUrl` so relative URLs can be tested. Full documentation: [README on GitHub](https://github.com/DynamicDave/DynamicDave.Umbraco.UrlInspector/blob/main/src/DynamicDave.Umbraco.UrlInspector/README.md).
