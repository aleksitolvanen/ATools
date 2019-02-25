using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace ATools
{
    public class MultithreadFileLogger : IMultithreadFileLogger
    {
        public string DateTimeFormat = "dd.MM.yyyy HH:mm:ss";

        public int BlockingCollectionItemCount { get { return bc.Count; } }

        private BlockingCollection<LogItem> bc = new BlockingCollection<LogItem>();

        private string FilePath { get; set; }

        private bool WriteTimeStamp { get; set; }

        private const string TraceMsg = "{0}Trace - {1}";

        private const string InfoMsg = "{0}Info - {1}";

        private const string WarningMsg = "{0}* Warning * - {1} (Action {2} on {3})";

        private const string ErrorMsg = "{0}*** Error *** - {1} (Action {2} on {3})";

        private const string SimpleMsg = "{0}{1}";

        public MultithreadFileLogger(string path, bool writeTimeStamp = true, bool appendFile = true)
        {
            FilePath = path;
            WriteTimeStamp = writeTimeStamp;

            if (!appendFile)
            {
                File.Delete(FilePath);
            }

            Task.Factory.StartNew(() =>
            {
                foreach (LogItem logItem in bc.GetConsumingEnumerable())
                {
                    switch (logItem.LogType)
                    {
                        case LogItem.LogTypeEnum.Trace:
                            Write(string.Format(TraceMsg, PrefixTimeStamp(), logItem.Msg));
                            break;
                        case LogItem.LogTypeEnum.Info:
                            Write(string.Format(InfoMsg, PrefixTimeStamp(), logItem.Msg));
                            break;
                        case LogItem.LogTypeEnum.Warning:
                            Write(string.Format(WarningMsg, PrefixTimeStamp(), logItem.Msg, logItem.Action, logItem.Obj));
                            break;
                        case LogItem.LogTypeEnum.Error:
                            Write(string.Format(ErrorMsg, PrefixTimeStamp(), logItem.Msg, logItem.Action, logItem.Obj));
                            break;
                        case LogItem.LogTypeEnum.Simple:
                            Write(string.Format(SimpleMsg, PrefixTimeStamp(), logItem.Msg));
                            break;
                        default:
                            Write(string.Format(SimpleMsg, PrefixTimeStamp(), logItem.Msg));
                            break;
                    }
                }
            });
        }

        private void Write(string msg, bool appendFile = true)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(FilePath, appendFile))
            {
                file.WriteLine(msg);
            }
        }

        ~MultithreadFileLogger()
        {
            bc.CompleteAdding();
        }

        public void WriteLine(string msg)
        {
            LogItem p = new LogItem(LogItem.LogTypeEnum.Simple, msg);
            bc.Add(p);
        }

        public void WriteTrace(string errorMsg)
        {
            LogItem p = new LogItem(LogItem.LogTypeEnum.Trace, errorMsg);
            bc.Add(p);
        }

        public void WriteInfo(string msg)
        {
            LogItem p = new LogItem(LogItem.LogTypeEnum.Info, msg);
            bc.Add(p);
        }

        public void WriteWarning(string errorMsg, string errorAction, string errorObject)
        {
            LogItem p = new LogItem(LogItem.LogTypeEnum.Warning, errorMsg, errorAction, errorObject);
            bc.Add(p);
        }

        public void WriteError(string errorMsg, string errorAction, string errorObject)
        {
            LogItem p = new LogItem(LogItem.LogTypeEnum.Error, errorMsg, errorAction, errorObject);
            bc.Add(p);
        }

        private string PrefixTimeStamp()
        {
            if (!WriteTimeStamp)
            {
                return string.Empty;
            }
            DateTime now = DateTime.Now;
            return $"[{now.ToString(DateTimeFormat)}] - ";
        }
    }

    public interface IMultithreadFileLogger
    {
        void WriteLine(string msg);

        void WriteTrace(string msg);
        void WriteInfo(string msg);
        void WriteWarning(string errorObject, string errorAction, string errorMsg);
        void WriteError(string errorObject, string errorAction, string errorMsg);
    }

    internal class LogItem
    {
        internal enum LogTypeEnum { Trace, Info, Warning, Error, Simple };

        internal LogTypeEnum LogType { get; set; }
        internal string Msg { get; set; }
        internal string Action { get; set; }
        internal string Obj { get; set; }

        internal LogItem()
        {
            LogType = LogTypeEnum.Info;
            Msg = string.Empty;
        }
        internal LogItem(LogTypeEnum logType, string logMsg)
        {
            LogType = logType;
            Msg = logMsg;
        }
        internal LogItem(LogTypeEnum logType, string logMsg, string logAction, string logObj)
        {
            LogType = logType;
            Msg = logMsg;
            Action = logAction;
            Obj = logObj;
        }
    }
}
