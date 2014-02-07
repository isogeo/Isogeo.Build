// $Id: FileInfo.cs 6375 2011-02-11 16:11:39Z mcartoixa $
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Xml;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Isogeo.Build.Tasks
{

    /// <summary>Gets the information about a specific file.</summary>
    /// <example>Outputs "MyMessage" to the "ListenerFile" CCNet listener file.
    /// <code><![CDATA[
    /// <Target Name="Test">
    ///   <FileInfo Path="Test.txt">
    ///     <Output TaskParameter="Length" ItemName="TestFileLength" />
    ///   </FileInfo>
    /// </Target>
    /// ]]></code>
    /// </example>
    public class FileInfo:
        Task
    {

        public override bool Execute()
        {
            var fi=new System.IO.FileInfo(Path.ItemSpec);
            Exists=fi.Exists;
            IsReadOnly=fi.IsReadOnly;
            Length=fi.Length;
            Name=fi.Name;
            FullName=fi.FullName;
            Version = fi.LastWriteTimeUtc.ToBinary().ToString( "x", CultureInfo.InvariantCulture );
            return true;
        }

        [Required]
        public ITaskItem Path
        {
            get;
            set;
        }

        [Output]
        public bool Exists
        {
            get;
            set;
        }

        [Output]
        public bool IsReadOnly
        {
            get;
            set;
        }

        [Output]
        public long Length
        {
            get;
            set;
        }

        [Output]
        public string Name
        {
            get;
            set;
        }

        [Output]
        public string FullName
        {
            get;
            set;
        }

        [Output]
        public string Version
        {
            get;
            set;
        }
    }
}
