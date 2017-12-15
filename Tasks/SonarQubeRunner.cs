using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Isogeo.Build.Tasks
{
    public class SonarQubeRunner:
        ToolTask
    {

        public override bool Execute()
        {
            var home = Environment.GetEnvironmentVariable("SONAR_SCANNER_HOME");

            if (!string.IsNullOrEmpty(home))
            {
                // The scanner resets SONAR_SCANNER_HOME for some reason?
                EnvironmentVariables=new string[] {
                    "_SONAR_SCANNER_HOME=" + home
                };
            }

            return base.Execute();
        }

        protected override string GenerateCommandLineCommands()
        {
            var builder = new CommandLineBuilder();

            builder.AppendSwitchUnquotedIfNotNull("", Action.ToString().ToLowerInvariant());
            builder.AppendSwitchIfNotNull("/k:", ProjectKey);
            builder.AppendSwitchIfNotNull("/n:", ProjectName);
            builder.AppendSwitchIfNotNull("/v:", ProjectVersion);
            builder.AppendSwitchIfNotNull("/s:", Settings);
            builder.AppendSwitchIfNotNull("/d:sonar.branch.name=", BranchName);

            string options = Environment.GetEnvironmentVariable("SONAR_SCANNER_OPTS");
            if (!string.IsNullOrWhiteSpace(options))
            {
                //TODO: refine the parsing of options
                foreach(var opt in options.Split(' '))
                    builder.AppendSwitchUnquotedIfNotNull("", Regex.Replace(opt, "^-D", "/d:"));
            }

            return builder.ToString();
        }

        protected override string GenerateFullPathToTool()
        {
            string path = Environment.CurrentDirectory;

            if (!string.IsNullOrEmpty(ToolPath))
            {
                path=Path.GetFullPath(ToolPath);
                if (!Directory.Exists(ToolPath))
                    path=Path.GetDirectoryName(path);
            }

            return Path.Combine(path, ToolExe);
        }

        [Required]
        public string Action
        {
            get
            {
                return _Action.ToString().ToLowerInvariant();
            }
            set
            {
                _Action=(SonarQubeAction)Enum.Parse(typeof(SonarQubeAction), value.Replace("-", ""), true);
            }
        }

        public string ProjectKey { get; set; }

        public string ProjectName { get; set; }

        public string ProjectVersion { get; set; }

        /// <summary>The name of the branch to be analyzed.</summary>
        /// <seealso href="https://docs.sonarqube.org/display/PLUG/Branch+Plugin" />
        public string BranchName {
            get { return _BranchName; }
            set { _BranchName=(string.IsNullOrEmpty(value) ? null : value); }
        }

        public ITaskItem Settings { get; set; }

        protected override string ToolName { get { return "SonarQube.Scanner.MSBuild.exe"; } }

        private SonarQubeAction _Action;
        private string _BranchName;
    }

    public enum SonarQubeAction
    {
        Begin,
        End
    }
}
