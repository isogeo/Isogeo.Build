// $Id: SetEnvironmentFromBatch.cs 6042 2010-11-25 16:31:49Z mcartoixa $
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Isogeo.Build.Tasks
{

    /// <summary>Executes a Windows batch file and synchronizes the resulting
    /// environment variables with the current MSBuild process.</summary>
    /// <example>
    /// <code><![CDATA[
    /// <Target Name="Test">
    ///   <SetEnvironmentFromBatch
    ///     BatchFile="SetEnv.cmd"
    ///     EnableDelayedExpansion="True"
    ///     EnableExtensions="True"
    ///   />
    /// </Target>
    /// ]]></code>
    public class SetEnvironmentFromBatch:
        Task
    {

        public SetEnvironmentFromBatch()
        {
        }

        public override bool Execute()
        {
            bool ret=true;

            string toolPath=GenerateFullPathToTool();
            string commandLine=GenerateCommandLine();

            Log.LogCommandLine(string.Concat(toolPath, " ", commandLine));

            _ErrorData=new Queue();
            _OutputData=new Queue();

            using (_ExitedEvent=new ManualResetEvent(false))
                using (_ErrorDataAvailableEvent=new ManualResetEvent(false))
                    using (_OutputDataAvailableEvent=new ManualResetEvent(false))
                    {
                        Process process=new Process() {
                            StartInfo=GetProcessStartInfo(toolPath, commandLine),
                            EnableRaisingEvents=true
                        };
                        using (process)
                        {
                            process.Exited+=new EventHandler(
                                delegate(object o, EventArgs e) {
                                    ret=(process.ExitCode==0);
                                    _ExitedEvent.Set();
                                }
                            );
                            process.ErrorDataReceived+=new DataReceivedEventHandler(
                                delegate(object o, DataReceivedEventArgs e) {
                                    ReceiveData(e, _ErrorData, _ErrorDataAvailableEvent);
                                }
                            );
                            process.OutputDataReceived+=new DataReceivedEventHandler(
                                delegate(object o, DataReceivedEventArgs e) {
                                    ReceiveData(e, _OutputData, _OutputDataAvailableEvent);
                                }
                            );

                            process.Start();
                            process.StandardInput.Close();
                            process.BeginErrorReadLine();
                            process.BeginOutputReadLine();

                            HandleNotifications(process);
                        }
                    }

            return ret;
        }

        private ProcessStartInfo GetProcessStartInfo(string toolPath, string commandLine)
        {
            ProcessStartInfo ret=new ProcessStartInfo(toolPath, commandLine) {
                CreateNoWindow=true,
                UseShellExecute=false,
                RedirectStandardError=true,
                RedirectStandardOutput=true,
                StandardErrorEncoding=CurrentSystemOemEncoding,
                StandardOutputEncoding=CurrentSystemOemEncoding,
                RedirectStandardInput=true,
            };

            return ret;
        }

        private string GenerateCommandLine()
        {
            CommandLineBuilder batchBuilder=new CommandLineBuilder();
            batchBuilder.AppendFileNameIfNotNull(BatchFile);
            if (!string.IsNullOrEmpty(BatchArguments))
                batchBuilder.AppendSwitch(BatchArguments);
            batchBuilder.AppendSwitch("> nul && SET");

            CommandLineBuilder builder=new CommandLineBuilder();
            if (_EnableExtensions.HasValue)
                builder.AppendSwitch(string.Concat("/E:", (EnableExtensions ? "ON" : "OFF")));

            if (_EnableDelayedExpansion.HasValue)
                builder.AppendSwitch(string.Concat("/V:", (EnableDelayedExpansion ? "ON" : "OFF")));

            builder.AppendSwitch("/C");
            builder.AppendSwitch(batchBuilder.ToString());

            return builder.ToString();
        }

        private string GenerateFullPathToTool()
        {
            return _Comspec;
        }

        private void HandleNotifications(Process proc)
        {
            WaitHandle[] waitHandles=new WaitHandle[] { _ErrorDataAvailableEvent, _OutputDataAvailableEvent, _ExitedEvent };
            bool cont=true;
            while (cont)
            {
                switch (WaitHandle.WaitAny(waitHandles))
                {
                case 0:
                    {
                        LogErrorData(_ErrorData, _ErrorDataAvailableEvent);
                        HandleOutputData(_OutputData, _OutputDataAvailableEvent);
                        continue;
                    }
                case 1:
                    {
                        HandleOutputData(_OutputData, _OutputDataAvailableEvent);
                        continue;
                    }
                case 2:
                    {
                        proc.WaitForExit();
                        LogErrorData(_ErrorData, _ErrorDataAvailableEvent);
                        HandleOutputData(_OutputData, _OutputDataAvailableEvent);
                        cont=false;
                        continue;
                    }
                }
            }
        }

        private void HandleOutputData(Queue dataQueue, ManualResetEvent dataAvailableSignal)
        {
            lock (dataQueue.SyncRoot)
            {
                while (dataQueue.Count>0)
                {
                    string[] env=((string)dataQueue.Dequeue()).Split('=');

                    if (env.Length>1)
                    {
                        string name=env[0];
                        string value=string.Join("=", env, 1, env.Length-1);

                        if (string.Compare(Environment.GetEnvironmentVariable(name), value)!=0)
                        {
                            Log.LogMessageFromText(string.Format(CultureInfo.CurrentCulture, "SET {0}={1}", name, value), MessageImportance.Normal);
                            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
                            if (string.Compare(name, "WindowsSdkDir", StringComparison.OrdinalIgnoreCase)==0)
                                WindowsSdkDir=new TaskItem(value);
                        }
                    }
                }

                dataAvailableSignal.Reset();
            }
        }

        private void LogErrorData(Queue dataQueue, ManualResetEvent dataAvailableSignal)
        {
            lock (dataQueue.SyncRoot)
            {
                while (dataQueue.Count>0)
                    Log.LogError((string)dataQueue.Dequeue());

                dataAvailableSignal.Reset();
            }
        }

        private static void ReceiveData(DataReceivedEventArgs e, Queue dataQueue, ManualResetEvent dataAvailableSignal)
        {
            if (e.Data!=null)
                lock (dataQueue.SyncRoot)
                {
                    dataQueue.Enqueue(e.Data);
                    dataAvailableSignal.Set();
                }
        }

        [DllImport("kernel32.dll")]
        private static extern int GetOEMCP();

        [Required]
        public ITaskItem BatchFile
        {
            get;
            set;
        }

        [Output]
        public ITaskItem WindowsSdkDir
        {
            get;
            set;
        }

        public string BatchArguments
        {
            get;
            set;
        }

        public bool EnableExtensions
        {
            get
            {
                return _EnableExtensions.GetValueOrDefault(true);
            }
            set
            {
                _EnableExtensions=value;
            }
        }

        public bool EnableDelayedExpansion
        {
            get
            {
                return _EnableDelayedExpansion.GetValueOrDefault(true);
            }
            set
            {
                _EnableDelayedExpansion=value;
            }
        }

        internal static Encoding CurrentSystemOemEncoding
        {
            get
            {
                if (_CurrentOemEncoding==null)
                {
                    _CurrentOemEncoding=Encoding.Default;
                    try
                    {
                        _CurrentOemEncoding=Encoding.GetEncoding(GetOEMCP());
                    } catch (ArgumentException)
                    {
                    } catch (NotSupportedException)
                    {
                    }
                }
                return _CurrentOemEncoding;
            }
        }

        private bool? _EnableExtensions;
        private bool? _EnableDelayedExpansion;
        private Queue _ErrorData;
        private ManualResetEvent _ErrorDataAvailableEvent;
        private Queue _OutputData;
        private ManualResetEvent _OutputDataAvailableEvent;
        private ManualResetEvent _ExitedEvent;

        private static Encoding _CurrentOemEncoding;
        private static string _Comspec=Environment.GetEnvironmentVariable("COMSPEC");
    }
}
