using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("/api/")]
public class WeatherForecastController : ControllerBase
{
    

    [HttpGet("test")]
    public IActionResult Get()
    {
        return Ok("��� - ��� ���");
    }
    [HttpPost("test")]
    public IActionResult Post()
    {
        return Ok("��� - ��� ����");
    }

    [HttpPut("test")]
    public IActionResult Put()
    {
        return Ok("��� - ��� ���");
    }

    [HttpDelete("test")]
    public IActionResult Delete()
    {
        return Ok("��� - ��� ������");
    }

}
