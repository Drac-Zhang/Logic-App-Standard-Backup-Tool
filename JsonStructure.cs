using System;
using System.Text;
using System.Collections.Generic;

namespace Company.Function
{
    public class BackupInfo
    {
        public string LogicAppName { get; set; }
        public string ConnectionString { get; set; }
    }

    public class ExceptionInfo
    {
        public string Message { get; private set; }
        public string StackTrace { get; private set; }
        public ExceptionInfo(Exception ex)
        {
            if (ex != null)
            {
                this.Message = ex.Message;
                this.StackTrace = ex.StackTrace;

                //TODO: might need to also include inner exception
            }
        }
    }

    public class BackupResult
    {
        public string LogicAppName { get; set; }
        public string Status { get; set; }
        public ExceptionInfo ErrorMessage { get; private set; }

        public BackupResult(string LogicAppName, string Status)
        {
            this.LogicAppName = LogicAppName;
            this.Status = Status;
        }

        public BackupResult(string LogicAppName, string Status, Exception ex)
        {
            this.LogicAppName = LogicAppName;
            this.Status = Status;
            this.ErrorMessage = new ExceptionInfo(ex);
        }
    }
}