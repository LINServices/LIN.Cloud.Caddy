using LIN.Cloud.Caddy.Models;
using LIN.Cloud.Caddy.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LIN.Cloud.Caddy.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AuthController(IApiKeyService apiKeyService) : ControllerBase
{
    [HttpGet("keys")]
    public async Task<IActionResult> GetKeys()
    {
        var keys = await apiKeyService.GetAllKeys();
        return Ok(keys);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] KeyGenerationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest("Description is required.");

        var newKey = await apiKeyService.GenerateKey(request.Description);
        return Ok(newKey);
    }

    [HttpDelete("deactivate/{key}")]
    public async Task<IActionResult> Deactivate(string key)
    {
        var result = await apiKeyService.DeactivateKey(key);
        if (!result)
            return NotFound("Key not found.");

        return Ok("Key deactivated.");
    }
}