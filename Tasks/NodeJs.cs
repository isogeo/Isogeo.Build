using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Win32;

namespace Isogeo.Build.Tasks
{

    public class NodeJs:
        ToolTask
    {

        public NodeJs()
        {
            LogStandardErrorAsError=true;
            FailOnError=true;
        }

        protected override string GenerateCommandLineCommands()
        {
            var builder=new CommandLineBuilder();

            builder.AppendTextUnquoted(Arguments);

            return builder.ToString();
        }

        protected override string GenerateFullPathToTool()
        {
            string path=null;

            if (!string.IsNullOrEmpty(ToolPath))
                path=Path.GetDirectoryName(Path.GetFullPath(ToolPath));

            if (path==null)
            {
                var key=Registry.LocalMachine.OpenSubKey(_NodeInstallKey);
                if ((key==null) && (IntPtr.Size==8))
                    key=Registry.LocalMachine.OpenSubKey(_NodeInstallKeyWow6432);
                if (key==null)
                    key=Registry.CurrentUser.OpenSubKey(_NodeInstallKey);
                if ((key==null) && (IntPtr.Size==8))
                    key=Registry.CurrentUser.OpenSubKey(_NodeInstallKeyWow6432);

                if (key!=null)
                {
                    Log.LogMessage(MessageImportance.Low, "Node.js registry key found in \"{0}\"", key.Name);
                    object p=key.GetValue("InstallPath");
                    if (p!=null)
                    {
                        path=Path.GetDirectoryName(Path.GetFullPath(p.ToString()));
                        Log.LogMessage(MessageImportance.Low, "Node.js path found in registry: \"{0}\"", key.Name);
                    }
                }
            }

            if (path==null)
            {
                string programFiles=Environment.GetEnvironmentVariable("ProgramFiles");

                string p=Path.Combine(programFiles, "nodejs", ToolExe);
                if (File.Exists(p))
                    path=Path.GetDirectoryName(p);
            }

            if (path==null)
            {
                string programFiles=Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                if (programFiles!=null)
                {
                    string p=Path.Combine(programFiles, "nodejs", ToolExe);
                    if (File.Exists(p))
                        path=Path.GetDirectoryName(p);
                }
            }

            if (path==null)
            {
                string wd=GetWorkingDirectory();
                if (!string.IsNullOrEmpty(wd))
                    path=Path.GetDirectoryName(Path.GetFullPath(wd));
                else
                    path=Environment.CurrentDirectory;
            }

            return Path.Combine(path, ToolExe);
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            singleLine=_InvalidCharacters.Replace(singleLine, "");

            Match wm=_Warnings.Match(singleLine);
            if (wm.Success)
            {
                Log.LogWarning("node", wm.Groups["CODE"].Value, "", "", 0, 0, 0, 0, wm.Groups["MESSAGE"].Value);
                return;
            }

            base.LogEventsFromTextOutput(singleLine, MessageImportance.Normal);
        }

        protected override bool HandleTaskExecutionErrors()
        {
            if (!FailOnError)
            {
                Log.LogMessageFromText(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        SR.NodeJsToolFailed,
                        ToolName,
                        ExitCode
                    ),
                    MessageImportance.Normal
                );
                return true;
            }

            return base.HandleTaskExecutionErrors();
        }

        protected override string GetWorkingDirectory()
        {
            if (WorkingDirectory!=null)
                return WorkingDirectory.ItemSpec;

            return base.GetWorkingDirectory();
        }

        internal static string GetShortPathName(string path)
        {
            if (path.Length>256)
                path.Insert(0, @"\\?\");

            var spath=new StringBuilder(255);
            GetShortPathName(path, spath, 255);
            return spath.ToString();
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetShortPathName(string path, StringBuilder shortPath, int shortPathLength);

        public string Arguments
        {
            get;
            set;
        }

        public bool FailOnError { get; set; }

        public ITaskItem WorkingDirectory
        {
            get;
            set;
        }

        public string[] PathAdditions
        {
            get;
            set;
        }

        protected override string ToolName
        {
            get
            {
                return "node.exe";
            }
        }

#pragma warning disable 618
        [Obsolete]
        protected override StringDictionary EnvironmentOverride
        {
            get
            {
                var ret=new StringDictionary();
                string path=GetShortPathName(Path.GetDirectoryName(GenerateFullPathToTool()));

                if ((PathAdditions!=null) && (PathAdditions.Length>0))
                {
                    var additions=new List<string>(PathAdditions.Select(p => GetShortPathName(p)));
                    additions.Add(path);
                    additions.Add(Environment.GetEnvironmentVariable("PATH"));
                    ret.Add(
                        "PATH",
                        string.Join(";", additions)
                    );
                    return ret;
                } else
                    ret.Add(
                        "PATH",
                        string.Join(";", path, Environment.GetEnvironmentVariable("PATH"))
                    );


                return ret;
            }
        }
#pragma warning restore 618

        private static Regex _InvalidCharacters=new Regex(@"(\e\[\d+m|\a)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
        private static Regex _Warnings=new Regex(@"<WARN>\s*((?<CODE>\w+)\s*,\s*)?(?<MESSAGE>.+)\s*</WARN>", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        private const string _NodeInstallKey=@"SOFTWARE\Node.js";
        private const string _NodeInstallKeyWow6432=@"SOFTWARE\Wow6432Node\Node.js";
    }
}
