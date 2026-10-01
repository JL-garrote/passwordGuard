using passwordGuard.Api.models;
using passwordGuard.Api.Service;
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<comprobarContrasenaService>();
// Singleton: la lista de contraseñas comunes se lee del disco una sola vez al arrancar
builder.Services.AddSingleton<contrasenasComunesService>();
// AddHttpClient le pasa a GeminiService un HttpClient gestionado por ASP.NET
builder.Services.AddHttpClient<IConsejosIAService, GeminiService>(client => client.Timeout = TimeSpan.FromSeconds(15));
// Microservicio de Python: timeout corto porque es opcional y no debe frenar la respuesta
builder.Services.AddHttpClient<IEstimadorService, EstimadorService>(client =>
{
    string url = builder.Configuration["Estimador:Url"] ?? "http://127.0.0.1:8000";
    client.BaseAddress = new Uri(url.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(3);
});
// Contrasena no guarda estado, así que una sola instancia sirve para todas las peticiones
builder.Services.AddSingleton<Contrasena>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Permite que las pruebas de integración levanten la API con WebApplicationFactory<Program>
public partial class Program { }
