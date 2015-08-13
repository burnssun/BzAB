using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using Microsoft.WindowsAPICodePack.Shell;


namespace BzAB
{
    public class AutoBackupModel
    {

        internal struct LASTINPUTINFO
        {
            public uint cbSize;

            public uint dwTime;
        }

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public uint GetIdleTime()
        {
            LASTINPUTINFO lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
            GetLastInputInfo(ref lastInPut);

            return ((uint)Environment.TickCount - lastInPut.dwTime);
        }


        public Process SilentRun(string exe, string args)
        {
            System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo();
            start.FileName = exe;
            start.Arguments = args;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = false;
            Process p = Process.Start(start);
            p.WaitForExit(60000);
            Console.WriteLine(p.ExitCode);
            return p;
        }

        // DEPRECATED: 
        string MainModuleDir()
        {
            string main = Process.GetCurrentProcess().MainModule.FileName;
            return Path.GetDirectoryName(main);
        }

        string svnexe;

        public AutoBackupModel()
        {
            svnexe = @"C:\Program Files\TortoiseSVN\bin\svn.exe";
        }

        void Job(string workingPath)
        {
            System.Environment.CurrentDirectory = workingPath;
          
            List<String> added = new List<String>();
            List<String> deleted = new List<String>();

            System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo();
            start.FileName = svnexe;
            start.Arguments = "st ";
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;

            Process worker = Process.Start(start);
            worker.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                // Prepend line numbers to each line of the output. 
                if (!String.IsNullOrEmpty(e.Data))
                {
                    string[] items = Regex.Split(e.Data, "\\s+");
                    switch (items[0])
                    {
                        case "?":
                            added.Add(items[1]);
                            break;

                        case "!":
                            deleted.Add(items[1]);
                            break;
                    }
                }
            });

            worker.BeginOutputReadLine();
            worker.WaitForExit(60000);

            if (added.Count > 0)
            {
                String addedfilesargs = String.Join(" ", added.Select(s => "\"" + s + "\"").ToArray());
                SilentRun(svnexe, "add " + addedfilesargs);
            }

            if (deleted.Count > 0)
            {
                String deletedfilesargs = String.Join(" ", deleted.Select(s => "\"" + s + "\"").ToArray());
                SilentRun(svnexe, "del " + deletedfilesargs);
            }

            if (added.Count > 0 || deleted.Count > 0)
            {
                string msg = DateTime.Now.ToString();
                SilentRun(svnexe, String.Format("ci -m \"{0}\"", msg));
            }

            worker = null;            
        }

        public void Run()
        {
            Console.WriteLine(Process.GetCurrentProcess().MainModule.FileName);

            PerformanceCounter cpu = new PerformanceCounter( "Processor Information", "% Processor Time", "_Total" );
            PerformanceCounter diskidle = new PerformanceCounter( "LogicalDisk", "% Idle Time", "_Total");
            
            while (true)
            {
                
                using (ShellLibrary library = ShellLibrary.Load("Subversion", true))
                {
                    foreach (ShellFileSystemFolder folder in library)
                    {
                        string repopath = folder.Path;                        
                        //uint idletime = GetIdleTime();
                        while( true )
                        {
                            if (diskidle.NextValue() < 80.0 && cpu.NextValue() > 20)
                            {
                                Thread.Sleep(5000);
                                continue;
                            }                                                                                     
                            Console.WriteLine(String.Format("[#{0}] {1} Backup Start", DateTime.Now.ToLongTimeString(), repopath));
                            Stopwatch stopWatch = new Stopwatch();
                            stopWatch.Start();
                            Job(repopath);
                            stopWatch.Stop();
                            Console.WriteLine(String.Format("[#{0}] {1} Backup End ({2}ms)", DateTime.Now.ToLongTimeString(), repopath, stopWatch.ElapsedMilliseconds));
                            break;

                        }
                    }                    
                }

                Thread.Sleep(30 * 60 * 1000); // 30 min
            }
        }
    }
}
