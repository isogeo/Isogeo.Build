using System;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Win32 = Microsoft.Win32;

namespace Isogeo.Build.Tasks
{

    public class NodePackageManager:
        NodeJs
    {

        public NodePackageManager()
        {
            LogStandardErrorAsError=false;
        }

        protected override string GenerateCommandLineCommands()
        {
            var builder=new CommandLineBuilder();

            builder.AppendFileNameIfNotNull(Path.Combine(Path.GetDirectoryName(GenerateFullPathToTool()), @"node_modules\npm\bin\npm-cli.js"));

            builder.AppendSwitchUnquotedIfNotNull("", Action.ToString().ToLowerInvariant());
            switch (_Only)
            {
            case NpmOnly.Development:
                builder.AppendSwitchIfNotNull("--only=", "development");
                break;
            case NpmOnly.Production:
                builder.AppendSwitchIfNotNull("--only=", "production");
                break;
            }
            builder.AppendSwitchIfNotNull("", "--no-bin-links");
            builder.AppendSwitchIfNotNull("", "--no-color");
            builder.AppendSwitchIfNotNull("", "--no-progress");
            builder.AppendSwitchIfNotNull("--registry ", Registry);
            builder.AppendSwitchIfNotNull("--cache ", Cache.ItemSpec);

            string gypMsvsVersion=Environment.GetEnvironmentVariable("GYP_MSVS_VERSION");
            if (!string.IsNullOrWhiteSpace(gypMsvsVersion))
                builder.AppendSwitchIfNotNull("--msvs_version=", gypMsvsVersion);

            string args=base.GenerateCommandLineCommands();
            if (!string.IsNullOrEmpty(args))
                builder.AppendSwitchUnquotedIfNotNull("", args);

            return builder.ToString();
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            Match em=_Errors.Match(singleLine);
            if (em.Success)
            {
                string m=em.Groups["MESSAGE"].Value;
                if (!string.IsNullOrEmpty(m))
                {
                    Log.LogError("npm", "", "", "", 0, 0, 0, 0, m);
                    return;
                }
            }

            Match wm=_Warnings.Match(singleLine);
            if (wm.Success)
            {
                string m=wm.Groups["MESSAGE"].Value;
                if (!string.IsNullOrEmpty(m))
                {
                    Log.LogWarning("npm", "", "", "", 0, 0, 0, 0, m);
                    return;
                }
            }

            base.LogEventsFromTextOutput(singleLine, messageImportance);
        }

        [Required]
        public string Action
        {
            get
            {
                return Regex.Replace(_Action.ToString(), "(?!^)[A-Z]", m => string.Concat("-", m.Value)).ToLowerInvariant();
            }
            set
            {
                _Action=(NpmAction)Enum.Parse(typeof(NpmAction), value.Replace("-", ""), true);
            }
        }

        public string Registry
        {
            get;
            set;
        }

        public string Only
        {
            get
            {
                return _Only.ToString();
            }
            set
            {
                _Only=(NpmOnly)Enum.Parse(typeof(NpmOnly), value, true);
            }
        }

        public ITaskItem Cache
        {
            get;
            set;
        }

#pragma warning disable 618
        [Obsolete]
        protected override StringDictionary EnvironmentOverride
        {
            get
            {
                var key=Win32.Registry.LocalMachine.OpenSubKey(_GitInstallKey);
                if ((key==null) && (IntPtr.Size==8))
                    key=Win32.Registry.LocalMachine.OpenSubKey(_GitInstallKeyWow6432);

                if (key!=null)
                {
                    object p=key.GetValue("InstallLocation");
                    if (p!=null)
                    {
                        var ret=base.EnvironmentOverride;
                        if (ret==null)
                            ret=new StringDictionary();

                        string path=Environment.GetEnvironmentVariable("PATH");
                        if (ret.ContainsKey("PATH"))
                        {
                            path=ret["PATH"];
                            ret.Remove("PATH");
                        }
                        ret.Add(
                            "PATH",
                            string.Join(";", path, Path.Combine(Path.GetDirectoryName(Path.GetFullPath(p.ToString())), "bin"))
                        );
                        return ret;
                    }
                }

                return base.EnvironmentOverride;
            }
        }
#pragma warning restore 618

        private NpmAction _Action;
        private NpmOnly _Only;

        private static Regex _Errors=new Regex(@"^(npm\s+)?ERR!\s+((?<MESSAGE>.+))?$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
        private static Regex _Warnings=new Regex(@"^(npm\s+)?WARN\s+((?<MESSAGE>.+))?$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        private const string _GitInstallKey=@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1";
        private const string _GitInstallKeyWow6432=@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1";
    }

    public enum NpmAction
    {
        Install,
        Dedupe,
        RunScript,
        Update
    }

    public enum NpmOnly
    {
        All=0,
        Development,
        Production
    }
}
