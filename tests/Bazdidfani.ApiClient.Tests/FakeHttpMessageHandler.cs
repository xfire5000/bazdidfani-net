using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bazdidfani.ApiClient.Tests
{
    /// <summary>
    /// جایگزین <c>Http::fake()</c> در تست‌های پکیج Laravel: پاسخ‌ها بر اساس بخشی از مسیر درخواست
    /// تعیین می‌شوند و همهٔ درخواست‌های ارسال‌شده برای بررسی نگهداری می‌شوند.
    /// </summary>
    internal sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<(string PathFragment, Func<int, (HttpStatusCode Status, string Body)> Responder)> _routes =
            new List<(string, Func<int, (HttpStatusCode, string)>)>();

        private readonly Dictionary<string, int> _hits = new Dictionary<string, int>();

        public List<RecordedRequest> Requests { get; } = new List<RecordedRequest>();

        public FakeHttpMessageHandler Respond(string pathFragment, string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _routes.Add((pathFragment, _ => (status, body)));

            return this;
        }

        public FakeHttpMessageHandler RespondInSequence(
            string pathFragment,
            params (HttpStatusCode Status, string Body)[] responses)
        {
            _routes.Add((pathFragment, attempt => responses[Math.Min(attempt, responses.Length - 1)]));

            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // AbsoluteUri شکل percent-encoded واقعیِ روی سیم را می‌دهد؛ ToString() آن را decode می‌کند.
            var url = request.RequestUri!.AbsoluteUri;
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync().ConfigureAwait(false);

            Requests.Add(new RecordedRequest(request.Method, url, CopyHeaders(request), body));

            foreach (var (pathFragment, responder) in _routes)
            {
                if (!url.Contains(pathFragment))
                {
                    continue;
                }

                _hits.TryGetValue(pathFragment, out var attempt);
                _hits[pathFragment] = attempt + 1;

                var (status, responseBody) = responder(attempt);

                return new HttpResponseMessage(status)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
                };
            }

            throw new InvalidOperationException($"درخواست پیش‌بینی‌نشده به {url} ارسال شد.");
        }

        private static Dictionary<string, string> CopyHeaders(HttpRequestMessage request)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var header in request.Headers)
            {
                headers[header.Key] = string.Join(",", header.Value);
            }

            return headers;
        }

        internal sealed class RecordedRequest
        {
            public RecordedRequest(HttpMethod method, string url, Dictionary<string, string> headers, string? body)
            {
                Method = method;
                Url = url;
                Headers = headers;
                Body = body;
            }

            public HttpMethod Method { get; }

            public string Url { get; }

            public Dictionary<string, string> Headers { get; }

            public string? Body { get; }

            public string? Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;
        }
    }
}
