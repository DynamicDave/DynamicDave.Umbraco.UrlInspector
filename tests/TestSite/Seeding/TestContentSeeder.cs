using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace TestSite.Seeding;

public class TestContentSeeder(
    IRuntimeState runtimeState,
    IContentService contentService,
    IContentTypeService contentTypeService,
    ILanguageService languageService,
    ITemplateService templateService,
    IRedirectUrlService redirectUrlService,
    IDomainService domainService,
    IShortStringHelper shortStringHelper,
    ILogger<TestContentSeeder> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static readonly Guid SuperUser = Constants.Security.SuperUserKey;

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (runtimeState.Level != RuntimeLevel.Run || contentService.GetRootContent().Any())
        {
            return; // not ready, or already seeded
        }

        logger.LogInformation("Seeding test content");
        await EnsureLanguagesAsync();
        var type = await EnsureContentTypeAsync();

        var home = Create("Home", "Home", null, type, publish: false);
        await EnsureDomainsAsync(home);
        Publish(home);
        var diensten = Create("Diensten", "Services", home, type, publish: true);
        redirectUrlService.Register("/website-laten-maken", diensten.Key, "nl-NL");
        redirectUrlService.Register("/diensten/website", diensten.Key, "nl-NL");
        Create("Concept", "Draft", home, type, publish: false);
    }

    private async Task EnsureDomainsAsync(IContent home)
    {
        var result = await domainService.UpdateDomainsAsync(
            home.Key,
            new DomainsUpdateModel
            {
                DefaultIsoCode = "nl-NL",
                Domains =
                [
                    new DomainModel { DomainName = "localhost:44414/nl", IsoCode = "nl-NL" },
                    new DomainModel { DomainName = "localhost:44414/en", IsoCode = "en-US" },
                ],
            });
        LogIfFailed("Assign domains to Home", result);
    }

    private void Publish(IContent content)
    {
        var result = contentService.Publish(content, ["*"]);
        if (!result.Success)
        {
            logger.LogWarning("Publishing {Name} failed: {Result}", content.Name, result.Result);
        }
    }

    private void LogIfFailed(string action, dynamic attempt)
    {
        if (!attempt.Success)
        {
            logger.LogWarning("{Action} failed: {Status}", action, (object?)attempt.Status);
        }
    }

    private async Task EnsureLanguagesAsync()
    {
        if (await languageService.GetAsync("en-US") is null)
        {
            LogIfFailed("Create language en-US", await languageService.CreateAsync(new Language("en-US", "English (United States)") { IsDefault = true }, SuperUser));
        }

        var dutch = await languageService.GetAsync("nl-NL");
        if (dutch is null)
        {
            dutch = new Language("nl-NL", "Dutch (Netherlands)");
            LogIfFailed("Create language nl-NL", await languageService.CreateAsync(dutch, SuperUser));
            dutch = await languageService.GetAsync("nl-NL");
        }

        if (dutch is { IsDefault: false })
        {
            dutch.IsDefault = true;
            LogIfFailed("Update language nl-NL", await languageService.UpdateAsync(dutch, SuperUser));
        }
    }

    private async Task<IContentType> EnsureContentTypeAsync()
    {
        var existing = contentTypeService.Get("testPage");
        if (existing is not null)
        {
            return existing;
        }

        var template = (await templateService.CreateAsync(
            "Test Page", "testPage",
            "@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage\n<h1>@Model.Name</h1>",
            SuperUser)).Result;

        var type = new ContentType(shortStringHelper, Constants.System.Root)
        {
            Alias = "testPage",
            Name = "Test Page",
            AllowedAsRoot = true,
            Variations = ContentVariation.Culture,
            AllowedTemplates = template is null ? [] : [template],
        };
        if (template is not null)
        {
            type.SetDefaultTemplate(template);
        }

        type.AllowedContentTypes = [new ContentTypeSort(type.Key, 0, type.Alias)];
        LogIfFailed("Create content type testPage", await contentTypeService.CreateAsync(type, SuperUser));
        return type;
    }

    private IContent Create(string nl, string en, IContent? parent, IContentType type, bool publish)
    {
        var content = parent is null
            ? contentService.Create(nl, Constants.System.Root, type)
            : contentService.Create(nl, parent.Key, type.Alias);
        content.SetCultureName(nl, "nl-NL");
        content.SetCultureName(en, "en-US");
        contentService.Save(content);
        if (publish)
        {
            Publish(content);
        }

        return content;
    }
}
