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

        protected override string GenerateCommandLineCommands()
        {
            var builder = new CommandLineBuilder();

            builder.AppendSwitchUnquotedIfNotNull("", Action.ToString().ToLowerInvariant());
            builder.AppendSwitchIfNotNull("/k:", ProjectKey);
            builder.AppendSwitchIfNotNull("/n:", ProjectName);
            builder.AppendSwitchIfNotNull("/v:", ProjectVersion);
            builder.AppendSwitchIfNotNull("/s:", Settings);

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
            string path = null;

            if (!string.IsNullOrEmpty(ToolPath))
                path=Path.GetDirectoryName(Path.GetFullPath(ToolPath));

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

        public ITaskItem Settings { get; set; }

        protected override string ToolName {  get { return "MSBuild.SonarQube.Runner.exe"; } }

        private SonarQubeAction _Action;
    }

    public enum SonarQubeAction
    {
        Begin,
        End
    }
}
