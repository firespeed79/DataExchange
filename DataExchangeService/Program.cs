using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;

namespace DataExchangeService
{
    static class Program
    {
        private static Process RuningInstance(bool sameNameInstance = false)
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] Processes = Process.GetProcessesByName(currentProcess.ProcessName);
            foreach (Process process in Processes)
            {
                if (process.Id != currentProcess.Id)
                {
                    if (sameNameInstance)
                    {
                        string current = new FileInfo(currentProcess.MainModule.FileName).Name.ToLower();
                        string This = new FileInfo(process.MainModule.FileName).Name.ToLower();
                        if (This == current)
                        {
                            return process;
                        }
                    }
                    else
                    {
                        if (process.MainModule.FileName.Equals(currentProcess.MainModule.FileName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return process;
                        }
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main()
        {
            Process process = RuningInstance();
            if (process == null)
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new Service1()
                };
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                using (EventLog eventLog = new EventLog("Application", Environment.MachineName, AppDomain.CurrentDomain.FriendlyName))
                {
                    eventLog.WriteEntry("系统中已经运行了[" + process.MainModule.FileName + "]程序！当前只允许一个实例在运行。", EventLogEntryType.Warning);
                }
            }
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string str = string.Format("应用程序错误，请及时联系系统管理员:{0},应用程序状态：{1}", e.ExceptionObject.ToString(), (e.IsTerminating ? "终止" : "未终止"));

            StackTrace st = new StackTrace(true);
            StackFrame sf = st.GetFrame(0);
            string fileName = sf.GetFileName();
            Type type = sf.GetMethod().ReflectedType;
            string assName = type.Assembly.FullName;
            string typeName = type.FullName;
            string methodName = sf.GetMethod().Name;
            int lineNo = sf.GetFileLineNumber();
            int colNo = sf.GetFileColumnNumber();
            Logs.Create(str, fileName + " : " + assName + "." + typeName + "=>" + methodName + "(" + lineNo + "行" + colNo + "列)");
        }
    }
}
