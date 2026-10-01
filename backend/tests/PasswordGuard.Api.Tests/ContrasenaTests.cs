using passwordGuard.Api.models;

namespace PasswordGuard.Api.Tests
{
    public class ContrasenaTests
    {
        private readonly Contrasena _contrasena = new();

        [Theory]
        [InlineData("Barcelona2023!", "palabra(9) año(4) simbolo(1) longitud: 14")]
        [InlineData("pass7391", "palabra(4) numero(4) longitud: 8")]
        [InlineData("Maria2025x", "palabra(5) año(4) palabra(1) longitud: 10")]
        [InlineData("hola1850", "palabra(4) numero(4) longitud: 8")]
        [InlineData("hola1899", "palabra(4) numero(4) longitud: 8")]
        [InlineData("hola1900", "palabra(4) año(4) longitud: 8")]
        [InlineData("abc123", "palabra(3) numero(3) longitud: 6")]
        [InlineData("@#$", "simbolo(3) longitud: 3")]
        public void PartirContrasena_ClasificaCadaBloque(string contrasena, string esperado)
        {
            Assert.Equal(esperado, _contrasena.partirContrasena(contrasena));
        }

        [Theory]
        [InlineData("aaaa1111", "repeticion(4) repeticion(4) longitud: 8")]
        [InlineData("AAaa!!!!", "repeticion(4) repeticion(4) longitud: 8")]
        [InlineData("zzz", "repeticion(3) longitud: 3")]
        public void PartirContrasena_DetectaRepeticionesSinDistinguirMayusculas(string contrasena, string esperado)
        {
            Assert.Equal(esperado, _contrasena.partirContrasena(contrasena));
        }

        [Fact]
        public void PartirContrasena_LaRepeticionTienePrioridadSobreElAnio()
        {
            // 1111 está dentro del rango de años, pero antes es una repetición
            Assert.Equal("repeticion(4) longitud: 4", _contrasena.partirContrasena("1111"));
        }

        [Fact]
        public void PartirContrasena_DosCaracteresIgualesNoSonRepeticion()
        {
            Assert.Equal("palabra(2) longitud: 2", _contrasena.partirContrasena("aa"));
        }

        [Fact]
        public void PartirContrasena_AceptaElAnioSiguienteAlActual()
        {
            int siguiente = DateTime.Now.Year + 1;
            Assert.Equal("año(4) longitud: 4", _contrasena.partirContrasena(siguiente.ToString()));
        }

        [Fact]
        public void PartirContrasena_DosAniosEnElFuturoEsNumero()
        {
            int futuro = DateTime.Now.Year + 2;
            Assert.Equal("numero(4) longitud: 4", _contrasena.partirContrasena(futuro.ToString()));
        }

        [Fact]
        public void PartirContrasena_CadenaVaciaSoloDevuelveLaLongitud()
        {
            Assert.Equal("longitud: 0", _contrasena.partirContrasena(""));
        }

        [Fact]
        public void PartirContrasena_NuncaIncluyeElTextoDeLaContrasena()
        {
            string estructura = _contrasena.partirContrasena("Barcelona2023!");
            Assert.DoesNotContain("Barcelona", estructura);
            Assert.DoesNotContain("2023", estructura);
            Assert.DoesNotContain("!", estructura);
        }

        [Theory]
        [InlineData("hola15031995", "palabra(4) fecha(8) longitud: 12")]   // día-mes-año
        [InlineData("hola19950315", "palabra(4) fecha(8) longitud: 12")]   // año-mes-día
        [InlineData("01011900", "fecha(8) longitud: 8")]                   // límite inferior
        [InlineData("29022024", "fecha(8) longitud: 8")]                   // 29 de febrero en año bisiesto
        public void PartirContrasena_DetectaFechasDeOchoDigitos(string contrasena, string esperado)
        {
            Assert.Equal(esperado, _contrasena.partirContrasena(contrasena));
        }

        [Theory]
        [InlineData("32011995")]   // día 32
        [InlineData("15131995")]   // mes 13
        [InlineData("29022023")]   // 29 de febrero en año no bisiesto
        [InlineData("31121899")]   // anterior a 1900
        [InlineData("12345678")]   // no es fecha en ningún formato
        public void PartirContrasena_OchoDigitosQueNoSonFechaSonNumero(string contrasena)
        {
            Assert.Equal("numero(8) longitud: 8", _contrasena.partirContrasena(contrasena));
        }

        [Fact]
        public void PartirContrasena_FechaFueraDelRangoDeAniosEsNumero()
        {
            string dentroDeDosAnios = $"0101{DateTime.Now.Year + 2}";
            Assert.Equal("numero(8) longitud: 8", _contrasena.partirContrasena(dentroDeDosAnios));
        }

        [Fact]
        public void PartirContrasena_OchoDigitosIgualesSonRepeticionAntesQueFecha()
        {
            Assert.Equal("repeticion(8) longitud: 8", _contrasena.partirContrasena("11111111"));
        }
    }
}
