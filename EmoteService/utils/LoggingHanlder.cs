using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class LoggingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Log request details
        Debug.WriteLine($"Request: {request.Method} {request.RequestUri}");
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            Debug.WriteLine($"Request Content: {content}");
        }

        // Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // Log response details
        Debug.WriteLine($"Response: {(int)response.StatusCode} {response.ReasonPhrase}");
        if (response.Content != null)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"Response Content: {responseContent}");
        }

        return response;
    }
}

