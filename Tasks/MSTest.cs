using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Win32;

namespace Isogeo.Build.Tasks
{
    public class MSTest:
        ToolTask
    {

        public MSTest()
        {
            ToolsVersion="10.0";
        }

        protected override string GenerateCommandLineCommands()
        {
            var builder=new CommandLineBuilder();

            builder.AppendSwitch("/nologo");
            builder.AppendSwitch("/usestderr");

            builder.AppendSwitchIfNotNull("/category:", Category);
            builder.AppendSwitchIfNotNull("/testsettings:", Settings);
            builder.AppendSwitchIfNotNull("/searchpathroot:", SearchPathRoot);
            builder.AppendSwitchIfNotNull("/resultsfileroot:", ResultsFileRoot);
            if (NoResults)
                builder.AppendSwitch("/noresults");

            if (Containers!=null)
                foreach (ITaskItem i in Containers)
                    builder.AppendSwitchIfNotNull("/testcontainer:", i);

            builder.AppendSwitchIfNotNull("/publish:", TeamCollectionUri);
            builder.AppendSwitchIfNotNull("/publishbuild:", TeamBuildUri);
            builder.AppendSwitchIfNotNull("/teamproject:", TeamProject);
            builder.AppendSwitchIfNotNull("/platform:", Platform);
            builder.AppendSwitchIfNotNull("/flavor:", Flavor);

            return builder.ToString();
        }

        protected override bool ValidateParameters()
        {
            if (!string.IsNullOrEmpty(Flavor) || !string.IsNullOrEmpty(Platform) || !string.IsNullOrEmpty(TeamBuildUri) || !string.IsNullOrEmpty(TeamProject))
            {
                bool ret=!string.IsNullOrEmpty(TeamCollectionUri);
                if (!ret)
                    Log.LogError(SR.NoTeamCollectionSpecifiedValidationError);
                return ret;
            }

            return base.ValidateParameters();
        }

        protected override string GenerateFullPathToTool()
        {
            var key=Registry.LocalMachine.OpenSubKey(
                string.Format(
                    CultureInfo.InvariantCulture,
                    IntPtr.Size==8 ? _VSInstallKeyWow6432  : _VSInstallKey,
                    ToolsVersion
                )
            );
            if (key!=null)
                return Path.Combine(key.GetValue("InstallDir").ToString(), ToolName);

            return null;
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            Match m=_ResultsRegex.Match(singleLine);
            if (m.Success)
                switch (m.Groups["RESULT"].Value)
                {
                case "Error":
                case "Failed":
                case "Inconclusive":
                    Log.LogError(string.Format(CultureInfo.CurrentCulture, "{0} test: {1}", m.Groups["RESULT"].Value, m.Groups["TESTNAME"].Value));
                    return;
                default:
                    Log.LogMessageFromText(singleLine, MessageImportance.Low);
                    return;
                }

            Log.LogMessageFromText(singleLine, messageImportance);
        }

        public string Category
        {
            get;
            set;
        }

        public ITaskItem[] Containers
        {
            get;
            set;
        }

        public string Flavor
        {
            get;
            set;
        }

        public bool NoResults
        {
            get;
            set;
        }

        public string Platform
        {
            get;
            set;
        }

        public ITaskItem Settings
        {
            get;
            set;
        }

        public ITaskItem ResultsFileRoot
        {
            get;
            set;
        }

        public ITaskItem SearchPathRoot
        {
            get;
            set;
        }

        public string TeamCollectionUri
        {
            get;
            set;
        }

        public string TeamBuildUri
        {
            get;
            set;
        }

        public string TeamProject
        {
            get;
            set;
        }

        public string ToolsVersion
        {
            get;
            set;
        }

        /// <summary>Gets the name of the executable file to run.</summary>
        /// <returns>The name of the executable file to run.</returns>
        protected override string ToolName
        {
            get
            {
                return "MSTest.exe";
            }
        }

        private const string _VSInstallKey=@"SOFTWARE\Microsoft\VisualStudio\{0}";
        private const string _VSInstallKeyWow6432=@"SOFTWARE\Wow6432Node\Microsoft\VisualStudio\{0}";
        private static readonly Regex _ResultsRegex=new Regex(@"^(?<RESULT>(Error|Failed|Inconclusive|Passed))\s+(?<TESTNAME>.*)$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    }
}
