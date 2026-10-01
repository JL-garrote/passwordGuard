using passwordGuard.Api.Service;

namespace PasswordGuard.Api.Tests
{
    public class ComprobarContrasenaServiceTests
    {
        private readonly comprobarContrasenaService _servicio = new(new contrasenasComunesService());

        [Theory]
        [InlineData("1234567", false)]
        [InlineData("12345678", true)]
        [InlineData("", false)]
        public void ComprobarLongitud_ExigeAlMenosOchoCaracteres(string contrasena, bool esperado)
        {
            Assert.Equal(esperado, _servicio.comprobarLongitud(contrasena));
        }

        [Theory]
        [InlineData("abcD", true)]
        [InlineData("abcd", false)]
        [InlineData("", false)]
        public void ComprobarMayusculas(string contrasena, bool esperado)
        {
            Assert.Equal(esperado, _servicio.comprobarMayusculas(contrasena));
        }

        [Theory]
        [InlineData("ABCd", true)]
        [InlineData("ABCD", false)]
        public void ComprobarMinusculas(string contrasena, bool esperado)
        {
            Assert.Equal(esperado, _servicio.comprobarMinusculas(contrasena));
        }

        [Theory]
        [InlineData("abc1", true)]
        [InlineData("abcd", false)]
        public void ComprobarNumeros(string contrasena, bool esperado)
        {
            Assert.Equal(esperado, _servicio.comprobarNumeros(contrasena));
        }

        [Theory]
        [InlineData("abc!", true)]
        [InlineData("abc 1", true)]
        [InlineData("abc1", false)]
        public void ComprobarSimbolos(string contrasena, bool esperado)
        {
            Assert.Equal(esperado, _servicio.comprobarSimbolos(contrasena));
        }

        [Theory]
        [InlineData("Barcelona2023!", true, true, true, true, true)]
        [InlineData("barcelona2023!", true, true, true, true, false)]  // falta mayúscula
        [InlineData("barcelona2023!", false, true, true, true, true)]  // la mayúscula no se exige
        [InlineData("Barcelona!", true, true, true, true, false)]      // falta número
        [InlineData("Barcelona!", true, true, false, true, true)]
        [InlineData("Barcelona2023", true, true, true, true, false)]   // falta símbolo
        [InlineData("Barcelona2023", true, true, true, false, true)]
        [InlineData("Ab1!", true, true, true, true, false)]            // demasiado corta
        [InlineData("Ab1!", false, false, false, false, false)]        // la longitud se exige siempre
        public void ComprobarContrasena_RespetaLosRequisitosElegidos(
            string contrasena, bool mayusculas, bool minusculas, bool numeros, bool simbolos, bool esperado)
        {
            Assert.Equal(esperado, _servicio.comprobarContrasena(contrasena, mayusculas, minusculas, numeros, simbolos));
        }

        [Fact]
        public void ComprobarContrasena_SinParametrosExigeTodo()
        {
            Assert.True(_servicio.comprobarContrasena("Barcelona2023!"));
            Assert.False(_servicio.comprobarContrasena("Barcelona2023"));
        }

        [Fact]
        public void ComprobarContrasenaSinSimbolos_NoExigeSimbolos()
        {
            Assert.True(_servicio.comprobarContrasenaSinSimbolos("Barcelona2023"));
        }

        [Fact]
        public void ComprobarContrasenaSinNumeros_NoExigeNumeros()
        {
            Assert.True(_servicio.comprobarContrasenaSinNumeros("Barcelona!"));
        }

        [Fact]
        public void ComprobarLongitud_NoArrastraEstadoEntreLlamadas()
        {
            Assert.True(_servicio.comprobarLongitud("12345678"));
            Assert.False(_servicio.comprobarLongitud("123"));
        }

        [Theory]
        [InlineData("Ab1!", 4)]                 // corta, pero con los 4 tipos
        [InlineData("xqzvbnmlkjhgtrew", 4)]     // 16 minúsculas: 3 por longitud + 1
        [InlineData("xK9#qL2!vT", 5)]           // 10 caracteres: 1 por longitud + 4 tipos
        [InlineData("xK9#qL2!vTw4", 6)]         // 12 caracteres: 2 por longitud + 4 tipos
        [InlineData("Xq9!Xq9!Xq9!Xq9!", 7)]     // 16 caracteres y los 4 tipos: máximo
        public void CalcularPuntuacion_SumaLongitudYTipos(string contrasena, int esperado)
        {
            Assert.Equal(esperado, _servicio.calcularPuntuacion(contrasena));
        }

        [Theory]
        [InlineData("password")]
        [InlineData("Primetime21")]
        public void CalcularPuntuacion_ContrasenaComunValeCero(string contrasena)
        {
            Assert.Equal(0, _servicio.calcularPuntuacion(contrasena));
        }

        [Theory]
        [InlineData(0, "Débil")]
        [InlineData(2, "Débil")]
        [InlineData(3, "Media")]
        [InlineData(4, "Media")]
        [InlineData(5, "Fuerte")]
        [InlineData(6, "Fuerte")]
        [InlineData(7, "Muy fuerte")]
        public void ObtenerNivel_RespetaLosLimites(int puntuacion, string esperado)
        {
            Assert.Equal(esperado, _servicio.obtenerNivel(puntuacion));
        }

        [Theory]
        [InlineData("password", "Débil")]
        [InlineData("xK9#qL2!vT", "Fuerte")]
        [InlineData("Xq9!Xq9!Xq9!Xq9!", "Muy fuerte")]
        public void EvaluarContrasena_DevuelveElNivel(string contrasena, string esperado)
        {
            Assert.Equal(esperado, _servicio.evaluarContrasena(contrasena));
        }
    }
}
