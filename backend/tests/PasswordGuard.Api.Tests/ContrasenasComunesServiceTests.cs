using passwordGuard.Api.Service;

namespace PasswordGuard.Api.Tests
{
    // Usa el archivo real data/10k_most_common.txt, que se copia a la carpeta de salida de los tests
    public class ContrasenasComunesServiceTests
    {
        private readonly contrasenasComunesService _servicio = new();

        [Theory]
        [InlineData("password")]
        [InlineData("123456")]
        [InlineData("qwerty")]
        [InlineData("primetime21")]
        public void EsContrasenaComun_DetectaLasDeLaLista(string contrasena)
        {
            Assert.True(_servicio.esContrasenaComun(contrasena));
        }

        [Theory]
        [InlineData("PASSWORD")]
        [InlineData("Primetime21")]
        public void EsContrasenaComun_NoDistingueMayusculas(string contrasena)
        {
            Assert.True(_servicio.esContrasenaComun(contrasena));
        }

        [Theory]
        [InlineData("xK9#qL2!vT")]
        [InlineData("Correct-Horse-Battery-Staple")]
        public void EsContrasenaComun_NoMarcaLasQueNoEstan(string contrasena)
        {
            Assert.False(_servicio.esContrasenaComun(contrasena));
        }
    }
}
