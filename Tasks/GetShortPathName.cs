using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Isogeo.Build.Tasks
{

    public class GetShortPathName:
        Task
    {

        /// <summary>When overridden in a derived class, executes the task.</summary>
        /// <returns><c>true</c> if the task successfully executed; otherwise, <c>false</c>.</returns>
        public override bool Execute()
        {
            Output=new TaskItem(Execute(Path.GetFullPath(Input.ItemSpec)));

            return true;
        }

        internal static string Execute(string path)
        {
            if (path.Length>256)
                path.Insert(0, @"\\?\");

            var spath = new StringBuilder(255);
            NativeMethod(path, spath, 255);
            return spath.ToString();
        }

        [DllImport("kernel32.dll", EntryPoint="GetShortPathName", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int NativeMethod(string path, StringBuilder shortPath, int shortPathLength);

        [Required]
        public ITaskItem Input
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
        public ITaskItem Output
        {
            get;
            set;
        }
    }
}
