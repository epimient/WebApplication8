using System;
using System.Net;
using System.Text.Json;

namespace WebApplication8.Services  
{
    public class OpenWeatherService
    {
        private readonly IHttpClientFactory _factory;
        private readonly IConfiguration _config;

        public OpenWeatherService(IHttpClientFactory factory, IConfiguration config)
        {
            _factory = factory;
            _config = config;
        }

        public async Task<(double temperatura, int humedad)> GetWeatherAsync(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City must be provided.", nameof(city));

            var apikey = _config["OpenWeather:Apikey"] ?? _config["OpenWeather:ApiKey"];
            if (string.IsNullOrEmpty(apikey))
                throw new InvalidOperationException("OpenWeather API key is not configured. Check appsettings.json (OpenWeather:Apikey).");

            var client = _factory.CreateClient();

            var encodedCity = Uri.EscapeDataString(city);
            // units=metric para obtener grados Celsius
            var url = $"https://api.openweathermap.org/data/2.5/weather?q={encodedCity}&appid={apikey}&units=metric";

            var response = await client.GetAsync(url);

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Proporciona información útil para el controlador / logs
                throw new HttpRequestException($"OpenWeather API error {(int)response.StatusCode} {response.ReasonPhrase}: {json}");
            }

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("main", out var main))
                throw new InvalidOperationException("Invalid response from OpenWeather: 'main' section missing.");

            if (!main.TryGetProperty("temp", out var tempProp) || !tempProp.TryGetDouble(out var temperatura))
                throw new InvalidOperationException("Invalid response from OpenWeather: 'temp' missing or invalid.");

            if (!main.TryGetProperty("humidity", out var humidityProp) || !humidityProp.TryGetInt32(out var humedad))
                throw new InvalidOperationException("Invalid response from OpenWeather: 'humidity' missing or invalid.");

            return (temperatura, humedad);
        }

    }
}
