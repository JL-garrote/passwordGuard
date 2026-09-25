using Microsoft.AspNetCore.Mvc;
using passwordGuard.Api.Service;

namespace passwordGuard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class nivelSeguridadController : ControllerBase
    {
        private readonly comprobarContrasenaService _comprobarContrasenaService;
        private readonly contrasenasComunesService _contrasenasComunesService;

        public nivelSeguridadController(comprobarContrasenaService comprobarContrasenaService, contrasenasComunesService contrasenasComunesService)
        {
            _comprobarContrasenaService = comprobarContrasenaService;
            _contrasenasComunesService = contrasenasComunesService;
        }

        [HttpPost("comprobar")]
        public IActionResult ComprobarContrasena([FromBody] ContrasenaRequest request)
        {
            bool esValida = _comprobarContrasenaService.comprobarContrasena(request.Contrasena, request.UsarMayusculas, request.UsarMinusculas, request.UsarNumeros, request.UsarSimbolos);
            string nivelSeguridad = _comprobarContrasenaService.evaluarContrasena(request.Contrasena);   
            return Ok(new { Valida = esValida, NivelSeguridad = nivelSeguridad });
        }

        [HttpPost("evaluar")]
        public IActionResult EvaluarContrasena([FromBody] ContrasenaRequest request)
        {
            bool comun = _contrasenasComunesService.esContrasenaComun(request.Contrasena);
            return Ok(new { Valida = comun });
        }
    }

    public class ContrasenaRequest
    {
        public string Contrasena { get; set; }
        public bool UsarMayusculas { get; set; }
        public bool UsarMinusculas { get; set; }
        public bool UsarNumeros { get; set; }
        public bool UsarSimbolos { get; set; }
    }
}   