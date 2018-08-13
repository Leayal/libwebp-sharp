using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace libwebp_sharp
{
    /// <summary>
    /// Provides generic methods to decode WebP image format.
    /// </summary>
    public class DecoderWrapper : CommandLineWrapper
    {
        private readonly List<string> paramList;
        private byte[] buffer;

        /// <summary>
        /// Create a new instance by finding CLI tool from PATH and CurrentDirectory.
        /// </summary>
        public DecoderWrapper() : this(Helper.StringDependOS("dwebp.exe", "dwebp")) { }

        /// <summary>
        /// Create a new instance with provided path to the CLI tool.
        /// </summary>
        /// <param name="cli_path">The tool filename or fullpath to the tool</param>
        public DecoderWrapper(string cli_path) : base(cli_path)
        {
            this.paramList = new List<string>(15);
            this.buffer = new byte[4096];
        }

        /// <summary>
        /// Gets the version from the CLI tool.
        /// </summary>
        public string GetToolVersion()
        {
            string result = "Unknown";
            using (Process proc = this.CreateProcess("-version", true))
            {
                proc.StartInfo.RedirectStandardInput = false;
                proc.Start();
                proc.WaitForExit();
                string tmp = proc.StandardOutput.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(tmp) && tmp != "\n")
                    result = tmp.Trim();
            }
            return result;
        }

        /// <summary>
        /// Asynchronously gets the version from the CLI tool.
        /// </summary>
        public Task<string> GetCLIVersionAsync()
        {
            TaskCompletionSource<string> t = new TaskCompletionSource<string>();
            Process proc = this.CreateProcess("-version", true);
            try
            {
                proc.StartInfo.RedirectStandardInput = false;
                proc.EnableRaisingEvents = true;
                proc.Exited += (a, b) =>
                {
                    Process process = (Process)a;
                    string result = "Unknown";
                    string tmp = process.StandardOutput.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(tmp) && tmp != "\n")
                        result = tmp.Trim();
                    process.Dispose();
                    t.SetResult(result);
                };
                proc.Start();
            }
            catch
            {
                if (proc != null)
                    proc.Dispose();
                throw;
            }
            return t.Task;
        }

        /// <summary>
        /// Disable dithering.
        /// </summary>
        public bool NoDither { get; set; } = false;
        /// <summary>
        /// Disable in-loop filtering.
        /// </summary>
        public bool NoFilter { get; set; } = false;
        /// <summary>
        /// Don't use the fancy YUV420 upscaler.
        /// </summary>
        public bool NoFancy { get; set; } = false;
        private int? _ditherStrength;
        /// <summary>
        /// Dithering Strength. Accepts value from 0 to 100. Set null for 'Auto'.
        /// </summary>
        public int? DitherStrength
        {
            get => this._ditherStrength;
            set
            {
                if (value.HasValue)
                    if (value.Value < 0 || value.Value > 100)
                    {
                        throw new InvalidDataException();
                    }
                this._ditherStrength = value;
            }
        }
        /// <summary>
        /// Use alpha-plane dithering if needed.
        /// </summary>
        public bool AlphaDither { get; set; } = false;
        /// <summary>
        /// Only output the alpha-plane.
        /// </summary>
        public bool AlphaOnly { get; set; } = false;

        /// <summary>
        /// Decode the webp image from a <see cref="Stream"/> and write the output to a <see cref="Stream"/>. This method is NOT thread-safe.
        /// </summary>
        /// <param name="webpcontent">The stream contains webp data</param>
        /// <param name="output">The output stream</param>
        public void Decode(Stream webpcontent, Stream output)
        {
            using (Process proc = this.CreateProcess(this.BuildParams(string.Empty, string.Empty), true))
            {
                proc.Start();
                Helper.StreamCopy(webpcontent, proc.StandardInput.BaseStream, ref this.buffer);
                proc.StandardInput.BaseStream.Flush();
                proc.StandardInput.BaseStream.Close();
                Helper.StreamCopy(proc.StandardOutput.BaseStream, output, ref this.buffer);
            }
        }

        /// <summary>
        /// Decode the webp image from a <see cref="Stream"/> with a fixed length to read and write the output to a <see cref="Stream"/>. This method is NOT thread-safe.
        /// </summary>
        /// <param name="webpcontent">The stream contains webp data</param>
        /// <param name="bytesToReadFromStream">The length to read from <paramref name="webpcontent"/></param>
        /// <param name="output">The output stream</param>
        public void Decode(Stream webpcontent, long bytesToReadFromStream, Stream output)
        {
            using (Process proc = this.CreateProcess(this.BuildParams(string.Empty, string.Empty), true))
            {
                proc.Start();
                Helper.StreamCopy(webpcontent, bytesToReadFromStream, proc.StandardInput.BaseStream, ref this.buffer);
                proc.StandardInput.BaseStream.Flush();
                proc.StandardInput.BaseStream.Close();
                Helper.StreamCopy(proc.StandardOutput.BaseStream, output, ref this.buffer);
            }
        }

        /// <summary>
        /// Decode the webp image from a file and write the output to a <see cref="Stream"/>. This method is NOT thread-safe.
        /// </summary>
        /// <param name="webp_filepath">The path to the webp file</param>
        /// <param name="output">The output stream</param>
        public void Decode(string webp_filepath, Stream output)
        {
            using (Process proc = this.CreateProcess(this.BuildParams(webp_filepath, string.Empty), true))
            {
                proc.StartInfo.RedirectStandardInput = false;
                proc.Start();
                Helper.StreamCopy(proc.StandardOutput.BaseStream, output, ref this.buffer);
            }
        }

        /// <summary>
        /// Decode the webp image from a file and write the output to another file. This method is NOT thread-safe.
        /// </summary>
        /// <param name="webp_filepath">The path to the webp file</param>
        /// <param name="output_filepath">The path to write output file</param>
        public void Decode(string webp_filepath, string output_filepath)
        {
            FileStream fs = new FileStream(webp_filepath, FileMode.Open, FileAccess.Read, FileShare.Read, 8);
            fs.Dispose();
            using (Process proc = this.CreateProcess(this.BuildParams(webp_filepath, output_filepath), false))
            {
                proc.Start();
                proc.WaitForExit();
            }
        }

        /// <summary>
        /// Asynchronously decode the webp image from a <see cref="Stream"/> and write the output to a <see cref="Stream"/>. This method is thread-safe.
        /// </summary>
        /// <param name="webpcontent">The stream contains webp data</param>
        /// <param name="output">The output stream</param>
        /// <returns></returns>
        public Task DecodeAsync(Stream webpcontent, Stream output)
        {
            return Task.Run(() =>
            {
                var list = new List<string>(15);
                using (Process proc = this.CreateProcess(this.BuildParams(list, string.Empty, string.Empty), true))
                {
                    proc.Start();
                    byte[] buffer = new byte[4096];
                    Helper.StreamCopy(webpcontent, proc.StandardInput.BaseStream, ref buffer);
                    proc.StandardInput.BaseStream.Flush();
                    proc.StandardInput.BaseStream.Close();
                    Helper.StreamCopy(proc.StandardOutput.BaseStream, output, ref buffer);
                }
            });
        }

        /// <summary>
        /// Asynchronously decode the webp image from a <see cref="Stream"/> with a fixed length to read and write the output to a <see cref="Stream"/>. This method is thread-safe.
        /// </summary>
        /// <param name="webpcontent">The stream contains webp data</param>
        /// <param name="bytesToReadFromStream">The length to read from <paramref name="webpcontent"/></param>
        /// <param name="output">The output stream</param>
        /// <returns></returns>
        public Task DecodeAsync(Stream webpcontent, long bytesToReadFromStream, Stream output)
        {
            return Task.Run(() =>
            {
                var list = new List<string>(15);
                using (Process proc = this.CreateProcess(this.BuildParams(list, string.Empty, string.Empty), true))
                {
                    proc.Start();
                    byte[] buffer = new byte[4096];
                    Helper.StreamCopy(webpcontent, bytesToReadFromStream, proc.StandardInput.BaseStream, ref buffer);
                    proc.StandardInput.BaseStream.Flush();
                    proc.StandardInput.BaseStream.Close();
                    Helper.StreamCopy(proc.StandardOutput.BaseStream, output, ref buffer);
                }
            });
        }

        /// <summary>
        /// Asynchronously decode the webp image from a file and write the output to a <see cref="Stream"/>. This method is thread-safe.
        /// </summary>
        /// <param name="webp_filepath">The path to the webp file</param>
        /// <param name="output">The output stream</param>
        /// <returns></returns>
        public Task DecodeAsync(string webp_filepath, Stream output)
        {
            return Task.Run(() =>
            {
                FileStream fs = new FileStream(webp_filepath, FileMode.Open, FileAccess.Read, FileShare.Read, 8);
                fs.Dispose();
                var list = new List<string>(15);
                using (Process proc = this.CreateProcess(this.BuildParams(list, webp_filepath, string.Empty), true))
                {
                    proc.StartInfo.RedirectStandardInput = false;
                    proc.Start();
                    proc.StandardOutput.BaseStream.CopyTo(output);
                }
            });
        }

        /// <summary>
        /// Asynchronously decode the webp image from a file and write the output to another file. This method is thread-safe.
        /// </summary>
        /// <param name="webp_filepath">The path to the webp file</param>
        /// <param name="output_filepath">The path to write output file</param>
        /// <returns></returns>
        public Task DecodeAsync(string webp_filepath, string output_filepath)
        {
            FileStream fs = new FileStream(webp_filepath, FileMode.Open, FileAccess.Read, FileShare.Read, 8);
            fs.Dispose();
            var list = new List<string>(15);
            TaskCompletionSource<bool> t = new TaskCompletionSource<bool>();
            Process proc = this.CreateProcess(this.BuildParams(list, webp_filepath, output_filepath), false);
            try
            {
                proc.EnableRaisingEvents = true;
                proc.Exited += (a, b) =>
                {
                    ((Process)a).Dispose();
                    t.SetResult(true);
                };
                proc.Start();
            }
            catch
            {
                if (proc != null)
                    proc.Dispose();
                throw;
            }
            return t.Task;
        }

        protected string BuildParams(string input, string output)
        {
            this.paramList.Clear();
            return this.BuildParams(this.paramList, input, output);
        }

        protected string BuildParams(IList<string> list, string input, string output)
        {
            this.BuildParams(list);
            if (string.IsNullOrWhiteSpace(output))
            {
                list.Add("-o");
                list.Add("-");
            }
            else
            {
                list.Add("-o");
                list.Add(output);
            }
            if (string.IsNullOrWhiteSpace(input))
            {
                list.Add("--");
                list.Add("-");
            }
            else
            {
                list.Add("--");
                list.Add(input);
            }
            return Helper.ArgsToString(list);
        }

        protected override void BuildParams(IList<string> paramlist)
        {
            base.BuildParams(paramlist);
            if (this.NoFancy)
                paramlist.Add("-nofancy");

            if (this.NoDither)
                paramlist.Add("-nodither");

            if (this.NoFilter)
                paramlist.Add("-nofilter");

            if (this.DitherStrength.HasValue)
            {
                paramlist.Add("-dither");
                paramlist.Add(this.DitherStrength.Value.ToString());
            }

            if (this.AlphaDither)
                paramlist.Add("-alpha_dither");

            if (this.AlphaOnly)
                paramlist.Add("-alpha");
        }
    }
}
