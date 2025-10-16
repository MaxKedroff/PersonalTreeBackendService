using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    

    [HttpGet]
    public IActionResult Get()
    {
        return Ok();
    }
}
