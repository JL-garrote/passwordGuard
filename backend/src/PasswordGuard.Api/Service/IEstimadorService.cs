using passwordGuard.Api.models;

namespace passwordGuard.Api.Service
{
    public interface IEstimadorService
    {
        Task<EstimacionAtaque?> EstimarAsync(string contrasena, CancellationToken cancelacion = default);
    }
}
