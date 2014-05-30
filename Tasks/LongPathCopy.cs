using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Isogeo.Build.Tasks
{

    /// <summary>Copies files.</summary>
    public class LongPathCopy:
        Task
    {

        public override bool Execute()
        {
            bool ret=true;

            if ((SourceFiles==null) || (SourceFiles.Length==0))
            {
                DestinationFiles=new ITaskItem[0];
                CopiedFiles=new ITaskItem[0];
                return ret;
            }

            if (DestinationFiles==null)
            {
                DestinationFiles=new ITaskItem[SourceFiles.Length];
                for (int i=0; i<SourceFiles.Length; ++i)
                {
                    DestinationFiles[i]=new TaskItem(Path.Combine(DestinationFolder.ItemSpec, Path.GetFileName(SourceFiles[i].ItemSpec)));
                    SourceFiles[i].CopyMetadataTo(DestinationFiles[i]);
                }
            }

            for (int i=0; i<SourceFiles.Length;++i )
            {
                var ddir=Path.Combine(DestinationFiles[i].GetMetadata("RootDir"), DestinationFiles[i].GetMetadata("Directory"));
                if (!Exists(ddir) && !Create(ddir))
                {
                    ret|=false;
                    Log.LogErrorFromException(new Win32Exception(Marshal.GetLastWin32Error()));

                }

                Log.LogMessageFromText(
                    string.Format(
                        "Copying file from \"{0}\" to \"{1}\".",
                        SourceFiles[i].ItemSpec,
                        DestinationFiles[i].ItemSpec
                    ),
                    MessageImportance.Low
                );
                if (!Copy(SourceFiles[i].ItemSpec, DestinationFiles[i].ItemSpec))
                {
                    ret|=false;
                    Log.LogErrorFromException(new Win32Exception(Marshal.GetLastWin32Error()));
                }
            }

            CopiedFiles=new List<ITaskItem>(DestinationFiles).ToArray();
            return ret;
        }

        internal bool Create(string directory)
        {
            Log.LogMessageFromText(
                string.Format(
                    "Creating directory \"{0}\".",
                    directory
                ),
                MessageImportance.Normal
            );

            var dirs=directory.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            var pathBuilder=new StringBuilder();
            for (int i=0; i<dirs.Length; ++i )
            {
                pathBuilder.Append(dirs[i]);
                pathBuilder.Append(Path.DirectorySeparatorChar);

                var path=pathBuilder.ToString();
                if (!Exists(path) && !CreateDirectory(string.Concat(@"\\?\", path), IntPtr.Zero))
                    return false;
            }

            return true;
        }

        internal static bool Exists(string directory)
        {
            if (directory.Length>256)
                directory.Insert(0, @"\\?\");

            FileAttributes fa=GetFileAttributes(directory);
            if ((int)fa==-1)
                return false;

            return fa.HasFlag(FileAttributes.Directory);
        }

        internal bool Copy(string source, string destination)
        {
            var spath=source;
            if (spath.Length>256)
                spath.Insert(0, @"\\?\");

            var dpath=destination;
            if (dpath.Length>256)
                dpath.Insert(0, @"\\?\");

            return CopyFile(spath, dpath, false);
        }

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CopyFile(string lpExistingFileName, string lpNewFileName, bool bFailIfExists);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateDirectory(string lpPathName, IntPtr lpSecurityAttributes);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
        private static extern FileAttributes GetFileAttributes(string lpFileName);

        [Required]
        public ITaskItem[] SourceFiles
        {
            get;
            set;
        }

        public ITaskItem DestinationFolder
        {
            get;
            private set;
        }

        [Output]
        public ITaskItem[] CopiedFiles
        {
            get;
            private set;
        }

        [Output]
        public ITaskItem[] DestinationFiles
        {
            get;
            set;
        }
    }
}
