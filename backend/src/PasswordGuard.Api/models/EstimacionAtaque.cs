namespace passwordGuard.Api.models
{
    // Respuesta del microservicio de Python: cuánto tardaría un ataque que prueba primero lo más "humano"
    public class EstimacionAtaque
    {
        public int Longitud { get; set; }
        public double Bits { get; set; }
        public double IntentosEstimados { get; set; }
        public double Segundos { get; set; }
        public string Categoria { get; set; } = string.Empty;
    }
}
