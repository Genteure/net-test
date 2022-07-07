using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace NetworkDiagnostics
{
    public static class HttpReq
    {
        public static async Task Run(string url)
        {
            Console.WriteLine("\n");

            var uri = new Uri(url);
            var ips = Array.Empty<IPAddress>();
            try
            {
                ips = await Dns.GetHostAddressesAsync(uri.DnsSafeHost).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine($"DNS 结果：{uri.DnsSafeHost}");
            var options = new JsonSerializerOptions
            {
                Converters =
                {
                    new IPAddressToStringJsonConverter(),
                    new ExceptionToStringJsonConverter()
                }
            };
            Console.WriteLine(JsonSerializer.Serialize(ips, options));

            var list = new List<Task<HttpResult>>();

            list.Add(Run2(url, null));

            if (ips.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork) is { } v4)
            {
                list.Add(Run2(url, v4));
            }
            if (ips.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetworkV6) is { } v6)
            {
                list.Add(Run2(url, v6));
            }

            var results = await Task.WhenAll(list).ConfigureAwait(false);
            Console.WriteLine($"请求结果");
            Console.WriteLine(JsonSerializer.Serialize(results, options));
        }

        public static async Task<HttpResult> Run2(string url, IPAddress? ip)
        {
            var client = CreateHttpClient();

            var builder = ip is null ? new UriBuilder(url) : new UriBuilder(url)
            {
                Host = ip.ToString(),
            };

            var request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
            if (ip is not null)
            {
                var originalUri = new Uri(url);
                request.Headers.Host = originalUri.IsDefaultPort ? originalUri.Host : originalUri.Host + ":" + originalUri.Port;
            }
            try
            {
                var resp = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new HttpResult
                {
                    Url = url,
                    IP = ip,
                    Success = false,
                    Exception = ex,
                };
            }
            return new HttpResult
            {
                Success = true,
                Url = url,
                IP = ip,
            };
        }

        private const string HttpHeaderAccept = "*/*";
        private const string HttpHeaderOrigin = "https://live.bilibili.com";
        private const string HttpHeaderReferer = "https://live.bilibili.com/";
        private const string HttpHeaderUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.54 Safari/537.36";

        private static HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
            });
            var headers = httpClient.DefaultRequestHeaders;
            headers.Add("Accept", HttpHeaderAccept);
            headers.Add("Origin", HttpHeaderOrigin);
            headers.Add("Referer", HttpHeaderReferer);
            headers.Add("User-Agent", HttpHeaderUserAgent);
            return httpClient;
        }
    }

    public class HttpResult
    {
        public string Url { get; set; } = string.Empty;
        public IPAddress? IP { get; set; }
        public bool Success { get; set; }
        public Exception? Exception { get; set; }
    }
}
