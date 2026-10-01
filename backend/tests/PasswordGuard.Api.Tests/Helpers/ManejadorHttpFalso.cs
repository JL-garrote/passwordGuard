using System.Net;
using System.Text;

namespace PasswordGuard.Api.Tests.Helpers
{
    // Sustituye a la red: el HttpClient le entrega la petición y devuelve la respuesta que le digamos
    public class ManejadorHttpFalso : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

        public HttpRequestMessage? UltimaPeticion { get; private set; }
        public string? UltimoCuerpo { get; private set; }

        public ManejadorHttpFalso(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        public static ManejadorHttpFalso ConJson(string json, HttpStatusCode estado = HttpStatusCode.OK) =>
            new((_, _) => Task.FromResult(new HttpResponseMessage(estado)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }));

        public static ManejadorHttpFalso ConError(Exception error) =>
            new((_, _) => Task.FromException<HttpResponseMessage>(error));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            UltimaPeticion = request;
            // El cuerpo se lee aquí porque el HttpClient lo libera después de enviarlo
            UltimoCuerpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return await _responder(request, cancellationToken);
        }
    }
}
