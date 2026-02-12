using System.Text; // Biblioteca para manejar codificaciones de texto
using System.Text.Json; // Biblioteca para serializar y deserializar JSON

namespace WebApplication8.Services
{
    /// <summary>
    /// Servicio que se encarga de comunicarse con la API de Groq para generar descripciones del clima
    /// y sugerencias de actividades usando inteligencia artificial
    /// </summary>
    public class AiWeatherService
    {
        // Cliente HTTP factory para crear clientes HTTP de forma segura
        private readonly IHttpClientFactory _httpClientFactory;
        
        // Interfaz para acceder a la configuración de la aplicación (appsettings.json)
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Constructor del servicio que inyecta las dependencias necesarias
        /// </summary>
        /// <param name="httpClientFactory">Factoría para crear clientes HTTP</param>
        /// <param name="configuration">Acceso a la configuración de la aplicación</param>
        public AiWeatherService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory; // Almacenamos la factoría de HTTP clients
            _configuration = configuration; // Almacenamos la configuración
        }

        /// <summary>
        /// Método asincrónico que genera una descripción del clima en lenguaje natural usando IA
        /// </summary>
        /// <param name="temperature">Temperatura actual</param>
        /// <param name="humidity">Humedad actual</param>
        /// <param name="city">Nombre de la ciudad</param>
        /// <returns>Una cadena con la descripción del clima generada por IA</returns>
        public async Task<string> GenerateWeatherDescriptionAsync(double temperature, int humidity, string city)
        {
            // Obtenemos la clave API de Groq desde la configuración
            var apiKey = _configuration["Groq:ApiKey"];
            
            // Verificamos si la clave API está vacía o nula
            if (string.IsNullOrEmpty(apiKey))
            {
                // Si no hay clave API, lanzamos una excepción indicando el problema
                throw new InvalidOperationException("Groq API key is not configured. Check appsettings.json (Groq:ApiKey).");
            }

            // Creamos un cliente HTTP usando la factoría inyectada
            var client = _httpClientFactory.CreateClient();

            // Creamos el prompt (instrucción) que le daremos al modelo de IA
            // Este prompt incluye la información del clima y le pedimos una descripción específica
            var prompt = $"Describe brevemente en lenguaje natural y amigable el clima actual en {city}. " +
                        $"La temperatura es de {temperature}°C y la humedad es del {humidity}%. " +
                        $"Responde en máximo 2 frases usando un lenguaje coloquial y atractivo.";

            // Creamos el cuerpo de la solicitud que enviaremos a la API de Groq
            // Este objeto define cómo queremos que el modelo responda
            var requestBody = new
            {
                // Especificamos el modelo de IA que queremos usar
                model = "llama-3.1-8b-instant", 
                
                // Mensajes que enviamos al modelo (rol de usuario con el prompt)
                messages = new[] {
                    new {
                        role = "user", // Indicamos que este mensaje viene del usuario
                        content = prompt // Contenido del mensaje (el prompt que creamos)
                    }
                },
                
                // Temperatura: controla la creatividad de la respuesta (0.3 = menos creativo, más preciso)
                temperature = 0.3, 
                
                // Máximo número de tokens (palabras/piezas de texto) que queremos en la respuesta
                max_tokens = 100 
            };

            // Convertimos el objeto requestBody a una cadena JSON
            var jsonContent = JsonSerializer.Serialize(requestBody);
            
            // Creamos un contenido HTTP con el JSON, usando codificación UTF-8
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            // Establecemos el tipo de contenido como JSON en los encabezados
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            
            // Creamos un mensaje de solicitud HTTP POST para enviar a la API de Groq
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = httpContent // Adjuntamos el contenido JSON al mensaje
            };
            
            // Agregamos el encabezado de autorización con nuestra clave API
            request.Headers.Add("Authorization", $"Bearer {_configuration["Groq:ApiKey"]}");

            // Enviamos la solicitud a la API de Groq y esperamos la respuesta
            var response = await client.SendAsync(request);
            
            // Verificamos si la respuesta fue exitosa (código 200-299)
            if (!response.IsSuccessStatusCode)
            {
                // Leemos el contenido de error de la respuesta
                var errorContent = await response.Content.ReadAsStringAsync();
                
                // Si el error es de autenticación (clave incorrecta), lanzamos una excepción específica
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException($"Groq API authentication failed. Please verify your API key is correct and active. Error: {errorContent}");
                }
                
                // Para otros tipos de error, lanzamos una excepción general con detalles
                throw new HttpRequestException($"Groq API error {(int)response.StatusCode}: {errorContent}");
            }

            // Leemos la respuesta JSON que envió la API de Groq
            var responseJson = await response.Content.ReadAsStringAsync();
            
            // Parseamos el JSON para poder navegar por sus propiedades
            using var document = JsonDocument.Parse(responseJson);
            var root = document.RootElement;

            // Buscamos la propiedad "choices" en la respuesta (contiene las opciones generadas por el modelo)
            if (root.TryGetProperty("choices", out var choicesArray) && 
                // Verificamos que haya al menos una opción en el array
                choicesArray.GetArrayLength() > 0 &&
                // Buscamos el primer elemento del array de opciones
                choicesArray[0].TryGetProperty("message", out var message) &&
                // Dentro del mensaje, buscamos la propiedad "content" que contiene la respuesta real
                message.TryGetProperty("content", out var content))
            {
                // Devolvemos el contenido como string, o una cadena vacía si es nulo
                return content.GetString() ?? string.Empty;
            }

            // Si no encontramos la estructura esperada en la respuesta, lanzamos una excepción
            throw new InvalidOperationException("Invalid response format from Groq API");
        }

        /// <summary>
        /// Método asincrónico que genera sugerencias de actividades según el clima usando IA
        /// </summary>
        /// <param name="temperature">Temperatura actual</param>
        /// <param name="humidity">Humedad actual</param>
        /// <param name="city">Nombre de la ciudad</param>
        /// <returns>Una cadena con sugerencias de actividades generadas por IA</returns>
        public async Task<string> GenerateActivitySuggestionsAsync(double temperature, int humidity, string city)
        {
            // Obtenemos la clave API de Groq desde la configuración
            var apiKey = _configuration["Groq:ApiKey"];
            
            // Verificamos si la clave API está vacía o nula
            if (string.IsNullOrEmpty(apiKey))
            {
                // Si no hay clave API, lanzamos una excepción indicando el problema
                throw new InvalidOperationException("Groq API key is not configured. Check appsettings.json (Groq:ApiKey).");
            }

            // Creamos un cliente HTTP usando la factoría inyectada
            var client = _httpClientFactory.CreateClient();

            // Creamos el prompt (instrucción) para generar sugerencias de actividades
            var prompt = $"Sugiere 2 o 3 actividades breves y específicas para el clima actual en {city}. " +
                        $"La temperatura es de {temperature}°C y la humedad es del {humidity}%. " +
                        $"Responde con una lista corta, cada actividad en una línea, sin explicaciones largas.";

            // Creamos el cuerpo de la solicitud que enviaremos a la API de Groq
            var requestBody = new
            {
                // Especificamos el modelo de IA que queremos usar
                model = "llama-3.1-8b-instant", 
                
                // Mensajes que enviamos al modelo (rol de usuario con el prompt)
                messages = new[] {
                    new {
                        role = "user", // Indicamos que este mensaje viene del usuario
                        content = prompt // Contenido del mensaje (el prompt que creamos)
                    }
                },
                
                // Temperatura: controla la creatividad de la respuesta (0.3 = menos creativo, más preciso)
                temperature = 0.3, 
                
                // Máximo número de tokens (palabras/piezas de texto) que queremos en la respuesta
                max_tokens = 100 
            };

            // Convertimos el objeto requestBody a una cadena JSON
            var jsonContent = JsonSerializer.Serialize(requestBody);
            
            // Creamos un contenido HTTP con el JSON, usando codificación UTF-8
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            // Establecemos el tipo de contenido como JSON en los encabezados
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            
            // Creamos un mensaje de solicitud HTTP POST para enviar a la API de Groq
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
            {
                Content = httpContent // Adjuntamos el contenido JSON al mensaje
            };
            
            // Agregamos el encabezado de autorización con nuestra clave API
            request.Headers.Add("Authorization", $"Bearer {_configuration["Groq:ApiKey"]}");

            // Enviamos la solicitud a la API de Groq y esperamos la respuesta
            var response = await client.SendAsync(request);
            
            // Verificamos si la respuesta fue exitosa (código 200-299)
            if (!response.IsSuccessStatusCode)
            {
                // Leemos el contenido de error de la respuesta
                var errorContent = await response.Content.ReadAsStringAsync();
                
                // Si el error es de autenticación (clave incorrecta), lanzamos una excepción específica
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException($"Groq API authentication failed. Please verify your API key is correct and active. Error: {errorContent}");
                }
                
                // Para otros tipos de error, lanzamos una excepción general con detalles
                throw new HttpRequestException($"Groq API error {(int)response.StatusCode}: {errorContent}");
            }

            // Leemos la respuesta JSON que envió la API de Groq
            var responseJson = await response.Content.ReadAsStringAsync();
            
            // Parseamos el JSON para poder navegar por sus propiedades
            using var document = JsonDocument.Parse(responseJson);
            var root = document.RootElement;

            // Buscamos la propiedad "choices" en la respuesta (contiene las opciones generadas por el modelo)
            if (root.TryGetProperty("choices", out var choicesArray) && 
                // Verificamos que haya al menos una opción en el array
                choicesArray.GetArrayLength() > 0 &&
                // Buscamos el primer elemento del array de opciones
                choicesArray[0].TryGetProperty("message", out var message) &&
                // Dentro del mensaje, buscamos la propiedad "content" que contiene la respuesta real
                message.TryGetProperty("content", out var content))
            {
                // Devolvemos el contenido como string, o una cadena vacía si es nulo
                return content.GetString() ?? string.Empty;
            }

            // Si no encontramos la estructura esperada en la respuesta, lanzamos una excepción
            throw new InvalidOperationException("Invalid response format from Groq API");
        }
    }
}