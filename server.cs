using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Globalization;

namespace Webserver
{
    class Program
    {
        private const int port = 5050;
        private const string WebServerPath = @"www";
        private static readonly string serverEtag = Guid.NewGuid().ToString("N");

        /// <summary>
        /// starts a tcp listener & a thread to listen for incoming requests
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                var myListener = new TcpListener(IPAddress.Loopback, port);
                myListener.Start();
                Console.WriteLine($"Server running on http://{IPAddress.Loopback}:{port}/ \nQuit the server with CTRL-BREAK");
                var th = new Thread(new ThreadStart(StartListen));
                th.Start();

                /// <summary>
                /// a loop that waits for a client to connect, then reads the request from the client
                /// </summary>
                void StartListen()
                {
                    while (true)
                    {
                        using var client = myListener.AcceptTcpClient();
                        using var stream = client.GetStream();

                        // read request
                        var requestBytes = new byte[1024];
                        var bytesRead = stream.Read(requestBytes, 0, requestBytes.Length);
                        var request = Encoding.UTF8.GetString(requestBytes, 0, bytesRead);

                        // parse the headers from the request
                        var (headers, requestType) = ParseHeaders(request);
                        var requestFirstLine = requestType.Split(" ");
                        var httpVersion = requestFirstLine.LastOrDefault();
                        var contentType = headers.GetValueOrDefault("Accept");
                        var contentEncoding = headers.GetValueOrDefault("Accept-Encoding");

                        // request type check
                        if (!requestType.StartsWith("GET"))
                        {
                            WriteResponse(stream, httpVersion, 405, "Method Not Allowed", contentType, contentEncoding, null, null);
                        }
                        else
                        {
                            var requestedPath = requestFirstLine[1];
                            var fileContent = GetContent(requestedPath);
                            if (fileContent != null)
                            {
                                var requestLines = request.Split('\n');
                                var firstLine = requestLines[0].TrimEnd('\r', '\n');

                                WriteResponse(stream, httpVersion, 200, "OK", contentType, contentEncoding, firstLine, fileContent);
                            }
                            else
                            {
                                var requestLines = request.Split('\n');
                                var firstLine = requestLines[0].TrimEnd('\r', '\n');

                                WriteResponse(stream, httpVersion, 404, "Page Not Found", contentType, contentEncoding, firstLine, null);
                            }
                        }
                    }
                }
            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"Err: {error.Message}");
            }
        }

        /// <summary>
        /// returns the content of the requested file if it exists
        /// </summary>
        private static byte[]? GetContent(string requestedPath)
        {
            if (requestedPath == "/") requestedPath = "index.html";
            var filePath = Path.Combine(WebServerPath, requestedPath.TrimStart('/'));

            if (!File.Exists(filePath)) return null;

            return File.ReadAllBytes(filePath);
        }

        /// <summary>
        /// writing the response to the client
        /// </summary>
        private static void WriteResponse(NetworkStream networkStream, string? httpVersion, int statusCode, string statusMsg, string? contentType, string? contentEncoding, string? firstLine, byte[]? content)
        {
            var currentDateTime = DateTime.Now;
            var formattedDateTime = currentDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(firstLine))
            {
                Console.WriteLine($"[{formattedDateTime}] '{firstLine}' {statusCode} {statusMsg}");
            }

            var contentLength = content?.Length;
            WriteResponseHeaders(networkStream, httpVersion, statusCode, statusMsg, contentType, contentEncoding, contentLength);

            if (content != null)
            {
                networkStream.Write(content, 0, content.Length);
            }
        }

        /// <summary>
        /// writing the response headers to the network stream
        /// </summary>
        private static void WriteResponseHeaders(NetworkStream networkStream, string? httpVersion, int statusCode, string statusMsg, string? contentType, string? contentEncoding, int? contentLength)
        {
            var responseHeaderBuffer = $"HTTP/1.1 {statusCode} {statusMsg}\r\n" +
                                       $"Connection: Keep-Alive\r\n" +
                                       $"Date: {DateTime.UtcNow.ToString()}\r\n" +
                                       $"Server: Win10 PC \r\n" +
                                       $"Etag: \"{serverEtag}\"\r\n" +
                                       $"Content-Encoding: {contentEncoding}\r\n" +
                                       $"Content-Length: {contentLength}\r\n" +
                                       $"Content-Type: {contentType}\r\n\r\n";

            var responseBytes = Encoding.UTF8.GetBytes(responseHeaderBuffer);
            networkStream.Write(responseBytes, 0, responseBytes.Length);
        }

        /// <summary>
        /// parsing the headers from the request string
        /// </summary>
        private static (Dictionary<string, string> headers, string requestType) ParseHeaders(string headerString)
        {
            var headerLines = headerString.Split('\r', '\n');
            var firstLine = headerLines[0];
            var headerValues = new Dictionary<string, string>();

            for (int i = 1; i < headerLines.Length; i++)
            {
                var headerLine = headerLines[i];
                if (string.IsNullOrWhiteSpace(headerLine)) break;

                var delimiterIndex = headerLine.IndexOf(':');
                if (delimiterIndex >= 0)
                {
                    var headerName = headerLine.Substring(0, delimiterIndex).Trim();
                    var headerValue = headerLine.Substring(delimiterIndex + 1).Trim();
                    headerValues.Add(headerName, headerValue);
                }
            }

            return (headerValues, firstLine);
        }
    }
}
