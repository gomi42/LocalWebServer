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
using System.Diagnostics;
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
        private const string GomiTestServerStyle = "GomiTestServerStyle.css";
        private const string HtaccessFilename = ".htaccess";
        private const string PhpExeLocation = "php\\php.exe";
        private const string LocalHost = "http://localhost";

        private WebApplication webApplication;
        private string phpExecutable;
        private Dictionary<int, string> prefixRootMappings;

        ////////////////////////////////////////////////////////////////////

        public const int MaxPorts = 5;
        public const int InitialPort = 8080;

        ////////////////////////////////////////////////////////////////////

        public SimpleWebServer()
        {
            prefixRootMappings = new Dictionary<int, string>();

            var loc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            phpExecutable = Path.Combine(loc, PhpExeLocation);

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

            if (prefixRootMappings.ContainsKey(host))
            {
                prefixRootMappings[host] = rootDir;
            }
            else
            {
                prefixRootMappings.Add(host, rootDir);
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
        }

        ////////////////////////////////////////////////////////////////////

        private async Task ProcessRequest(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            var port = context.Connection.LocalPort;

            if (!prefixRootMappings.TryGetValue(port, out string rootDir))
            {
                response.StatusCode = 404;
                return;
            }

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
                fileName = ApplyHtaccess(rootDir, url, fileName);

                // 3: process PHP file?
                if (!string.IsNullOrEmpty(phpExecutable) && Path.GetExtension(fileName).ToLower() == ".php")
                {
                    ExecutePhp(rootDir, fileName, url, out string result);
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

        ////////////////////////////////////////////////////////////////////

        public int ExecutePhp(string rootDir, string phpFile, string uri, out string result)
        {
            Process myProcess = new Process();

            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.CreateNoWindow = true;
            myProcess.StartInfo.FileName = phpExecutable;
            myProcess.StartInfo.Arguments = $"-f {phpFile}";
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.WorkingDirectory = rootDir;
            myProcess.StartInfo.EnvironmentVariables.Add("REQUEST_URI", uri);
            myProcess.StartInfo.EnvironmentVariables.Add("PHP_DOCUMENT_ROOT", rootDir.Replace('\\', '/'));

            myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            myProcess.Start();
            result = myProcess.StandardOutput.ReadToEnd();
            myProcess.WaitForExit();

            if (myProcess.ExitCode != 0)
            {
            }

            return myProcess.ExitCode;
        }

        ////////////////////////////////////////////////////////////////////

        // https://httpd.apache.org/docs/2.4/mod/mod_rewrite.html
        private Regex cmdPattern = new Regex(@"^ *(?<command>[^ ]+) +(?<params>.+)$", RegexOptions.IgnoreCase);
        private Regex enginePattern = new Regex(@"^(?<onoff>on|off) *$", RegexOptions.IgnoreCase);

        // RewriteBase url
        private Regex basePattern = new Regex(@"^(?<url>[^ ]+)$", RegexOptions.IgnoreCase);

        // RewriteCond %{REQUEST_FILENAME} !-f
        private Regex condPattern = new Regex(@"^(?<testString>[^ ]+) +(?<not>!)? *(?<condition>[^ ]+)( +\[(?<options>.+)\])?$", RegexOptions.IgnoreCase);

        // RewriteRule ^(.*)$ /index.php [ABC]
        private Regex rulePattern = new Regex(@"^(?<not>!)? *(?<pattern>[^ ]+) +(?<substitution>[^ ]+)( +\[(?<options>.+)\])?$", RegexOptions.IgnoreCase);

        public string ApplyHtaccess(string rootDir, string url, string filename)
        {
            var ht = Path.Combine(rootDir, HtaccessFilename);

            if (!File.Exists(ht))
            {
                return filename;
            }

            bool isEngineOn = false;
            bool isConditionSet = false;
            bool conditionValue = false;
            bool isOrSet = false;
            string baseDir = string.Empty;

            using (StreamReader sr = new StreamReader(ht))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    var match = cmdPattern.Match(line);

                    if (match.Success)
                    {
                        var command = match.Groups["command"].Value;

                        if (command.StartsWith("#"))
                        {
                            continue;
                        }

                        var parameters = match.Groups["params"].Value;

                        switch (command)
                        {
                            case "RewriteEngine":
                            {
                                var match2 = enginePattern.Match(parameters);

                                if (match2.Success)
                                {
                                    var onOff = match2.Groups["onoff"].Value.ToLower();

                                    if (onOff != "on")
                                    {
                                        return filename;
                                    }

                                    isEngineOn = true;
                                }
                                else
                                {
                                    return filename;
                                }
                                break;
                            }

                            case "RewriteBase":
                            {
                                var match2 = basePattern.Match(parameters);

                                if (match2.Success)
                                {
                                    baseDir = match2.Groups["url"].Value;
                                }
                                break;
                            }

                            case "RewriteCond":
                            {

                                if (!isEngineOn)
                                {
                                    break;
                                }

                                var match2 = condPattern.Match(parameters);

                                if (match2.Success)
                                {
                                    var testString = match2.Groups["testString"].Value;
                                    var not = !string.IsNullOrEmpty(match2.Groups["not"].Value);
                                    var condition = match2.Groups["condition"].Value;
                                    var options = match2.Groups["options"].Value.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                    var nocase = options.Contains("NC") || options.Contains("nocase");

                                    if (testString == "%{REQUEST_FILENAME}" && condition == "-f")
                                    {
                                        var exists = File.Exists(filename);

                                        if (not)
                                        {
                                            exists = !exists;
                                        }

                                        if (!isConditionSet)
                                        {
                                            isConditionSet = true;
                                            conditionValue = exists;
                                        }
                                        else
                                        {
                                            if (isOrSet)
                                            {
                                                conditionValue |= exists;
                                            }
                                            else
                                            {
                                                conditionValue &= exists;
                                            }
                                        }

                                        isOrSet = options.Contains("OR") || options.Contains("ornext");
                                    }
                                }
                                break;
                            }

                            case "RewriteRule":
                            {
                                if (!isEngineOn || isConditionSet && !conditionValue)
                                {
                                    isConditionSet = false;
                                    break;
                                }

                                isConditionSet = false;
                                var match2 = rulePattern.Match(parameters);

                                if (match2.Success)
                                {
                                    var not = !string.IsNullOrEmpty(match2.Groups["not"].Value);
                                    var pattern = match2.Groups["pattern"].Value;
                                    var substitution = match2.Groups["substitution"].Value;
                                    var options = match2.Groups["options"].Value;

                                    Regex patternMatcher = new Regex(pattern, RegexOptions.IgnoreCase);
                                    var match3 = patternMatcher.Match(url);

                                    var success = match3.Success;

                                    if (not)
                                    {
                                        success = !success;
                                    }

                                    if (success)
                                    {
                                        string urlRel;

                                        if (substitution[0] == '/')
                                        {
                                            urlRel = substitution.Substring(1);
                                        }
                                        else
                                        {
                                            urlRel = baseDir + substitution;

                                            if (urlRel[0] == '/')
                                            {
                                                urlRel = urlRel.Substring(1);
                                            }
                                        }

                                        var fileName = Path.Combine(rootDir, urlRel);
                                        fileName = WebUtility.UrlDecode(fileName);

                                        return fileName;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }

            return filename;
        }
    }
}

