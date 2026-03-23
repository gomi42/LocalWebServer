//
// Author:
//   Michael Göricke
//
// Copyright (c) 2025
//
// This file is part of LocalTestServer.
//
// LocalTestServer is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see<http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace WebServer
{
    public class SimpleWebServer
    {
        class ServerInfo
        {
            public string RootDir;
            public bool SendFakeServer;
        }

        private const string GomiTestServerStyle = "GomiTestServerStyle.css";
        private const string PhpExeLocation = "php\\php-cgi.exe";
        private const string PhpHost = "127.0.0.1";
        private const int PhpPort = 9000;
        private const string LocalHost = "http://localhost";

        private WebApplication webApplication;
        private FastCgiClient fastCgiClient;
        private PhpCgiManager phpCgiManager;
        private Dictionary<int, ServerInfo> prefixRootMappings;

        ////////////////////////////////////////////////////////////////////

        public const int MaxPorts = 5;
        public const int InitialPort = 8080;

        ////////////////////////////////////////////////////////////////////

        public SimpleWebServer()
        {
            prefixRootMappings = new Dictionary<int, ServerInfo>();

            var loc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string phpExecutable = Path.Combine(loc, PhpExeLocation);

            if (!File.Exists(phpExecutable))
            {
                Regex baseDirPattern = new Regex(@"^(?<basedir>.*)\\(bin\\(Debug|Release)\\.*)$", RegexOptions.IgnoreCase);
                var match = baseDirPattern.Match(loc);

                if (match.Success)
                {
                    var basedir = match.Groups["basedir"].Value;
                    phpExecutable = Path.Combine(basedir, PhpExeLocation);

                    if (!File.Exists(phpExecutable))
                    {
                        phpExecutable = string.Empty;
                    }
                }
                else
                {
                    phpExecutable = string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(phpExecutable))
            {
                phpCgiManager = new PhpCgiManager(phpExecutable, PhpHost, PhpPort);
                phpCgiManager.Start();
                fastCgiClient = new FastCgiClient(PhpHost, PhpPort);
            }

            var builder = WebApplication.CreateBuilder();
            var urls = new string[MaxPorts];

            for (int i = 0; i < MaxPorts; i++)
            {
                urls[i] = FormatHostUri(i + InitialPort);
            }

            builder.WebHost.UseUrls(urls);
            webApplication = builder.Build();

            webApplication.MapGet("{*path}",
                                    async (HttpContext context) =>
                                    {
                                        await ProcessRequest(context);
                                    });

            webApplication.StartAsync();
        }

        ////////////////////////////////////////////////////////////////////

        public void AddPort(int port)
        {
        }

        ////////////////////////////////////////////////////////////////////

        public void ClearAllPorts()
        {
            prefixRootMappings.Clear();
        }

        ////////////////////////////////////////////////////////////////////

        public void SetPortRootMapping(int port, string rootDir)
        {
            var host = port;

            if (prefixRootMappings.TryGetValue(host, out var serverInfo))
            {
                serverInfo.RootDir = rootDir;
            }
            else
            {
                prefixRootMappings.Add(host, new ServerInfo { RootDir = rootDir });
            }
        }

        ////////////////////////////////////////////////////////////////////

        public void SetFakeServer(int port, bool sendFakeServer)
        {
            var host = port;

            if (prefixRootMappings.TryGetValue(host, out var serverInfo))
            {
                serverInfo.SendFakeServer = sendFakeServer;
            }
        }

        ////////////////////////////////////////////////////////////////////

        public static string FormatHostUri(int port)
        {
            return $"{LocalHost}:{port}/";
        }

        ////////////////////////////////////////////////////////////////////

        public void Stop()
        {
            webApplication.StopAsync();
            phpCgiManager.Stop();
        }

        ////////////////////////////////////////////////////////////////////

        private async Task ProcessRequest(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string rootDir;

            var port = context.Connection.LocalPort;

            if (!prefixRootMappings.TryGetValue(port, out var serverInfo))
            {
                response.StatusCode = 404;
                return;
            }

            rootDir = serverInfo.RootDir;
            var url = request.Path.Value;

            var urlRel = url.Substring(1);
            var fileName = Path.Combine(rootDir, urlRel);
            fileName = WebUtility.UrlDecode(fileName);

            FileStream fileStream = null;

            try
            {
                // 1: our special CSS file that we pretend to exist in the root?
                if (fileName == Path.Combine(rootDir, GomiTestServerStyle))
                {
                    var sri = System.Windows.Application.GetResourceStream(new Uri(GomiTestServerStyle, UriKind.Relative));
                    await SendStreamAsResponse(sri.Stream, fileName, response);
                    return;
                }

                // 2: apply .htaccess rules
                fileName = HtAccess.ApplyHtaccess(rootDir, url, fileName);

                // 3: process PHP file?
                if (fastCgiClient != null && Path.GetExtension(fileName).ToLower() == ".php")
                {
                    string result = await fastCgiClient.ExecuteAsync(fileName, rootDir, url, serverInfo.SendFakeServer ? "www.test.com" : "localhost");
                    await SendStringAsResponse(result, ".html", response);
                    return;
                }

                // 4: is the given link an existing directory?
                if (Directory.Exists(fileName))
                {
                    var indexhtml = Path.Combine(fileName, "index.html");

                    // 4a: does an index file exist?
                    if (File.Exists(indexhtml))
                    {
                        fileStream = File.OpenRead(indexhtml);
                        await SendStreamAsResponse(fileStream, indexhtml, response);
                    }
                    else
                    {
                        // 4b: show a directory listing
                        var listing = GetDirectoryListing(rootDir, fileName);
                        await SendStringAsResponse(listing, ".html", response);
                        return;
                    }
                }

                // 5: try to return the requested file
                fileStream = File.OpenRead(fileName);
                await SendStreamAsResponse(fileStream, fileName, response);
            }
            catch (Exception)
            {
                response.StatusCode = 404;
            }
            finally
            {
                fileStream?.Close();
            }
        }

        ////////////////////////////////////////////////////////////////////

        private async Task SendStringAsResponse(string str, string fileextension, HttpResponse response)
        {
            response.ContentType = MimeHelper.GetMimeType(fileextension);
            await response.WriteAsync(str);
        }

        ////////////////////////////////////////////////////////////////////

        private async Task SendStreamAsResponse(Stream stream, string filename, HttpResponse response)
        {
            response.ContentType = MimeHelper.GetMimeType(filename);
            await stream.CopyToAsync(response.Body);
        }

        ////////////////////////////////////////////////////////////////////

        private string GetDirectoryListing(string rootDir, string directory)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine($"<link rel=\"stylesheet\" type=\"text/css\" href=\"/{GomiTestServerStyle}\" />");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"page\">");
            sb.AppendLine("<div class=\"pageinner\">");
            sb.AppendLine("<div class=\"head1\">gomi local web server</div>");

            sb.AppendLine("<div class=\"head2\">");
            string u1 = WebUtility.HtmlEncode(directory);
            sb.AppendLine($"<div>{u1}</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class=\"listingcontainer\">");
            sb.AppendLine("<table class=\"listing\">");

            string root = directory.Substring(rootDir.Length);

            if (!string.IsNullOrEmpty(root))
            {
                var s = root.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                string parent = "/";

                for (int i = 0; i < s.Length - 1; i++)
                {
                    parent += s[i] + "/";
                }

                sb.AppendLine("<tr>");

                sb.AppendLine("<td class=\"td_icon\">");
                sb.AppendLine("<div class=\"icon ico_back\"/>");
                sb.AppendLine("</td>");

                sb.AppendLine("<td class=\"td_name\">");
                var url = UrlEncode(parent);
                sb.AppendLine($"<a href=\"{url}\">..</a>");
                sb.AppendLine("</td>");

                sb.AppendLine("</tr>");
            }

            var dirs = Directory.GetDirectories(directory);
            Array.Sort(dirs);

            foreach (var dir in dirs)
            {
                sb.AppendLine("<tr>");

                sb.AppendLine("<td class=\"td_icon\">");
                sb.AppendLine("<div class=\"icon ico_folder\"/>");
                sb.AppendLine("</td>");

                sb.AppendLine("<td class=\"td_name\">");
                var filename = Path.GetFileName(dir);
                var url = UrlEncode(filename) + "/";
                string output1 = WebUtility.HtmlEncode(filename);
                sb.AppendLine($"<a href=\"{url}\">{output1}</a>");
                sb.AppendLine("</td>");

                sb.AppendLine("</tr>");
            }

            var files = Directory.GetFiles(directory);
            Array.Sort(files);

            foreach (var file in files)
            {
                sb.AppendLine("<tr>");

                var ext = Path.GetExtension(file).ToLower();

                if (!string.IsNullOrEmpty(ext))
                {
                    ext = ext.Substring(1);
                }

                string extClass;

                switch (ext)
                {
                    case "html":
                    case "htm":
                        extClass = "ico_html";
                        break;

                    case "css":
                        extClass = "ico_css";
                        break;

                    case "svg":
                    case "jpg":
                    case "tif":
                    case "tiff":
                    case "dng":
                    case "png":
                    case "bmp":
                        extClass = "ico_img";
                        break;

                    default:
                        extClass = "ico_file";
                        break;
                }

                sb.AppendLine("<td class=\"td_icon\">");
                sb.AppendLine($"<div class=\"icon {extClass}\"/>");
                sb.AppendLine("</td>");

                sb.AppendLine("<td class=\"td_name\">");
                var filename = Path.GetFileName(file);
                var url = UrlEncode(filename);
                string output1 = WebUtility.HtmlEncode(filename);
                sb.AppendLine($"<a href=\"{url}\">{output1}</a>");
                sb.AppendLine("</td>");

                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        ////////////////////////////////////////////////////////////////////

        private string UrlEncode(string url)
        {
            var e = WebUtility.UrlEncode(url);
            e = e.Replace("%2F", "/");
            return e;
        }
    }
}

