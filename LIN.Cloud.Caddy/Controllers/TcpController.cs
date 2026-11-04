using LIN.Cloud.Caddy.Models;
using LIN.Cloud.Caddy.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LIN.Cloud.Caddy.Controllers;

/// <summary>
/// Servicios TCP crudos (Postgres, MongoDB, Redis...) publicados por Caddy mediante layer4.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TcpController(ITcpRouteService tcpRouteService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] TcpRegistrationRequest request)
    {
        var (success, message) = await tcpRouteService.CreateRegistration(request.Id, request.ListenPort, request.Host, request.Port, request.Target, request.Protocol, request.TerminateTls, request.IsActive);
        if (!success)
            return BadRequest(message);

        return Ok(message);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await tcpRouteService.GetAll());
    }

    [HttpPatch("{id}/state")]
    public async Task<IActionResult> UpdateState(string id, [FromBody] UpdateStateRequest request)
    {
        var (success, message) = await tcpRouteService.UpdateState(id, request.IsActive);
        if (!success)
            return BadRequest(message);

        return Ok(message);
    }

    [HttpDelete("remove/{id}")]
    public async Task<IActionResult> Remove(string id)
    {
        var (success, message) = await tcpRouteService.DeleteRegistration(id);
        if (!success)
            return BadRequest(message);

        return Ok(message);
    }

    [HttpPost("restore")]
    public async Task<IActionResult> Restore()
    {
        var (success, count) = await tcpRouteService.RestoreAll();
        if (!success)
            return StatusCode(500, "Failed to restore TCP servers to Caddy.");

        return Ok(new { Message = "Restore process completed.", Count = count });
    }
}
