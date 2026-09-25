using System.Text.Json;
using passwordGuard.Api.models;

namespace passwordGuard.Api.Service
{
    public class GeminiService
    {
        private const string UrlBase = "https://generativelanguage.googleapis.com/v1beta/models/";

        private const string Instrucciones =
            "Eres un experto en seguridad informática. Recibes la estructura anónima de una contraseña " +
            "(tipos de segmento y longitudes), nunca la contraseña real. Da exactamente 3 consejos en español, " +
            "concretos y breves, para hacerla más segura según esa estructura. No pidas la contraseña " +
            "ni inventes datos que no aparezcan en la estructura.";

        private static readonly object EsquemaRespuesta = new
        {
            type = "OBJECT",
            properties = new
            {
                consejos = new
                {
                    type = "ARRAY",
                    items = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            titulo = new { type = "STRING" },
                            texto = new { type = "STRING" },
                        },
                        required = new[] { "titulo", "texto" },
                    },
                },
            },
            required = new[] { "consejos" },
        };

        private readonly HttpClient _http;
        private readonly ILogger<GeminiService> _logger;
        private readonly string _apiKey;
        private readonly string _modelo;

        public GeminiService(HttpClient http, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _http = http;
            _logger = logger;
            _apiKey = configuration["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Falta Gemini:ApiKey en la configuración (.env).");
            _modelo = configuration["Gemini:Model"] ?? "gemini-3.8-flash";
        }

        public async Task<ConsejosIA?> ObtenerConsejosAsync(string patrones, CancellationToken cancelacion = default)
        {
            var cuerpo = new
            {
                systemInstruction = new { parts = new[] { new { text = Instrucciones } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = patrones } } } },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseSchema = EsquemaRespuesta,
                },
            };

            using var peticion = new HttpRequestMessage(HttpMethod.Post, $"{UrlBase}{_modelo}:generateContent");
            peticion.Headers.Add("x-goog-api-key", _apiKey);
            peticion.Content = JsonContent.Create(cuerpo);

            try
            {
                using HttpResponseMessage respuesta = await _http.SendAsync(peticion, cancelacion);

                if (!respuesta.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini respondió {Estado}", (int)respuesta.StatusCode);
                    return null;
                }

                var datos = await respuesta.Content.ReadFromJsonAsync<RespuestaGemini>(cancelacion);

                string? texto = datos?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                if (string.IsNullOrWhiteSpace(texto))
                {
                    _logger.LogWarning("Gemini devolvió una respuesta sin texto");
                    return null;
                }

                return JsonSerializer.Deserialize<ConsejosIA>(texto, JsonSerializerOptions.Web);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "No se pudo conectar con Gemini");
                return null;
            }
            catch (TaskCanceledException) when (!cancelacion.IsCancellationRequested)
            {
                _logger.LogWarning("Gemini tardó demasiado en responder");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Gemini devolvió un JSON que no se pudo leer");
                return null;
            }
        }

        private record RespuestaGemini(List<Candidato>? Candidates);
        private record Candidato(Contenido? Content);
        private record Contenido(List<Parte>? Parts);
        private record Parte(string? Text);
    }
}
