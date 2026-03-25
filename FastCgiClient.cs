using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
    public enum FcgiType : byte
    {
        BeginRequest = 1,
        AbortRequest = 2,
        EndRequest = 3,
        Params = 4,
        Stdin = 5,
        Stdout = 6,
        Stderr = 7
    }

    public struct FcgiHeader
    {
        public byte Version;
        public FcgiType Type;
        public ushort RequestId;
        public ushort ContentLength;
        public byte PaddingLength;
        public byte Reserved;

        public byte[] GetBytes()
        {
            var bytes = new byte[8];
            bytes[0] = Version;
            bytes[1] = (byte)Type;
            bytes[2] = (byte)(RequestId >> 8);
            bytes[3] = (byte)(RequestId & 0xFF);
            bytes[4] = (byte)(ContentLength >> 8);
            bytes[5] = (byte)(ContentLength & 0xFF);
            bytes[6] = PaddingLength;
            bytes[7] = Reserved;
            return bytes;
        }
    }

    public class FastCgiClient
    {
        private readonly string _host;
        private readonly int _port;

        public FastCgiClient(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public async Task<string> ExecuteAsync(string scriptFilename,
                                               string rootDir,
                                               string uri,
                                               string serverName,
                                               int port,
                                               string method = "GET",
                                               string query = "",
                                               string postData = "")
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port);
            using var stream = client.GetStream();

            ushort requestId = 1;

            // BEGIN_REQUEST
            await SendBeginRequest(stream, requestId);

            // PARAMS
            var parameters = new Dictionary<string, string>
            {
                { "SCRIPT_FILENAME", scriptFilename },
                { "REQUEST_METHOD", method },
                { "QUERY_STRING", query },
                { "CONTENT_LENGTH", postData.Length.ToString() },
                { "CONTENT_TYPE", "application/x-www-form-urlencoded" },
                { "SERVER_PROTOCOL", "HTTP/1.1" },

                { "REQUEST_URI", uri },
                { "HTTP_HOST",  $"{serverName}:{port}"},
                { "SERVER_NAME", serverName },
                { "DOCUMENT_ROOT", rootDir.Replace('\\', '/') }
            };

            var paramBytes = EncodeNameValuePairs(parameters);
            await SendRecord(stream, FcgiType.Params, requestId, paramBytes);

            // PARAMS End
            await SendRecord(stream, FcgiType.Params, requestId, Array.Empty<byte>());

            // STDIN (POST-Data)
            var stdinBytes = Encoding.UTF8.GetBytes(postData);
            await SendRecord(stream, FcgiType.Stdin, requestId, stdinBytes);

            // STDIN End
            await SendRecord(stream, FcgiType.Stdin, requestId, Array.Empty<byte>());

            // read response
            return await ReadResponse(stream);
        }

        private async Task SendBeginRequest(NetworkStream stream, ushort requestId)
        {
            byte[] body = { 0, 1, 0, 0, 0, 0, 0, 0 }; // role=1 (Responder)
            await SendRecord(stream, FcgiType.BeginRequest, requestId, body);
        }

        private async Task SendRecord(NetworkStream stream, FcgiType type, ushort requestId, byte[] content)
        {
            var header = new FcgiHeader
            {
                Version = 1,
                Type = type,
                RequestId = requestId,
                ContentLength = (ushort)content.Length,
                PaddingLength = 0,
                Reserved = 0
            };

            byte[] headerBytes = header.GetBytes();
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

            if (content.Length > 0)
            {
                await stream.WriteAsync(content, 0, content.Length);
            }
        }

        private async Task<string> ReadResponse(NetworkStream stream)
        {
            using var ms = new MemoryStream();

            while (true)
            {
                byte[] headerBytes = new byte[8];
                int read = await stream.ReadAsync(headerBytes, 0, 8);

                if (read == 0)
                {
                    break;
                }

                var header = ParseHeader(headerBytes);

                if (header.Type == FcgiType.Stdout)
                {
                    if (header.ContentLength == 0)
                    {
                        break;
                    }

                    byte[] buffer = new byte[header.ContentLength];
                    await stream.ReadExactlyAsync(buffer);
                    ms.Write(buffer, 0, buffer.Length);

                    if (header.PaddingLength > 0)
                    {
                        await stream.ReadExactlyAsync(new byte[header.PaddingLength]);
                    }
                }
                else
                {
                    // andere Record-Typen ignorieren
                    if (header.ContentLength > 0)
                    {
                        await stream.ReadExactlyAsync(new byte[header.ContentLength]);
                    }

                    if (header.PaddingLength > 0)
                    {
                        await stream.ReadExactlyAsync(new byte[header.PaddingLength]);
                    }
                }
            }

            string raw = Encoding.UTF8.GetString(ms.ToArray());

            // separate header from body
            int headerEnd = raw.IndexOf("\r\n\r\n");

            if (headerEnd >= 0)
            {
                return raw.Substring(headerEnd + 4); // return HTML only
            }

            return raw; // fallback
        }

        private FcgiHeader ParseHeader(byte[] bytes)
        {
            return new FcgiHeader
            {
                Version = bytes[0],
                Type = (FcgiType)bytes[1],
                RequestId = (ushort)((bytes[2] << 8) | bytes[3]),
                ContentLength = (ushort)((bytes[4] << 8) | bytes[5]),
                PaddingLength = bytes[6],
                Reserved = bytes[7]
            };
        }

        private static byte[] EncodeNameValuePairs(Dictionary<string, string> pairs)
        {
            using var ms = new MemoryStream();

            foreach (var kv in pairs)
            {
                byte[] name = Encoding.UTF8.GetBytes(kv.Key);
                byte[] value = Encoding.UTF8.GetBytes(kv.Value);

                WriteLength(ms, name.Length);
                WriteLength(ms, value.Length);

                ms.Write(name, 0, name.Length);
                ms.Write(value, 0, value.Length);
            }

            return ms.ToArray();
        }

        private static void WriteLength(Stream stream, int length)
        {
            if (length < 128)
            {
                stream.WriteByte((byte)length);
            }
            else
            {
                stream.WriteByte((byte)((length >> 24) | 0x80));
                stream.WriteByte((byte)(length >> 16));
                stream.WriteByte((byte)(length >> 8));
                stream.WriteByte((byte)length);
            }
        }
    }
}
