using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using passwordGuard.Api.models;
using passwordGuard.Api.Service;

namespace PasswordGuard.Api.Tests
{
    // Pruebas de integración: levantan la API completa en memoria.
    // Gemini y el estimador se sustituyen por dobles para no salir a la red.
    public class NivelSeguridadControllerTests : IDisposable
    {
        private const string Ruta = "/api/nivelSeguridad";

        private readonly IConsejosIAService _consejosIA = Substitute.For<IConsejosIAService>();
        private readonly IEstimadorService _estimador = Substitute.For<IEstimadorService>();
        private readonly WebApplicationFactory<Program> _fabrica;
        private readonly HttpClient _cliente;

        public NivelSeguridadControllerTests()
        {
            _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(servicios =>
                {
                    // El último registro gana: sustituye a GeminiService y EstimadorService
                    servicios.AddSingleton(_consejosIA);
                    servicios.AddSingleton(_estimador);
                }));
            _cliente = _fabrica.CreateClient();
        }

        public void Dispose()
        {
            _cliente.Dispose();
            _fabrica.Dispose();
        }

        private async Task<JsonElement> PostAsync(string endpoint, object cuerpo)
        {
            using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{Ruta}/{endpoint}", cuerpo);
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        }

        private static object Peticion(string contrasena) => new
        {
            contrasena,
            usarMayusculas = true,
            usarMinusculas = true,
            usarNumeros = true,
            usarSimbolos = true,
        };

        // ---------- comprobar ----------

        [Fact]
        public async Task Comprobar_ContrasenaQueCumpleTodo()
        {
            JsonElement datos = await PostAsync("comprobar", Peticion("Barcelona2023!"));

            Assert.True(datos.GetProperty("valida").GetBoolean());
            Assert.Equal("Fuerte", datos.GetProperty("nivelSeguridad").GetString());
        }

        [Fact]
        public async Task Comprobar_ContrasenaComunEsDebilYNoValida()
        {
            JsonElement datos = await PostAsync("comprobar", Peticion("password"));

            Assert.False(datos.GetProperty("valida").GetBoolean());
            Assert.Equal("Débil", datos.GetProperty("nivelSeguridad").GetString());
        }

        [Fact]
        public async Task Comprobar_SoloExigeLosTiposElegidos()
        {
            var cuerpo = new { contrasena = "barcelonamadrid", usarMinusculas = true };

            JsonElement datos = await PostAsync("comprobar", cuerpo);

            Assert.True(datos.GetProperty("valida").GetBoolean());
        }

        // ---------- evaluar ----------

        [Theory]
        [InlineData("Primetime21", true)]
        [InlineData("PASSWORD", true)]
        [InlineData("xK9#qL2!vT", false)]
        public async Task Evaluar_IndicaSiEsComun(string contrasena, bool esperado)
        {
            JsonElement datos = await PostAsync("evaluar", new { contrasena });

            Assert.Equal(esperado, datos.GetProperty("esComun").GetBoolean());
        }

        // ---------- consejos ----------

        [Fact]
        public async Task Consejos_DevuelveLaEstructuraYLosConsejos()
        {
            _consejosIA.ObtenerConsejosAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ConsejosIA { Consejos = [new Consejo { Titulo = "Evita años", Texto = "No pongas el año al final." }] });

            JsonElement datos = await PostAsync("consejos", new { contrasena = "Barcelona2023!" });

            Assert.Equal("palabra(9) año(4) simbolo(1) longitud: 14", datos.GetProperty("patrones").GetString());
            JsonElement consejo = datos.GetProperty("consejos")[0];
            Assert.Equal("Evita años", consejo.GetProperty("titulo").GetString());
            Assert.Equal("No pongas el año al final.", consejo.GetProperty("texto").GetString());
        }

        [Fact]
        public async Task Consejos_ALaIASoloLeLlegaLaEstructuraAnonima()
        {
            await PostAsync("consejos", new { contrasena = "Barcelona2023!" });

            await _consejosIA.Received(1).ObtenerConsejosAsync(
                "palabra(9) año(4) simbolo(1) longitud: 14", Arg.Any<CancellationToken>());
            await _consejosIA.DidNotReceive().ObtenerConsejosAsync(
                Arg.Is<string>(texto => texto.Contains("Barcelona")), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Consejos_SiLaIAFallaDevuelveLaEstructuraSinConsejos()
        {
            _consejosIA.ObtenerConsejosAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns((ConsejosIA?)null);

            JsonElement datos = await PostAsync("consejos", new { contrasena = "Barcelona2023!" });

            Assert.Equal("palabra(9) año(4) simbolo(1) longitud: 14", datos.GetProperty("patrones").GetString());
            Assert.Equal(JsonValueKind.Null, datos.GetProperty("consejos").ValueKind);
        }

        // ---------- estimar ----------

        [Fact]
        public async Task Estimar_DevuelveLaEstimacionDelMicroservicio()
        {
            _estimador.EstimarAsync("maria1234", Arg.Any<CancellationToken>()).Returns(new EstimacionAtaque
            {
                Longitud = 9,
                Bits = 21.3,
                IntentosEstimados = 2_600_000,
                Segundos = 0.00026,
                Categoria = "debil",
            });

            JsonElement datos = await PostAsync("estimar", new { contrasena = "maria1234" });

            JsonElement estimacion = datos.GetProperty("estimacion");
            Assert.Equal(9, estimacion.GetProperty("longitud").GetInt32());
            Assert.Equal(21.3, estimacion.GetProperty("bits").GetDouble());
            Assert.Equal(2_600_000, estimacion.GetProperty("intentosEstimados").GetDouble());
            Assert.Equal(0.00026, estimacion.GetProperty("segundos").GetDouble());
            Assert.Equal("debil", estimacion.GetProperty("categoria").GetString());
        }

        [Fact]
        public async Task Estimar_SiPythonNoEstaDisponibleDevuelveNull()
        {
            _estimador.EstimarAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns((EstimacionAtaque?)null);

            JsonElement datos = await PostAsync("estimar", new { contrasena = "maria1234" });

            Assert.Equal(JsonValueKind.Null, datos.GetProperty("estimacion").ValueKind);
        }

        // ---------- validación ----------

        [Theory]
        [InlineData("comprobar")]
        [InlineData("evaluar")]
        [InlineData("consejos")]
        [InlineData("estimar")]
        public async Task SinContrasenaDevuelve400(string endpoint)
        {
            using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{Ruta}/{endpoint}", new { });

            Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        }

        [Theory]
        [InlineData("consejos")]
        [InlineData("estimar")]
        public async Task SinContrasenaNoSeLlamaANingunServicioExterno(string endpoint)
        {
            using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync($"{Ruta}/{endpoint}", new { });

            await _consejosIA.DidNotReceiveWithAnyArgs().ObtenerConsejosAsync(default!, default);
            await _estimador.DidNotReceiveWithAnyArgs().EstimarAsync(default!, default);
        }
    }
}
