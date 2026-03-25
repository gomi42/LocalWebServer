using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WebServer
{
    public class PhpCgiManager : IDisposable
    {
        private Process process;
        private readonly string phpCgiPath;
        private readonly string bindAddress;
        private readonly Dictionary<string, string> env;

        public PhpCgiManager(string phpCgiPath, string host, int port, Dictionary<string, string> env = null)
        {
            this.phpCgiPath = phpCgiPath;
            this.bindAddress = $"{host}:{port}";
            this.env = env ?? new Dictionary<string, string>();
        }

        public void Start()
        {
            if (process != null && !process.HasExited)
            {
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = phpCgiPath,
                Arguments = "-b " + bindAddress,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // set Environment-Variables
            foreach (var kv in env)
            {
                psi.Environment[kv.Key] = kv.Value;
            }

            process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.Exited += (s, e) =>
            {
                Restart();
            };

            process.Start();
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public void Stop()
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch { }

            process = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
