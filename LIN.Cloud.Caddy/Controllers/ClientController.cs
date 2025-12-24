using LIN.Cloud.Caddy.Models;
using LIN.Cloud.Caddy.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LIN.Cloud.Caddy.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClientController(IRouteService routeService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegistrationRequest request)
    {
        var (success, message) = await routeService.CreateRegistration(request.Id, request.Host, request.Port);
        if (!success)
            return BadRequest(message);

        return Ok(message);
    }

    [HttpDelete("remove/{id}")]
    public async Task<IActionResult> Remove(string id)
    {
        var (success, message) = await routeService.DeleteRegistration(id);
        if (!success)
            return BadRequest(message);

        return Ok(message);
    }

    [HttpPost("restore")]
    public async Task<IActionResult> Restore()
    {
        var (success, count) = await routeService.RestoreAll();
        if (!success)
            return StatusCode(500, "Failed to restore routes to Caddy.");

        return Ok(new { Message = "Restore process completed.", Count = count });
    }

    [HttpGet("version")]
    public async Task<IActionResult> GetClientVersion()
    {
        var projectVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        var caddyVersion = await routeService.GetCaddyVersion();

        return Ok(new
        {
            ProjectVersion = projectVersion,
            CaddyVersion = caddyVersion
        });
    }
}