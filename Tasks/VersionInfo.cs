using System;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Isogeo.Build.Tasks
{

    /// <summary>Gets the version information components from a string.</summary>
    /// <example>Returns the specified version major, minor, build and revision components
    /// <code><![CDATA[
    /// <Target Name="Test">
    ///   <VersionInfo Version="1.0.0.0">
    ///     <Output ItemName="VersionMajor" TaskParameter="Major" />
    ///     <Output ItemName="VersionMinor" TaskParameter="Minor" />
    ///     <Output ItemName="VersionBuild" TaskParameter="Build" />
    ///     <Output ItemName="VersionRevision" TaskParameter="Revision" />
    ///   </VersionInfo>
    /// </Target>
    /// ]]></code>
    /// </example>
    public class VersionInfo:
        Task
    {

        /// <summary>When overridden in a derived class, executes the task.</summary>
        /// <returns><c>true</c> if the task successfully executed; otherwise, <c>false</c>.</returns>
        public override bool Execute()
        {
            try
            {
                Version v=new Version(Version);
                Major=v.Major;
                Minor=v.Minor;
                Build=v.Build;
                Revision=v.Revision;

                return true;
            } catch (Exception ex)
            {
                Log.LogErrorFromException(ex, false);

                return false;
            }
        }

        [Required]
        public string Version
        {
            get
            {
                return _Version;
            }
            set
            {
                _Version=value;
            }
        }

        [Output]
        public int Major
        {
            get
            {
                return _Major;
            }
            set
            {
                _Major=value;
            }
        }

        [Output]
        public int Minor
        {
            get
            {
                return _Minor;
            }
            set
            {
                _Minor=value;
            }
        }

        [Output]
        public int Build
        {
            get
            {
                return _Build;
            }
            set
            {
                _Build=value;
            }
        }

        [Output]
        public int Revision
        {
            get
            {
                return _Revision;
            }
            set
            {
                _Revision=value;
            }
        }

        private int _Major;
        private int _Minor;
        private int _Build;
        private int _Revision;

        private string _Version;
    }
}
