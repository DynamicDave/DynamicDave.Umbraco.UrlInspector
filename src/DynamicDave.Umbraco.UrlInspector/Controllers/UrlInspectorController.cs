using Asp.Versioning;
using DynamicDave.Umbraco.UrlInspector.Models;
using DynamicDave.Umbraco.UrlInspector.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Extensions;

namespace DynamicDave.Umbraco.UrlInspector.Controllers
{
    [ApiVersion("1.0")]
    [ApiExplorerSettings(GroupName = "UrlInspector")]
    public class UrlInspectorController(UrlInspectorService service, IAuthorizationService authorizationService) : DynamicDaveUmbracoUrlInspectorApiControllerBase
    {
        [HttpGet("document/{key:guid}")]
        [ProducesResponseType<UrlInspectorResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUrlInspector(Guid key)
        {
            if (!await CanBrowseAsync(key)) return StatusCode(StatusCodes.Status403Forbidden);

            var result = await service.InspectAsync(key);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("test-url")]
        [ProducesResponseType<TestUrlResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> TestUrl([FromBody] TestUrlRequest body, [FromQuery, BindRequired] Guid documentKey)
        {
            if (!await CanBrowseAsync(documentKey)) return StatusCode(StatusCodes.Status403Forbidden);

            var inspected = await service.InspectAsync(documentKey);
            if (inspected is null) return NotFound();

            // Allow-list is derived server-side only (document URLs + configured application URL);
            // never from the request Host header or client input.
            var hosts = service.GetAllowedAuthorities(inspected);
            return Ok(await service.TestAsync(body.Url, hosts, HttpContext.RequestAborted));

        }

        // Same check as Umbraco's own document endpoints: start nodes and the Browse permission of the user's groups.
        private async Task<bool> CanBrowseAsync(Guid key)
        {
            var result = await authorizationService.AuthorizeResourceAsync(
                User,
                ContentPermissionResource.WithKeys(ActionBrowse.ActionLetter, key),
                AuthorizationPolicies.ContentPermissionByResource);
            return result.Succeeded;
        }
    }
}
