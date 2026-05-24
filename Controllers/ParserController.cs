using Microsoft.AspNetCore.Mvc;
using SneakerAgregator.Services.ParserService;

namespace SneakerAgregator.Controllers;

[ApiController]
[Route("api/parser")]
public class ParserController(StreetBeatParser streetBeatParser, BrandshopParser brandshopParser) : ControllerBase
{
    [HttpPost("streetbeat")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RunStreetBeat()
    {
        try
        {
            await streetBeatParser.ParseAsync();
            return Ok(new { message = "Street Beat: парсинг завершён" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, detail = ex.InnerException?.Message });
        }
    }

    [HttpPost("brandshop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RunBrandshop()
    {
        try
        {
            await brandshopParser.ParseAsync();
            return Ok(new { message = "Brandshop: парсинг завершён" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, detail = ex.InnerException?.Message });
        }
    }

    [HttpPost("all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RunAll()
    {
        try
        {
            await streetBeatParser.ParseAsync();
            await brandshopParser.ParseAsync();
            return Ok(new { message = "Все магазины: парсинг завершён" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, detail = ex.InnerException?.Message });
        }
    }
}
