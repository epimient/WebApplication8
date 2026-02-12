using WebApplication8.Services; // Importamos los servicios que creamos para la aplicación

// Creamos un constructor de aplicación con la configuración predeterminada
var builder = WebApplication.CreateBuilder(args);

// Agregamos HttpClient al contenedor de servicios para hacer solicitudes HTTP
builder.Services.AddHttpClient();

// Registramos el servicio para obtener datos meteorológicos de OpenWeather
// Scoped significa que se crea una instancia por cada solicitud HTTP
builder.Services.AddScoped<OpenWeatherService>();

// Registramos el servicio para generar contenido con IA usando Groq
// También es Scoped, una instancia por solicitud HTTP
builder.Services.AddScoped<AiWeatherService>();

// Agregamos soporte para controladores de API
builder.Services.AddControllers();

// Aprendamos más sobre cómo configurar Swagger/OpenAPI en https://aka.ms/aspnetcore/swashbuckle
// Estos servicios son necesarios para generar documentación de la API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Construimos la aplicación con todos los servicios registrados
var app = builder.Build();

// Configuramos el pipeline de solicitudes HTTP
if (app.Environment.IsDevelopment())
{
    // En modo desarrollo, habilitamos Swagger para documentar y probar la API
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Habilitamos redirección a HTTPS para seguridad
app.UseHttpsRedirection();

// Habilitamos autorización (aunque no tenemos reglas específicas definidas)
app.UseAuthorization();

// Mapeamos los controladores para que la aplicación sepa qué rutas manejar
app.MapControllers();

// Iniciamos la ejecución de la aplicación
app.Run();