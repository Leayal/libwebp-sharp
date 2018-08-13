using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace libwebp_sharp
{
    /// <summary>
    /// Base class
    /// </summary>
    public abstract class CommandLineWrapper
    {
        private string fullPath;

        internal CommandLineWrapper(string filename)
        {
            if (File.Exists(filename))
            {
                this.fullPath = Path.GetFullPath(filename);
                return;
            }
            else if (string.Equals(Path.GetFileName(filename), filename, StringComparison.Ordinal))
            {
                List<string> dirs = null;
                string paths = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                string[] splitted;
                char[] splitter = { Helper.CharDependOS(';', ':') };
                if (!string.IsNullOrEmpty(paths))
                {
                    splitted = paths.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                    if (dirs == null)
                        dirs = new List<string>(splitted.Length);
                    dirs.AddRange(splitted);
                }
                paths = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
                if (!string.IsNullOrEmpty(paths))
                {
                    splitted = paths.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                    if (dirs == null)
                    {
                        dirs = new List<string>(splitted.Length);
                        dirs.AddRange(splitted);
                    }
                    else
                    {
                        for (int i = 0; i < splitted.Length; i++)
                        {
                            if (dirs.IndexOf(splitted[i]) == -1)
                                dirs.Add(splitted[i]);
                        }
                    }
                }

                for (int i = 0; i < dirs.Count; i++)
                {
                    paths = Path.Combine(dirs[i], filename);
                    if (File.Exists(paths))
                    {
                        this.fullPath = Path.GetFullPath(paths);
                        return;
                    }
                }
            }
            throw new FileNotFoundException($"Cannot find the file {filename}.");
        }

        /// <summary>
        /// Disable all assembly optimizations.
        /// </summary>
        public bool NoOptimizationMode { get; set; } = false;
        /// <summary>
        /// Use multi-threading for compressing if available.
        /// </summary>
        public bool Multithreading { get; set; } = true;

        protected virtual void BuildParams(IList<string> paramlist)
        {
            if (this.Multithreading)
                paramlist.Add("-mt");

            if (this.NoOptimizationMode)
                paramlist.Add("-noasm");

            paramlist.Add("-quiet");
        }

        protected virtual Process CreateProcess(string args, bool openPipe) => Helper.CreateProcess(this.fullPath, args, openPipe, openPipe);

        protected string RunAndGetText() => this.RunAndGetText(string.Empty);
        protected string RunAndGetText(string args)
        {
            string result = string.Empty;
            using (Process proc = string.IsNullOrWhiteSpace(args) ? this.CreateProcess(string.Empty, true) : this.CreateProcess(args, true))
            {
                proc.StartInfo.RedirectStandardInput = false;
                proc.Start();
                proc.WaitForExit();
                string tmp = proc.StandardOutput.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(tmp))
                    result = tmp.Trim();
            }
            return result;
        }
    }
}
