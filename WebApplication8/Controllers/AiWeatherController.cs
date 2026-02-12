using Microsoft.AspNetCore.Mvc; // Biblioteca para crear controladores de API
using WebApplication8.Services; // Importamos los servicios que creamos

namespace WebApplication8.Controllers
{
    // Atributo que indica que esta clase es un controlador de API
    [ApiController]
    
    // Atributo que define la ruta base para todos los endpoints de este controlador
    [Route("ai-weather")]
    
    /// <summary>
    /// Controlador que maneja las solicitudes relacionadas con la inteligencia artificial
    /// para generar descripciones del clima y sugerencias de actividades
    /// </summary>
    public class AiWeatherController : ControllerBase
    {
        // Servicio para obtener datos meteorológicos tradicionales de OpenWeather
        private readonly OpenWeatherService _openWeatherService;
        
        // Servicio para generar descripciones y sugerencias usando IA
        private readonly AiWeatherService _aiWeatherService;

        /// <summary>
        /// Constructor del controlador que inyecta los servicios necesarios
        /// </summary>
        /// <param name="openWeatherService">Servicio para obtener datos meteorológicos</param>
        /// <param name="aiWeatherService">Servicio para generar contenido con IA</param>
        public AiWeatherController(OpenWeatherService openWeatherService, AiWeatherService aiWeatherService)
        {
            // Almacenamos el servicio de clima tradicional
            _openWeatherService = openWeatherService;
            
            // Almacenamos el servicio de IA
            _aiWeatherService = aiWeatherService;
        }

        /// <summary>
        /// Endpoint GET que devuelve información meteorológica mejorada con IA
        /// </summary>
        /// <param name="city">Nombre de la ciudad para consultar el clima</param>
        /// <returns>Objeto con datos meteorológicos y contenido generado por IA</returns>
        [HttpGet("{city}")]
        public async Task<IActionResult> GetAiWeatherInfo(string city)
        {
            // Verificamos si el nombre de la ciudad está vacío o nulo
            if (string.IsNullOrEmpty(city))
            {
                // Si está vacío, devolvemos un error 400 (Bad Request)
                return BadRequest("City name is required.");
            }

            try
            {
                // Obtenemos los datos meteorológicos básicos (temperatura y humedad) de la ciudad
                var (temp, humidity) = await _openWeatherService.GetWeatherAsync(city);

                // Generamos una descripción del clima en lenguaje natural usando IA
                var weatherDescription = await _aiWeatherService.GenerateWeatherDescriptionAsync(temp, humidity, city);

                // Generamos sugerencias de actividades según el clima usando IA
                var activitySuggestions = await _aiWeatherService.GenerateActivitySuggestionsAsync(temp, humidity, city);

                // Devolvemos un objeto JSON con todos los datos (clima tradicional + IA)
                return Ok(new
                {
                    city = city, // Nombre de la ciudad
                    temperature = temp, // Temperatura obtenida de OpenWeather
                    humidity = humidity, // Humedad obtenida de OpenWeather
                    ai_weather_description = weatherDescription, // Descripción generada por IA
                    ai_activity_suggestions = activitySuggestions // Sugerencias de actividades generadas por IA
                });
            }
            catch (Exception ex)
            {
                // Si ocurre cualquier error, devolvemos un error 500 (Internal Server Error)
                return StatusCode(500, $"An error occurred while processing AI weather data: {ex.Message}");
            }
        }
    }
}