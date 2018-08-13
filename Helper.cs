using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace libwebp_sharp
{
    static class Helper
    {
        public static void StreamCopy(Stream source, Stream destination, int buffersize)
        {
            byte[] buffer = new byte[buffersize];
            StreamCopy(source, destination, ref buffer);
        }

        public static void StreamCopy(Stream source, Stream destination, ref byte[] buffer)
        {
            int byteread = source.Read(buffer, 0, buffer.Length);
            while (byteread > 0)
            {
                destination.Write(buffer, 0, byteread);
                byteread = source.Read(buffer, 0, buffer.Length);
            }
        }

        public static void StreamCopy(Stream source, long length, Stream destination, int buffersize)
        {
            byte[] buffer = new byte[buffersize];
            StreamCopy(source, length, destination, ref buffer);
        }

        public static void StreamCopy(Stream source, long length, Stream destination, ref byte[] buffer)
        {
            int bytesRead;
            long bytesLeftToCopy = length;
            while (bytesLeftToCopy > 0)
            {
                bytesRead = source.Read(buffer, 0, Math.Min((int)bytesLeftToCopy, buffer.Length));
                if (bytesRead == 0)
                {
                    destination.Flush();
                    break;
                }

                destination.Write(buffer, 0, bytesRead);
                bytesLeftToCopy -= bytesRead;
            }
        }

        public static Process CreateProcess(string filename, string args, bool redirect_stdin, bool redirect_stdout)
        {
            Process proc = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = filename,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardInput = redirect_stdin,
                    RedirectStandardOutput = redirect_stdout
                },
                EnableRaisingEvents = false
            };
            return proc;
        }

        public static string ArgsToString(IList<string> args)
        {
            if (args.Count == 0) return string.Empty;
            int totalcharacters = 0, i;

            for (i = 0; i < args.Count; i++)
            {
                if (args[i].IndexOf(' ') != -1)
                {
                    totalcharacters += (args[i].Length + 3);
                }
                else
                {
                    totalcharacters += (args[i].Length + 1);
                }
            }

            StringBuilder sb = new StringBuilder(totalcharacters);

            for (i = 0; i < args.Count; i++)
            {
                if (i != 0)
                {
                    sb.Append(' ');
                }
                if (args[i].IndexOf(' ') != -1)
                {
                    sb.Append('"');
                    sb.Append(args[i]);
                    sb.Append('"');
                }
                else
                {
                    sb.Append(args[i]);
                }
            }

            return sb.ToString();
        }

        public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        /// <summary>
        /// I don't use OSX, so I don't know how to make it work for OSX.
        /// </summary>
        public static char CharDependOS(char onWindows, char onLinux)
        {
            if (IsWindows)
                return onWindows;
            else if (IsLinux)
                return onLinux;
            else
                throw new NotSupportedException();
        }
        /// <summary>
        /// I don't use OSX, so I don't know how to make it work for OSX.
        /// </summary>
        public static string StringDependOS(string onWindows, string onLinux)
        {
            if (IsWindows)
                return onWindows;
            else if (IsLinux)
                return onLinux;
            else
                throw new NotSupportedException();
        }
        /// <summary>
        /// I don't use OSX, so I don't know how to make it work for OSX.
        /// </summary>
        public static int NumberDependOS(int onWindows, int onLinux)
        {
            if (IsWindows)
                return onWindows;
            else if (IsLinux)
                return onLinux;
            else
                throw new NotSupportedException();
        }
    }
}
