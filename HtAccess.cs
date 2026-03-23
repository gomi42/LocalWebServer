using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace WebServer
{
    internal static class HtAccess
    {
        private const string HtaccessFilename = ".htaccess";

        // https://httpd.apache.org/docs/2.4/mod/mod_rewrite.html
        private static Regex cmdPattern = new Regex(@"^ *(?<command>[^ ]+) +(?<params>.+)$", RegexOptions.IgnoreCase);
        private static Regex enginePattern = new Regex(@"^(?<onoff>on|off) *$", RegexOptions.IgnoreCase);

        // RewriteBase url
        private static Regex basePattern = new Regex(@"^(?<url>[^ ]+)$", RegexOptions.IgnoreCase);

        // RewriteCond %{REQUEST_FILENAME} !-f
        private static Regex condPattern = new Regex(@"^(?<testString>[^ ]+) +(?<not>!)? *(?<condition>[^ ]+)( +\[(?<options>.+)\])?$", RegexOptions.IgnoreCase);

        // RewriteRule ^(.*)$ /index.php [ABC]
        private static Regex rulePattern = new Regex(@"^(?<not>!)? *(?<pattern>[^ ]+) +(?<substitution>[^ ]+)( +\[(?<options>.+)\])?$", RegexOptions.IgnoreCase);

        ////////////////////////////////////////////////////////////////////

        public static string ApplyHtaccess(string rootDir, string url, string filename)
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

            using StreamReader sr = new StreamReader(ht);
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

            return filename;
        }
    }
}
