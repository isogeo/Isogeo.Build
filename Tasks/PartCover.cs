using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Win32;

namespace Isogeo.Build.Tasks
{

    /// <summary>Executes the PartCover coverage analysis tool.</summary>
    /// <example>
    /// <code><![CDATA[
    /// <Target Name="Test">
    ///   <PartCover
    ///     LibFile="Test.tlb"
    ///     AdditionalOptions="/GD /CG"
    ///     ToolsVersion="6.0"
    ///   />
    /// </Target>
    /// ]]></code>
    /// </example>
    public class PartCover:
        ToolTask
    {

        protected override string GenerateFullPathToTool()
        {
            string versionValueName=null;
            for (int i=0; i<_PartCoverComponentRegValues.Length; ++i)
                if (_PartCoverComponentRegValues[i, 0]==ToolsVersion)
                {
                    versionValueName=_PartCoverComponentRegValues[i, 1];
                    break;
                }
            if (versionValueName==null)
                return ToolPath;

            string subKeyName=string.Format(
                CultureInfo.InvariantCulture,
                @"{0}{1}\",
                _PartCoverComponentRegKey,
                versionValueName
            );

            RegistryKey key=Registry.LocalMachine.OpenSubKey(subKeyName);
            if (key==null)
            {
                Log.LogError("PartCover {0} could not be found (registry key \"{1}\" is missing)", ToolsVersion, subKeyName);
                return null;
            }

            string rd=(string)key.GetValue(versionValueName);
            if (string.IsNullOrEmpty(rd))
            {
                Log.LogError("PartCover root directory could not be found");
                return null;
            }

            return Path.Combine(rd, ToolName);
        }

        protected override string GenerateCommandLineCommands()
        {
            CommandLineBuilder builder=new CommandLineBuilder();

            // Does not seem to work when last argument...
            builder.AppendSwitchIfNotNull("--output ", Output);

            builder.AppendSwitch("--register");

            builder.AppendSwitchIfNotNull("--target ", Target);
            builder.AppendSwitchIfNotNull("--target-work-dir ", TargetWorkingDir);

            // Does not work in MSBuild 3.5 because of quotes. Fixed in 4.0...
            //builder.AppendSwitchIfNotNull("--target-args", TargetArgs);
            if (!string.IsNullOrEmpty(TargetArgs))
                builder.AppendSwitch(
                    string.Concat(
                        "--target-args \"",
                        TargetArgs.Replace("\"", "\\\""),
                        "\""
                    )
                );

            if (Include!=null)
                foreach (ITaskItem ti in Include)
                    builder.AppendSwitchUnquotedIfNotNull("--include ", ti);
            if (Exclude!=null)
                foreach (ITaskItem ti in Exclude)
                    builder.AppendSwitchUnquotedIfNotNull("--exclude ", ti);

            return builder.ToString();
        }

        protected override string GetWorkingDirectory()
        {
            string ret=null;
            if (TargetWorkingDir!=null)
                ret=TargetWorkingDir.GetMetadata("FullPath");

            if (string.IsNullOrEmpty(ret))
                ret=base.GetWorkingDirectory();

            return ret;
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            if (_OutputRegex.IsMatch(singleLine))
            {
                base.LogEventsFromTextOutput(singleLine, MessageImportance.Low);
                _ToolStarted=true;
                return;
            }

            base.LogEventsFromTextOutput(singleLine, (_ToolStarted?MessageImportance.Normal:MessageImportance.Low));
        }

        public ITaskItem[] Include
        {
            get;
            set;
        }

        public ITaskItem[] Exclude
        {
            get;
            set;
        }

        public ITaskItem Output
        {
            get;
            set;
        }

        [Required]
        public ITaskItem Target
        {
            get;
            set;
        }

        public ITaskItem TargetWorkingDir
        {
            get;
            set;
        }

        public string TargetArgs
        {
            get;
            set;
        }

        [Required]
        public string ToolsVersion
        {
            get
            {
                return _ToolsVersion;
            }
            set
            {
                _ToolsVersion=value;
            }
        }

        protected override string ToolName
        {
            get
            {
                return "PartCover.exe";
            }
        }

        private string _ToolsVersion="4.0";
        private bool _ToolStarted;

        private static Regex _OutputRegex=new Regex(@"^(\[\d{5}\] \[\d{5}\] |\s*<)", RegexOptions.Compiled | RegexOptions.Multiline);
        private const string _PartCoverComponentRegKey=@"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\S-1-5-18\Components\";
        private static readonly string[,] _PartCoverComponentRegValues={
            //{ "4.0", "25E5950C0EA22EA4C8404BEF9D697C5B" },
            { "4.0", "82FDEC2C38A025247A99E66B5F26490D" },
            { "3.5", "22814841D06BA814A97DF67A13409D72" },
            { "3.0", "22814841D06BA814A97DF67A13409D72" },
            { "2.0", "22814841D06BA814A97DF67A13409D72" }
        };
    }
}
