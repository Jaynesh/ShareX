﻿#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright © 2007-2015 ShareX Developers

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using ShareX.ScreenCaptureLib.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ShareX.ScreenCaptureLib
{
    public class FFmpegHelper : ExternalCLIManager
    {
        public static readonly int libmp3lame_qscale_end = 9;
        public static readonly string SourceNone = "None";
        public static readonly string SourceGDIGrab = "GDI grab";

        public StringBuilder Output { get; private set; }
        public ScreencastOptions Options { get; private set; }

        public FFmpegHelper(ScreencastOptions options)
        {
            Output = new StringBuilder();
            OutputDataReceived += FFmpegHelper_DataReceived;
            ErrorDataReceived += FFmpegHelper_DataReceived;
            Options = options;
            Helpers.CreateDirectoryIfNotExist(Options.OutputPath);
        }

        private void FFmpegHelper_DataReceived(object sender, DataReceivedEventArgs e)
        {
            lock (this)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Output.AppendLine(e.Data);
                }
            }
        }

        public bool Record()
        {
            int errorCode = Open(Options.FFmpeg.CLIPath, Options.GetFFmpegCommands());
            bool result = errorCode == 0;
            if (Options.FFmpeg.ShowError && !result)
            {
                new OutputBox(Output.ToString(), Resources.FFmpegHelper_Record_FFmpeg_error).ShowDialog();
            }
            return result;
        }

        public DirectShowDevices GetDirectShowDevices()
        {
            DirectShowDevices devices = new DirectShowDevices();

            if (File.Exists(Options.FFmpeg.CLIPath))
            {
                string arg = "-list_devices true -f dshow -i dummy";
                Open(Options.FFmpeg.CLIPath, arg);
                string output = Output.ToString();
                string[] lines = output.Lines();
                bool isVideo = true;
                Regex regex = new Regex("\\[dshow @ \\w+\\]  \"(.+)\"", RegexOptions.Compiled | RegexOptions.CultureInvariant);
                foreach (string line in lines)
                {
                    if (line.EndsWith("] DirectShow video devices", StringComparison.InvariantCulture))
                    {
                        isVideo = true;
                        continue;
                    }

                    if (line.EndsWith("] DirectShow audio devices", StringComparison.InvariantCulture))
                    {
                        isVideo = false;
                        continue;
                    }

                    Match match = regex.Match(line);

                    if (match.Success)
                    {
                        string value = match.Groups[1].Value;

                        if (isVideo)
                        {
                            devices.VideoDevices.Add(value);
                        }
                        else
                        {
                            devices.AudioDevices.Add(value);
                        }
                    }
                }
            }

            return devices;
        }

        public override void Close()
        {
            WriteInput("q");
        }
    }

    public class DirectShowDevices
    {
        public List<string> VideoDevices = new List<string>();
        public List<string> AudioDevices = new List<string>();
    }
}