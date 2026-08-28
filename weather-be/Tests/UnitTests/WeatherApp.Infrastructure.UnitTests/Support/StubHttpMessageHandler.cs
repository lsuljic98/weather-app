using System.Net;
using System.Text;

namespace WeatherApp.Infrastructure.UnitTests.Support;

/// <summary>Scripted HttpMessageHandler: records every request URI, then responds or throws.</summary>
public sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<Uri> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // A cancelled token means no request goes out at all.
        ct.ThrowIfCancellationRequested();
        Requests.Add(request.RequestUri!);
        return Task.FromResult(respond(request));
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Ok(string body) => Json(HttpStatusCode.OK, body);
}
