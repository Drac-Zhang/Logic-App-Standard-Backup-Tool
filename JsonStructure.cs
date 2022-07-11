using System;
using System.Text;

namespace Company.Function
{
    public class BackupInfo
    {
        public string LogicAppName { get; set; }
        public string ConnectionString { get; set; }
    }

    public class ExceptionInfo
    {
        public string Message {get; set;}
        public string StackTrace {get; set;}
    }
}