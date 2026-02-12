using Microsoft.AspNetCore.Mvc;
using WebApplication8.Services;

namespace WebApplication8.Controllers
{
    [ApiController]
    [Route("weather")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly OpenWeatherService _openWeatherService;

        public WeatherForecastController(OpenWeatherService openWeatherService)
        {
            _openWeatherService = openWeatherService;
        }

        [HttpGet("{city}")]
        public async Task<IActionResult> GetWeather(string city)
        {
            if (string.IsNullOrEmpty(city))
            {
                return BadRequest("City name is required.");
            }

            try
            {
                var (temp, humidity) = await _openWeatherService.GetWeatherAsync(city);
                return Ok(new 
                { 
                    city = city,
                    Temperature = temp, 
                    Humidity = humidity 
                });

            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return StatusCode(500, $"An error occurred while fetching weather data: {ex.Message}");
            }
        }
    }
}
