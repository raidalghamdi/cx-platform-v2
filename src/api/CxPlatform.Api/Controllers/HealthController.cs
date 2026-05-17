using CxPlatform.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CxPlatform.Api.Controllers;

[ApiController]
[Route("api/healthz")]
public class HealthController : ControllerBase
{
    private const string Version = "v2-phase0-2026.05.17";

    [HttpGet]
    public ActionResult<HealthDto> Get() =>
        Ok(new HealthDto(true, Version, DateTime.UtcNow));
}
