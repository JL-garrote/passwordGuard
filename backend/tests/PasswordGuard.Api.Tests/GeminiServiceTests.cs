using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using passwordGuard.Api.Service;
using PasswordGuard.Api.Tests.Helpers;

namespace PasswordGuard.Api.Tests
{
    public class GeminiServiceTests
    {
        private const string Patrones = "palabra(9) año(4) simbolo(1) longitud: 14";

        private static IConfiguration Configuracion(string? apiKey = "clave-de-prueba", string? modelo = null)
        {
            var valores = new Dictionary<string, string?>();
            if (apiKey is not null) valores["Gemini:ApiKey"] = apiKey;
            if (modelo is not null) valores["Gemini:Model"] = modelo;
            return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        }

        private static GeminiService CrearServicio(ManejadorHttpFalso manejador, IConfiguration? configuracion = null) =>
            new(new HttpClient(manejador), configuracion ?? Configuracion(), NullLogger<GeminiService>.Instance);

        // Gemini devuelve los consejos como texto JSON dentro de candidates[0].content.parts[0].text
        private static string RespuestaGemini(string texto) => JsonSerializer.Serialize(new
        {
            candidates = new[] { new { content = new { parts = new[] { new { text = texto } } } } },
        });

        private const string ConsejosJson =
            """{"consejos":[{"titulo":"Evita años","texto":"No pongas el año al final."},{"titulo":"Más largo","texto":"Usa 16 caracteres o más."},{"titulo":"Mezcla","texto":"Intercala los símbolos."}]}""";

        [Fact]
        public void Constructor_FallaSinApiKey()
        {
            var manejador = ManejadorHttpFalso.ConJson("{}");

            Assert.Throws<InvalidOperationException>(() => CrearServicio(manejador, Configuracion(apiKey: null)));
        }

        [Fact]
        public async Task ObtenerConsejosAsync_LeeLosTresConsejos()
        {
            var servicio = CrearServicio(ManejadorHttpFalso.ConJson(RespuestaGemini(ConsejosJson)));

            var consejos = await servicio.ObtenerConsejosAsync(Patrones);

            Assert.NotNull(consejos);
            Assert.Equal(3, consejos.Consejos.Count);
            Assert.Equal("Evita años", consejos.Consejos[0].Titulo);
            Assert.Equal("No pongas el año al final.", consejos.Consejos[0].Texto);
        }

        [Fact]
        public async Task ObtenerConsejosAsync_EnviaLaClaveEnLaCabeceraYSoloLaEstructura()
        {
            var manejador = ManejadorHttpFalso.ConJson(RespuestaGemini(ConsejosJson));
            var servicio = CrearServicio(manejador);

            await servicio.ObtenerConsejosAsync(Patrones);

            var peticion = manejador.UltimaPeticion!;
            Assert.Equal(HttpMethod.Post, peticion.Method);
            Assert.Equal("clave-de-prueba", peticion.Headers.GetValues("x-goog-api-key").Single());
            // La clave va en la cabecera, nunca en la URL (que acaba en registros)
            Assert.DoesNotContain("clave-de-prueba", peticion.RequestUri!.ToString());
            // Se lee el JSON porque el serializador escapa la "ñ" de "año"
            using var cuerpo = JsonDocument.Parse(manejador.UltimoCuerpo!);
            string? texto = cuerpo.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString();
            Assert.Equal(Patrones, texto);
        }

        [Fact]
        public async Task ObtenerConsejosAsync_UsaElModeloPorDefecto()
        {
            var manejador = ManejadorHttpFalso.ConJson(RespuestaGemini(ConsejosJson));

            await CrearServicio(manejador).ObtenerConsejosAsync(Patrones);

            Assert.EndsWith("/models/gemini-3.8-flash:generateContent", manejador.UltimaPeticion!.RequestUri!.AbsolutePath);
        }

        [Fact]
        public async Task ObtenerConsejosAsync_UsaElModeloConfigurado()
        {
            var manejador = ManejadorHttpFalso.ConJson(RespuestaGemini(ConsejosJson));

            await CrearServicio(manejador, Configuracion(modelo: "gemini-3.5-flash-lite")).ObtenerConsejosAsync(Patrones);

            Assert.EndsWith("/models/gemini-3.5-flash-lite:generateContent", manejador.UltimaPeticion!.RequestUri!.AbsolutePath);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.TooManyRequests)]
        public async Task ObtenerConsejosAsync_DevuelveNullSiGeminiRespondeConError(HttpStatusCode estado)
        {
            var servicio = CrearServicio(ManejadorHttpFalso.ConJson("""{"error":{}}""", estado));

            Assert.Null(await servicio.ObtenerConsejosAsync(Patrones));
        }

        [Theory]
        [InlineData("""{"candidates":[]}""")]
        [InlineData("""{}""")]
        public async Task ObtenerConsejosAsync_DevuelveNullSiNoHayTexto(string respuesta)
        {
            var servicio = CrearServicio(ManejadorHttpFalso.ConJson(respuesta));

            Assert.Null(await servicio.ObtenerConsejosAsync(Patrones));
        }

        [Fact]
        public async Task ObtenerConsejosAsync_DevuelveNullSiElTextoNoEsJson()
        {
            var servicio = CrearServicio(ManejadorHttpFalso.ConJson(RespuestaGemini("Lo siento, no puedo ayudar.")));

            Assert.Null(await servicio.ObtenerConsejosAsync(Patrones));
        }

        [Fact]
        public async Task ObtenerConsejosAsync_DevuelveNullSiNoHayConexion()
        {
            var servicio = CrearServicio(ManejadorHttpFalso.ConError(new HttpRequestException("Sin red")));

            Assert.Null(await servicio.ObtenerConsejosAsync(Patrones));
        }

        [Fact]
        public async Task ObtenerConsejosAsync_DevuelveNullSiGeminiTardaDemasiado()
        {
            var servicio = CrearServicio(ManejadorHttpFalso.ConError(new TaskCanceledException()));

            Assert.Null(await servicio.ObtenerConsejosAsync(Patrones));
        }
    }
}
