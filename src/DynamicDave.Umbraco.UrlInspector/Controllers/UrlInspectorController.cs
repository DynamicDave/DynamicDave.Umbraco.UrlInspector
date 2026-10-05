using Asp.Versioning;
using DynamicDave.Umbraco.UrlInspector.Models;
using DynamicDave.Umbraco.UrlInspector.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace DynamicDave.Umbraco.UrlInspector.Controllers
{
    [ApiVersion("1.0")]
    [ApiExplorerSettings(GroupName = "UrlInspector")]
    public class UrlInspectorController(UrlInspectorService service) : DynamicDaveUmbracoUrlInspectorApiControllerBase
    {
        [HttpGet("document/{key:guid}")]
        [ProducesResponseType<UrlInspectorResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUrlInspector(Guid key)
        {
            var result = await service.InspectAsync(key);
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost("test-url")]
        [ProducesResponseType<TestUrlResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> TestUrl([FromBody] TestUrlRequest body, [FromQuery, BindRequired] Guid documentKey)
        {
            var inspected = await service.InspectAsync(documentKey);
            if (inspected is null) return NotFound();

            // Allow-list is derived server-side only (document URLs + configured application URL);
            // never from the request Host header or client input.
            var hosts = service.GetAllowedAuthorities(inspected);
            return Ok(await service.TestAsync(body.Url, hosts));

        }
    }
}
