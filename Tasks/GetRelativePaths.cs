using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Isogeo.Build.Tasks
{

    public class GetRelativePaths:
        Task
    {

        /// <summary>When overridden in a derived class, executes the task.</summary>
        /// <returns><c>true</c> if the task successfully executed; otherwise, <c>false</c>.</returns>
        public override bool Execute()
        {
            string basedir=Path.GetFullPath(BaseDirectory ?? Environment.CurrentDirectory);

            Outputs=Inputs.Select<ITaskItem, ITaskItem>(i => ConvertToRelative(i, basedir)).ToArray();

            return true;
        }

        private static ITaskItem ConvertToRelative(ITaskItem source, string basedir)
        {
            var builder=new StringBuilder(MAX_PATH);
            PathRelativePathTo(builder, basedir, FileAttributes.Directory, Path.GetFullPath(source.ItemSpec), FileAttributes.Normal);
            return new TaskItem(builder.ToString());
        }

        [DllImport("shlwapi.dll", CharSet=CharSet.Auto)]
        private static extern bool PathRelativePathTo(
             [Out] StringBuilder pszPath,
             [In] string pszFrom,
             [In] FileAttributes dwAttrFrom,
             [In] string pszTo,
             [In] FileAttributes dwAttrTo
        );

        [Required]
        public ITaskItem[] Inputs
        {
            get;
            set;
        }

        public string BaseDirectory
        {
            get;
            set;
        }

        [Output]
        public ITaskItem[] Outputs
        {
            get;
            set;
        }

        private const int MAX_PATH=260;
    }
}
