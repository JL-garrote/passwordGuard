using System.Text.Json;
using passwordGuard.Api.models;

namespace passwordGuard.Api.Service
{
    public class EstimadorService : IEstimadorService
    {
        private static readonly JsonSerializerOptions OpcionesJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        private readonly HttpClient _http;
        private readonly ILogger<EstimadorService> _logger;

        public EstimadorService(HttpClient http, ILogger<EstimadorService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<EstimacionAtaque?> EstimarAsync(string contrasena, CancellationToken cancelacion = default)
        {
            try
            {
                using HttpResponseMessage respuesta = await _http.PostAsJsonAsync(
                    "estimar", new { contrasena }, OpcionesJson, cancelacion);

                if (!respuesta.IsSuccessStatusCode)
                {
                    _logger.LogWarning("El estimador respondió {Estado}", (int)respuesta.StatusCode);
                    return null;
                }

                return await respuesta.Content.ReadFromJsonAsync<EstimacionAtaque>(OpcionesJson, cancelacion);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "No se pudo conectar con el estimador");
                return null;
            }
            catch (TaskCanceledException) when (!cancelacion.IsCancellationRequested)
            {
                _logger.LogWarning("El estimador tardó demasiado en responder");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "El estimador devolvió un JSON que no se pudo leer");
                return null;
            }
        }
    }
}
