using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using passwordGuard.Api.Service;
using PasswordGuard.Api.Tests.Helpers;

namespace PasswordGuard.Api.Tests
{
    public class EstimadorServiceTests
    {
        private const string RespuestaPython =
            """{"longitud":9,"bits":21.3,"intentos_estimados":2600000.0,"segundos":0.00026,"categoria":"debil"}""";

        private static EstimadorService CrearServicio(ManejadorHttpFalso manejador) =>
            new(new HttpClient(manejador) { BaseAddress = new Uri("http://estimador.test/") },
                NullLogger<EstimadorService>.Instance);

        [Fact]
        public async Task EstimarAsync_LeeLaRespuestaEnSnakeCase()
        {
            var servicio = CrearServicio(ManejadorHttpFalso.ConJson(RespuestaPython));

            var estimacion = await servicio.EstimarAsync("maria1234");

            Assert.NotNull(estimacion);
            Assert.Equal(9, estimacion.Longitud);
            Assert.Equal(21.3, estimacion.Bits);
            Assert.Equal(2_600_000, estimacion.IntentosEstimados);
            Assert.Equal(0.00026, estimacion.Segundos);
            Assert.Equal("debil", estimacion.Categoria);
        }

        [Fact]
        public async Task EstimarAsync_EnviaLaContrasenaAlEndpointEstimar()
        {
            var manejador = ManejadorHttpFalso.ConJson(RespuestaPython);
            var servicio = CrearServicio(manejador);

            await servicio.EstimarAsync("maria1234");

            Assert.Equal(HttpMethod.Post, manejador.UltimaPeticion!.Method);
            Assert.Equal("http://estimador.test/estimar", manejador.UltimaPeticion.RequestUri!.ToString());
            using var cuerpo = JsonDocument.Parse(manejador.UltimoCuerpo!);
            Assert.Equal("maria1234", cuerpo.RootElement.GetProperty("contrasena").GetString());
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.UnprocessableEntity)]
        public async Task EstimarAsync_DevuelveNullSiPythonRespondeConError(HttpStatusCode estado)
        {
            var servicio = CrearServicio(ManejadorHttpFalso.ConJson("""{"detail":"error"}""", estado));

            Assert.Null(await servicio.EstimarAsync("maria1234"));
        }

        [Fact]
        public async Task EstimarAsync_DevuelveNullSiPythonNoEstaArrancado()
        {
            var servicio = CrearServicio(ManejadorHttpFalso.ConError(new HttpRequestException("Conexión rechazada")));

            Assert.Null(await servicio.EstimarAsync("maria1234"));
        }

        [Fact]
        public async Task EstimarAsync_DevuelveNullSiPythonTardaDemasiado()
        {
            // Así se manifiesta el timeout del HttpClient
            var servicio = CrearServicio(ManejadorHttpFalso.ConError(new TaskCanceledException()));

            Assert.Null(await servicio.EstimarAsync("maria1234"));
        }

        [Fact]
        public async Task EstimarAsync_DevuelveNullSiElJsonNoEsValido()
        {
            var servicio = CrearServicio(ManejadorHttpFalso.ConJson("esto no es json"));

            Assert.Null(await servicio.EstimarAsync("maria1234"));
        }

        [Fact]
        public async Task EstimarAsync_PropagaLaCancelacionDelCliente()
        {
            // Si es el usuario quien cancela (cierra la página), no es un fallo del estimador
            var manejador = new ManejadorHttpFalso(async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            var servicio = CrearServicio(manejador);
            using var cancelacion = new CancellationTokenSource();
            cancelacion.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => servicio.EstimarAsync("maria1234", cancelacion.Token));
        }
    }
}
