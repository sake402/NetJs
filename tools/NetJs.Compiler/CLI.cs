using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace NetJs.Compiler
{
    public static class CLIExtension
    {
        public static Task<(int ExitCode, string StdOut)> CLI(this string command, bool runDirect = false)
        {
            return Task.Run(() =>
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                if (OperatingSystem.IsWindows())
                {
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = "/c " + command;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }
                else if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
                {
                    startInfo.FileName = "/bin/sh";
                    // Enclose command in quotes to ensure arguments stay together on Unix systems
                    startInfo.Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"";
                }
                else
                {
                    throw new PlatformNotSupportedException("Unsupported operating system.");
                }
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true; // Recommended: capture errors too
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true; // Cleaner than WindowStyle.Hidden on Unix

                process.StartInfo = startInfo;
                process.Start();

                // 3. Read output streams
                var response = process.StandardOutput.ReadToEnd();
                var errorResponse = process.StandardError.ReadToEnd();

                process.WaitForExit();

                // If the command failed, you might want to combine error text with stdout
                if (process.ExitCode != 0 && !string.IsNullOrEmpty(errorResponse))
                {
                    response = $"{response}\nError: {errorResponse}".Trim();
                }

                return (process.ExitCode, response);
                //startInfo.FileName = "cmd.exe";
                //startInfo.Arguments = "/c " + command;
                //startInfo.RedirectStandardOutput = true;
                //startInfo.UseShellExecute = false;
                //process.StartInfo = startInfo;
                //process.Start();
                //var response = process.StandardOutput.ReadToEnd();
                //process.WaitForExit();
                //return (process.ExitCode, response);

                ////Console.WriteLine($"Executing \"{command}\"");
                //Process cmd = new Process();
                //cmd.StartInfo.FileName = !runDirect ? "cmd.exe" : command;
                //cmd.StartInfo.RedirectStandardInput = true;
                //cmd.StartInfo.RedirectStandardOutput = true;
                //cmd.StartInfo.CreateNoWindow = true;
                //cmd.StartInfo.UseShellExecute = false;
                //cmd.Start();

                //if (!runDirect)
                //{
                //    cmd.StandardInput.WriteLine(command);
                //}
                //cmd.StandardInput.Flush();
                //cmd.StandardInput.Close();
                //cmd.WaitForExit();
                //string response = cmd.StandardOutput.ReadToEnd();
                ////Console.WriteLine($"Executed with code \"{cmd.ExitCode}\" -> \"{response}\"");
                //return (cmd.ExitCode, response);
            });
        } 
    }
}
