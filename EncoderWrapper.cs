using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace libwebp_sharp
{
    /// <summary>
    /// Provides generic methods to encode image to WebP image format.
    /// </summary>
    public class EncoderWrapper : CommandLineWrapper
    {
        private readonly List<string> paramList;
        private byte[] buffer;

        /// <summary>
        /// Create a new instance by finding CLI tool from PATH and CurrentDirectory.
        /// </summary>
        public EncoderWrapper() : this(Helper.StringDependOS("cwebp.exe", "cwebp")) { }

        /// <summary>
        /// Create a new instance with provided path to the CLI tool.
        /// </summary>
        /// <param name="cli_path">The tool filename or fullpath to the tool</param>
        public EncoderWrapper(string cli_path) : base(cli_path)
        {
            this.paramList = new List<string>(30);
            this.buffer = new byte[4096];
        }

        private int? _quality;
        /// <summary>
        /// Quality factor. Allowed value from 0 to 100. 0=Lowest quality but fastest compression speed. Set to null to use default value.
        /// </summary>
        public int? Quality
        {
            get => this._quality;
            set
            {
                if (value.HasValue)
                {
                    if (value.Value < 0 || value.Value > 100)
                        throw new NotSupportedException();
                }
                this._quality = value;
            }
        }
        private int? _compressionLevel;
        /// <summary>
        /// Compression level. Allowed value from 0 to 6. 0=Fastest. Set to null to use default value.
        /// </summary>
        public int? CompressionLevel
        {
            get => this._compressionLevel;
            set
            {
                if (value.HasValue)
                {
                    if (value.Value < 0 || value.Value > 6)
                        throw new NotSupportedException();
                }
                this._compressionLevel = value;
            }
        }
        /// <summary>
        /// Use simple filter instead of strong.
        /// </summary>
        public bool NoStrong { get; set; } = false;
        /// <summary>
        /// The pre-defined profile for all the properties of this instance. Setting any properties in this instance will make the properties overrides the ones from the profile (NOT overrides the whole profile).
        /// </summary>
        public WebpPreset Preset { get; set; } = WebpPreset.Default;
        /// <summary>
        /// Reduce memory usage (Slower encoding)
        /// </summary>
        public bool LowMemoryMode { get; set; } = false;
        /// <summary>
        /// Ignores all transparency information.
        /// </summary>
        public bool NoAlphaMode { get; set; } = false;
        private int? _alphaQuality;
        /// <summary>
        /// Transparency-compression quality. Allowed value from 0 to 100. 0=Lowest quality. Set to null to use default value.
        /// </summary>
        public int? AlphaQuality
        {
            get => this._alphaQuality;
            set
            {
                if (value.HasValue)
                {
                    if (value.Value < 0 || value.Value > 100)
                        throw new NotSupportedException();
                }
                this._alphaQuality = value;
            }
        }
        /// <summary>
        /// Encoding algorithm.
        /// </summary>
        public CompressionMethod CompressionMethod { get; set; } = CompressionMethod.Lossy;
        /// <summary>
        /// Set the value to determine which metadata will be copied from source.
        /// </summary>
        public MetadataType MetadataType { get; set; } = MetadataType.None;
        /// <summary>
        /// Transparency-compression method. (I don't know what this does)
        /// </summary>
        public bool AlphaMethod { get; set; } = true;
        /// <summary>
        /// Predictive filtering for alpha plane.
        /// </summary>
        public AlphaFilter AlphaFilter { get; set; } = AlphaFilter.Fast;
        /// <summary>
        /// Use sharper (and slower) RGB->YUV conversion.
        /// </summary>
        public bool SharperYUV { get; set; } = false;
        /// <summary>
        /// Lossless preset with given level. (I don't know what this does)
        /// </summary>
        public LosslessPreset LosslessPreset { get; set; } = LosslessPreset.Fastest;
        /// <summary>
        /// Gets the version from the CLI tool.
        /// </summary>
        /// <returns></returns>
        public string GetToolVersion()
        {
            string result = "Unknown";
            using (Process proc = CreateProcess("-version", true))
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
        public Task<string> GetToolVersionAsync()
        {
            TaskCompletionSource<string> t = new TaskCompletionSource<string>();
            Process proc = CreateProcess("-version", true);
            try
            {
                proc.EnableRaisingEvents = true;
                proc.StartInfo.RedirectStandardInput = false;
                proc.Exited += (a, b) =>
                {
                    string result = "Unknown";
                    Process process = (Process)a;
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
        /// Encode the image from a <see cref="Stream"/> and write the output to a <see cref="Stream"/>. This method is NOT thread-safe.
        /// </summary>
        /// <param name="imagedata">The stream contains image data</param>
        /// <param name="output_webp">The output stream</param>
        public void Encode(Stream imagedata, Stream output_webp)
        {
            using (Process proc = this.CreateProcess(this.BuildParams(string.Empty, string.Empty), true))
            {
                proc.Start();
                Helper.StreamCopy(imagedata, proc.StandardInput.BaseStream, ref this.buffer);
                proc.StandardInput.BaseStream.Flush();
                proc.StandardInput.BaseStream.Close();
                Helper.StreamCopy(proc.StandardOutput.BaseStream, output_webp, ref this.buffer);
            }
        }

        /// <summary>
        /// Encode the image from a <see cref="Stream"/> with a fixed length to read and write the output to a <see cref="Stream"/>. This method is NOT thread-safe.
        /// </summary>
        /// <param name="imagedata">The stream contains image data</param>
        /// <param name="bytesToReadFromStream">The length to read from <paramref name="webpcontent"/></param>
        /// <param name="output_webp">The output stream</param>
        public void Encode(Stream imagedata, long bytesToReadFromStream, Stream output_webp)
        {
            using (Process proc = this.CreateProcess(this.BuildParams(string.Empty, string.Empty), true))
            {
                proc.Start();
                Helper.StreamCopy(imagedata, bytesToReadFromStream, proc.StandardInput.BaseStream, ref this.buffer);
                proc.StandardInput.BaseStream.Flush();
                proc.StandardInput.BaseStream.Close();
                Helper.StreamCopy(proc.StandardOutput.BaseStream, output_webp, ref this.buffer);
            }
        }

        /// <summary>
        /// Encode the image from a file and write the output to a <see cref="Stream"/>. This method is NOT thread-safe.
        /// </summary>
        /// <param name="image_filepath">The path to the image file</param>
        /// <param name="output_webp">The output stream</param>
        public void Encode(string image_filepath, Stream output_webp)
        {
            using (Process proc = this.CreateProcess(this.BuildParams(image_filepath, string.Empty), true))
            {
                proc.StartInfo.RedirectStandardInput = false;
                proc.Start();
                Helper.StreamCopy(proc.StandardOutput.BaseStream, output_webp, ref this.buffer);
            }
        }

        /// <summary>
        /// Encode the image from a file and write the output to another file. This method is NOT thread-safe.
        /// </summary>
        /// <param name="image_filepath">The path to the image file</param>
        /// <param name="output_filepath">The path to write output file</param>
        public void Encode(string image_filepath, string output_filepath)
        {
            FileStream fs = new FileStream(image_filepath, FileMode.Open, FileAccess.Read, FileShare.Read, 8);
            fs.Dispose();
            using (Process proc = this.CreateProcess(this.BuildParams(image_filepath, output_filepath), false))
            {
                proc.Start();
                proc.WaitForExit();
            }
        }

        /// <summary>
        /// Asynchronously encode the image from a <see cref="Stream"/> and write the output to a <see cref="Stream"/>. This method is thread-safe.
        /// </summary>
        /// <param name="imagedata">The stream contains image data</param>
        /// <param name="output">The output stream</param>
        /// <returns></returns>
        public Task EncodeAsync(Stream imagedata, Stream output)
        {
            return Task.Run(() =>
            {
                var list = new List<string>(15);
                using (Process proc = this.CreateProcess(this.BuildParams(list, string.Empty, string.Empty), true))
                {
                    proc.Start();
                    byte[] buffer = new byte[4096];
                    Helper.StreamCopy(imagedata, proc.StandardInput.BaseStream, ref buffer);
                    proc.StandardInput.BaseStream.Flush();
                    proc.StandardInput.BaseStream.Close();
                    Helper.StreamCopy(proc.StandardOutput.BaseStream, output, ref buffer);
                }
            });
        }

        /// <summary>
        /// Asynchronously encode the image from a <see cref="Stream"/> with a fixed length to read and write the output to a <see cref="Stream"/>. This method is thread-safe.
        /// </summary>
        /// <param name="imagedata">The stream contains image data</param>
        /// <param name="bytesToReadFromStream">The length to read from <paramref name="webpcontent"/></param>
        /// <param name="output">The output stream</param>
        /// <returns></returns>
        public Task EncodeAsync(Stream imagedata, long bytesToReadFromStream, Stream output)
        {
            return Task.Run(() =>
            {
                var list = new List<string>(15);
                using (Process proc = this.CreateProcess(this.BuildParams(list, string.Empty, string.Empty), true))
                {
                    proc.Start();
                    byte[] buffer = new byte[4096];
                    Helper.StreamCopy(imagedata, bytesToReadFromStream, proc.StandardInput.BaseStream, ref buffer);
                    proc.StandardInput.BaseStream.Flush();
                    proc.StandardInput.BaseStream.Close();
                    Helper.StreamCopy(proc.StandardOutput.BaseStream, output, ref buffer);
                }
            });
        }

        /// <summary>
        /// Asynchronously encode the image from a file and write the output to a <see cref="Stream"/>. This method is thread-safe.
        /// </summary>
        /// <param name="image_filepath">The path to the image file</param>
        /// <param name="output">The output stream</param>
        /// <returns></returns>
        public Task EncodeAsync(string image_filepath, Stream output)
        {
            return Task.Run(() =>
            {
                FileStream fs = new FileStream(image_filepath, FileMode.Open, FileAccess.Read, FileShare.Read, 8);
                fs.Dispose();
                var list = new List<string>(15);
                using (Process proc = this.CreateProcess(this.BuildParams(list, image_filepath, string.Empty), true))
                {
                    proc.StartInfo.RedirectStandardInput = false;
                    proc.Start();
                    proc.StandardOutput.BaseStream.CopyTo(output);
                }
            });
        }

        /// <summary>
        /// Asynchronously encode the image from a file and write the output to another file. This method is thread-safe.
        /// </summary>
        /// <param name="image_filepath">The path to the image file</param>
        /// <param name="output_filepath">The path to write output file</param>
        /// <returns></returns>
        public Task EncodeAsync(string image_filepath, string output_filepath)
        {
            FileStream fs = new FileStream(image_filepath, FileMode.Open, FileAccess.Read, FileShare.Read, 8);
            fs.Dispose();
            var list = new List<string>(15);
            TaskCompletionSource<bool> t = new TaskCompletionSource<bool>();
            Process proc = this.CreateProcess(this.BuildParams(list, image_filepath, output_filepath), false);
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

        protected override void BuildParams(IList<string> list)
        {
            list.Clear();
            if (this.Preset != WebpPreset.Default)
            {
                list.Add("-preset");
                list.Add(this.Preset.ToString().ToLower());
            }
            
            if (this.CompressionLevel.HasValue)
            {
                list.Add("-m");
                list.Add(this.CompressionLevel.ToString());
            }

            if (this.NoAlphaMode)
            {
                list.Add("-noalpha");
            }
            else
            {
                if (this.AlphaQuality.HasValue)
                {
                    list.Add("-alpha_q");
                    list.Add(this.AlphaQuality.ToString());
                }
            }

            switch (this.CompressionMethod)
            {
                case CompressionMethod.Lossless:
                    list.Add("-lossless");
                    if (this.Quality.HasValue)
                    {
                        list.Add("-q");
                        list.Add(this.Quality.ToString());
                    }
                    break;
                case CompressionMethod.NearLossless:
                    if (this.Quality.HasValue)
                    {
                        list.Add("-near_lossless");
                        list.Add(this.Quality.ToString());
                    }
                    else
                    {
                        list.Add("-near_lossless");
                        list.Add("100");
                    }
                    break;
                default:
                    if (this.Quality.HasValue)
                    {
                        list.Add("-q");
                        list.Add(this.Quality.ToString());
                    }

                    if (!this.NoAlphaMode)
                    {
                        list.Add("-alpha_method");
                        if (this.AlphaMethod)
                            list.Add("1");
                        else
                            list.Add("0");

                        list.Add("-alpha_filter");
                        list.Add(this.AlphaFilter.ToString().ToLower());
                    }
                    break;
            }

            if (this.NoStrong)
                list.Add("-nostrong");

            if (this.SharperYUV)
                list.Add("-sharp_yuv");

            if (this.Multithreading)
                list.Add("-mt");

            if (this.LowMemoryMode)
                list.Add("-low_memory");

            if (this.NoOptimizationMode)
                list.Add("-noasm");

            if (this.MetadataType != MetadataType.None)
            {
                if (this.MetadataType == MetadataType.All)
                {
                    list.Add("-metadata");
                    list.Add("all");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    if ((this.MetadataType & MetadataType.Exif) == MetadataType.Exif)
                    {
                        sb.Append("exif");
                    }
                    if ((this.MetadataType & MetadataType.Icc) == MetadataType.Icc)
                    {
                        if (sb.Length == 0)
                            sb.Append("icc");
                        else
                            sb.Append(",icc");
                    }
                    if ((this.MetadataType & MetadataType.Xmp) == MetadataType.Xmp)
                    {
                        if (sb.Length == 0)
                            sb.Append("xmp");
                        else
                            sb.Append(",xmp");
                    }
                    list.Add("-metadata");
                    list.Add(sb.ToString());
                }
            }
        }
    }
}
