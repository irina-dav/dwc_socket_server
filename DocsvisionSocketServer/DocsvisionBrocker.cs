using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace DocsvisionSocketServer
{
    public class DocsvisionBrocker
    {
        private static readonly Properties.Settings settings = Properties.Settings.Default;


        public static byte[] InvokeMethod(string strJsonMessage)
        {
            try
            {
                JObject jsonObj = JObject.Parse(strJsonMessage);
                string methodName = (string)jsonObj["methodName"];
                MethodInfo method = typeof(DocsvisionBrocker).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
                object[] args = method.GetParameters().Select(p => Convert.ChangeType(jsonObj[p.Name], p.ParameterType)).ToArray();
                return (byte[])method.Invoke(null, args);               
            }
            catch (Exception ex)
            {
                LogManager.WriteException(ex);
                return new byte[0];
            }

        }


        private static byte[] GetFileData(string fileId)
        {
            FileObject file = new FileObject(fileId);
            return file.AsByteArray();
        }


        private static byte[] GetListActiveTask_ApprovingContract(string account)
        {
            TaskList activeTaskList = new TaskList(SavedSearch.ApprovingContracts, account);
            return Encoding.UTF8.GetBytes(activeTaskList.ToJSON().ToString());
        }


        private static byte[] GetListActiveTask_AcquaintanceDocument(string account)
        {           
            TaskList activeTaskList = new TaskList(SavedSearch.AcquaintanceDocuments, account);
            return Encoding.UTF8.GetBytes(activeTaskList.ToJSON().ToString());
        }


        private static byte[] GetListActiveTask_ApprovingDocument(string account)
        {
            TaskList activeTaskList = new TaskList(SavedSearch.ApprovingDocuments, account);
            return Encoding.UTF8.GetBytes(activeTaskList.ToJSON().ToString());
        }


        private static byte[] GetListFinishedTask_ApprovingContract(string account, DateTime date1, DateTime date2)
        {
            TaskList finishedTaskList = new TaskList(SavedSearch.FinApprovingContracts, account, date1, date2);
            return Encoding.UTF8.GetBytes(finishedTaskList.ToJSON().ToString());
        }


        private static byte[] GetListFinishedTask_ApprovingDocument(string account, DateTime date1, DateTime date2)
        {
            TaskList finishedTaskList = new TaskList(SavedSearch.FinApprovingDocuments, account, date1, date2);
            return Encoding.UTF8.GetBytes(finishedTaskList.ToJSON().ToString());
        }


        private static byte[] GetListFinishedTask_AcquaintanceDocument(string account, DateTime date1, DateTime date2)
        {
            TaskList finishedTaskList = new TaskList(SavedSearch.FinAcquaintanceDocuments, account, date1, date2);
            return Encoding.UTF8.GetBytes(finishedTaskList.ToJSON().ToString());
        }
   

        private static byte[] GetTaskInfo(string taskId)
        {
            Task dvTask = new Task(taskId);           
            return Encoding.UTF8.GetBytes(dvTask.ToJSON().ToString());
        }
        
        private static byte[] GetCountTasks(string account)
        {
            JObject jObject = new JObject { };

            List<SavedSearch> searchesActiveTasks = new List<SavedSearch>()
            {
                SavedSearch.AcquaintanceDocuments,
                SavedSearch.ApprovingContracts,
                SavedSearch.ApprovingDocuments,
            };
            foreach (var seacrh in searchesActiveTasks)
            {
                TaskList activeTaskList = new TaskList(seacrh, account);
                jObject.Add(seacrh.Name, activeTaskList.Count);
            }

            return Encoding.UTF8.GetBytes(jObject.ToString());
        }


        private static byte[] EndTask(string taskId, string result, string comment, string account)
        {
            Task dvTask = new Task(taskId);
            dvTask.EndTask(account, result, comment);
            return Encoding.UTF8.GetBytes("success");
        }         
    }
}
