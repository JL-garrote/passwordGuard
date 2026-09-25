namespace passwordGuard.Api.models
{
    public class ConsejosIA
    {
        public List<Consejo> Consejos { get; set; } = [];
    }

    public class Consejo
    {
        public string Titulo { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
    }
}
