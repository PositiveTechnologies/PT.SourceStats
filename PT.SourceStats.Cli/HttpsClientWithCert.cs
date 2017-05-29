using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PT.SourceStats.Cli
{
    internal class HttpsClientWithCert
    {
        static HttpsClientWithCert()
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                return true;
            };
        }

        public async Task<string> SendData(Uri address, string data)
        {
            try
            {
                using (var handler = new WebRequestHandler())
                {
                    handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    handler.ClientCertificates.Add(LoadCert());
                    using (HttpClient client = new HttpClient(handler, false))
                    {
                        byte[] jsonBytes = Encoding.UTF8.GetBytes(data);
                        MemoryStream ms = new MemoryStream();
                        using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress, true))
                        {
                            gzip.Write(jsonBytes, 0, jsonBytes.Length);
                        }
                        ms.Position = 0;
                        StreamContent content = new StreamContent(ms);
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        content.Headers.ContentEncoding.Add("gzip");
                        HttpResponseMessage response = await client.PostAsync(address, content);
                        var results = await response.Content.ReadAsStringAsync();
                        return results;
                    }
                }
            }
            catch (Exception)
            {
            }

            return "Error";
        }

        public async Task<string> GetData(Uri address)
        {
            try
            {
                using (var handler = new WebRequestHandler())
                {
                    //handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    handler.ClientCertificates.Add(LoadCert());
                    using (HttpClient client = new HttpClient(handler, false))
                    {
                        HttpResponseMessage response = await client.GetAsync(address);
                        var results = await response.Content.ReadAsStringAsync();
                        return results;
                    }
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private X509Certificate LoadCert()
        {
            return new X509Certificate("ClientApproof.pfx", "");
        }

        public async Task DownloadFileTo(string url, string path, Action<int> progress)
        {
            var client = new WebClient();
            client.DownloadProgressChanged += (s, e) => progress?.Invoke((int)Math.Ceiling(((decimal)e.BytesReceived / e.TotalBytesToReceive) * 100));
            await client.DownloadFileTaskAsync(new Uri(url), path);
        }
    }
}
