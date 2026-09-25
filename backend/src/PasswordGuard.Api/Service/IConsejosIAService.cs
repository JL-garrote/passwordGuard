using passwordGuard.Api.models;

namespace passwordGuard.Api.Service
{
    public interface IConsejosIAService
    {
        Task<ConsejosIA?> ObtenerConsejosAsync(string patrones, CancellationToken cancelacion = default);
    }
}
