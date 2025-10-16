using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("/api/")]
public class WeatherForecastController : ControllerBase
{
    

    [HttpGet("test")]
    public IActionResult Get()
    {
        return Ok("йоу - это гет");
    }
    [HttpPost("test")]
    public IActionResult Post()
    {
        return Ok("йоу - это пост");
    }

    [HttpPut("test")]
    public IActionResult Put()
    {
        return Ok("йоу - это пут");
    }

    [HttpDelete("test")]
    public IActionResult Delete()
    {
        return Ok("йоу - это делете");
    }

}
