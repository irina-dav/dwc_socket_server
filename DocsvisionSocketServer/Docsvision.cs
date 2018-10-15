using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using DocsVision.Platform.ObjectManager;
using DocsVision.Platform.ObjectManager.Metadata;
using DocsVision.Platform.ObjectManager.SearchModel;
using DocsVision.Platform.ObjectManager.SystemCards;
using CardDefs = DocsVision.BackOffice.CardLib.CardDefs;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Reflection;

namespace DocsvisionSocketServer
{
    public class Docsvision
    {
        private static Properties.Settings settings = Properties.Settings.Default;

        private static Dictionary<string, string> savedSearches_ActiveTasks = new Dictionary<string, string>()
        {
             { "ApprovingContracts",        settings.Search_ApprovingContracts},              
             { "ApprovingDocuments",        settings.Search_ApprovingDocuments}, 
             { "AcquaintanceDocuments",     settings.Search_AcquaintanceDocuments},
        };
        private static Dictionary<string, string> savedSearches_FinishedTasks = new Dictionary<string, string>()
        {
             { "FinApprovingContracts",     settings.Search_FinApprovingContracts },
             { "FinApprovingDocuments",     settings.Search_FinApprovingDocuments},
             { "FinAcquaintanceDocuments",  settings.Search_FinAcquaintanceDocuments},
        };

        private static string SEARCH_CARD_TYPE = "{05E4BE46-6304-42A7-A780-FD07F7541AF0}";

        public static UserSession Session => DocsvisionSessionManager.Session;

        public static byte[] InvokeMethod(string strJsonMessage)
        {
            try
            {
                JObject jsonObj = JObject.Parse(strJsonMessage);
                string methodName = (string)jsonObj["methodName"];
                MethodInfo method = typeof(Docsvision).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
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
            FileData fileData = Session.FileManager.GetFile(new Guid(fileId));
            using (Stream input = fileData.OpenReadStream())
            {
                using (var memoryStream = new MemoryStream())
                {
                    input.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }            
        }

        private static byte[] GetListActiveTask_ApprovingContract(string account)
        {
            CardDataCollection cdTasks = SearchActiveTasks(savedSearches_ActiveTasks["ApprovingContracts"], account);
            return Encoding.UTF8.GetBytes(GetJson_ListTaskInfo(cdTasks).ToString());
        }

        private static byte[] GetListActiveTask_AcquaintanceDocument(string account)
        {
            CardDataCollection cdTasks = SearchActiveTasks(savedSearches_ActiveTasks["AcquaintanceDocuments"], account);
            return Encoding.UTF8.GetBytes(GetJson_ListTaskInfo(cdTasks).ToString());
        }

        private static byte[] GetListActiveTask_ApprovingDocument(string account)
        {
            CardDataCollection cdTasks = SearchActiveTasks(savedSearches_ActiveTasks["ApprovingDocuments"], account);
            return Encoding.UTF8.GetBytes(GetJson_ListTaskInfo(cdTasks).ToString());
        }

        private static byte[] GetListFinishedTask_ApprovingContract(string account, DateTime date1, DateTime date2)
        {
            CardDataCollection cdTasks = SearchFinishedTasks(savedSearches_FinishedTasks["FinApprovingContracts"], account, EndOfDate(date1), EndOfDate(date2));
            return Encoding.UTF8.GetBytes(GetJson_ListTaskInfo(cdTasks).ToString());
        }

        private static byte[] GetListFinishedTask_ApprovingDocument(string account, DateTime date1, DateTime date2)
        {
            CardDataCollection cdTasks = SearchFinishedTasks(savedSearches_FinishedTasks["FinApprovingDocuments"], account, EndOfDate(date1), EndOfDate(date2));
            return Encoding.UTF8.GetBytes(GetJson_ListTaskInfo(cdTasks).ToString());
        }

        private static byte[] GetListFinishedTask_AcquaintanceDocument(string account, DateTime date1, DateTime date2)
        {
            CardDataCollection cdTasks = SearchFinishedTasks(savedSearches_FinishedTasks["FinAcquaintanceDocuments"], account, EndOfDate(date1), EndOfDate(date2));
            return Encoding.UTF8.GetBytes(GetJson_ListTaskInfo(cdTasks).ToString());
        }
   
        private static byte[] GetTaskInfo(string taskId)
        {
            CardData cardData = Session.CardManager.GetCardData(new Guid(taskId));
            RowData rdMainInfo = cardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow;            

            DocsvisionTask dvTask = new DocsvisionTask(cardData);

            JObject jObject = new JObject {
                {"TaskInfo", GetJson_TaskInfo(cardData) },
                {"Files", GetJson_FilesInfo(dvTask) },
                {"History", GetJson_History(dvTask) },
            };
            return Encoding.UTF8.GetBytes(jObject.ToString());
        }
        
        private static byte[] GetCountTasks(string account)
        {
            JObject jObject = new JObject { };

            foreach (string key in savedSearches_ActiveTasks.Keys)
            {
                jObject.Add( key, SearchActiveTasks(savedSearches_ActiveTasks[key], account).Count);               
            }
            return Encoding.UTF8.GetBytes(jObject.ToString());
        }

        private static byte[] EndTask(string taskId, string result, string comment, string account)
        {
            CardData cardData = Session.CardManager.GetCardData(new Guid(taskId));
            RowData rdMainInfo = cardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow;
            RowData rdProp = cardData.Sections[new Guid("E1DB203C-EAB1-4084-A971-A0F47FBA56FE")].FirstRow;

            SetFieldValue(rdMainInfo, "ExecutionStopped", true);
            SetFieldValue(rdMainInfo, "EndDateActual", DateTime.Now.ToString());
            SetFieldValue(rdMainInfo, "CompletedUser", DocsvisionHelpers.GetEmployeeRowData_ByAccount(account)["RowID"].ToString());

            SetFieldValue(rdProp, "custResult", result);
            SetFieldValue(rdProp, "custComment", comment);

            return Encoding.UTF8.GetBytes("success");
        }

        private static CardDataCollection SearchActiveTasks(string savedSearchId, string account)
        {
            SearchQuery query = CreateSearchQueryFormSaved(savedSearchId);
            query.Parameters["paramalias1"].Value = "ps\\" + account;

            CardDataCollection cdColl = Session.CardManager.FindCards(query.GetXml());

            return cdColl;
        }

        private static CardDataCollection SearchFinishedTasks(string savedSearchId, string account, DateTime date1, DateTime date2)
        {        
            SearchQuery query = CreateSearchQueryFormSaved(savedSearchId);
            query.Parameters.First(p => p.Name == "account").Value = "ps\\" + account;
            query.Parameters.First(p => p.Name == "date1").Value = date1;
            query.Parameters.First(p => p.Name == "date2").Value = date2;

            CardDataCollection cdColl = Session.CardManager.FindCards(query.GetXml());

            return cdColl;
        }

        private static SearchQuery CreateSearchQueryFormSaved(string savedSearchId)
        {
            SearchCard searchCard = (SearchCard)Session.CardManager.GetDictionary(new Guid(SEARCH_CARD_TYPE));
            SavedSearchQuery savedQuery = searchCard.GetQuery(new Guid(savedSearchId));
            return savedQuery.Export();
        }

        private static JArray GetJson_ListTaskInfo(CardDataCollection cdColl)
        {
            JArray jArray = new JArray();
            
            foreach (CardData cardData in cdColl)
            {
                jArray.Add(GetJson_TaskInfo(cardData));
            }
            return jArray;
        }
  
        private static JObject GetJson_TaskInfo(CardData cardData)
        {
            RowData rdMainInfo = cardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow;
           
            DocsvisionTask dvTask = new DocsvisionTask(cardData);
            DocsvisionDocument dvDoc = dvTask.ParentDocument;
            string parentDoc_Kind = dvDoc.Kind;
            JObject parentDoc_JsonInfo;
            parentDoc_JsonInfo = dvDoc.ToJSON();
          
            var json = new JObject
            {
                { "TaskId", cardData.Id.ToString() },
                { "Kind", dvTask.Kind},
                { "Desc", cardData.Description },
                { "Name", dvTask.GetMainInfoFieldString("Name") },
                { "State",  dvTask.State},
                { "EndDate",   dvTask.GetMainInfoFieldDateTime("EndDate") },
                { "PerformerGroup", dvTask.PerformerGroup},
                { "PerformerEmployee", dvTask.PerformerEmployee},
                { "Notice", "" },
                {
                    "Document", parentDoc_JsonInfo
                }
            };
            return json;
        }

        private static JArray GetJson_FilesInfo(DocsvisionTask dvTask)
        {
            JArray jArray = new JArray();
            List<RowData> files_rdc = new List<RowData>();        
            string taskKind = dvTask.Kind;
            DocsvisionDocument dvDoc = dvTask.ParentDocument;
            DocsvisionDocumentFiles docFiles = new DocsvisionDocumentFiles(dvDoc, taskKind);
            return docFiles.ToJSON();
        }

        private static JArray GetJson_History(DocsvisionTask dvTask)
        {
            string taskKind = dvTask.Kind;
            DocsvisionDocument dvParentDoc = dvTask.ParentDocument;

            string docKind = dvParentDoc.Kind;
            string secHistoryId = "";
            if (docKind == "Договор" || taskKind == "Ознакомление")
                secHistoryId = "62176671-9806-4488-A3B9-D2D03016E252";
            else
                secHistoryId = "48E4B96B-A083-4B89-9FD0-E3F822A82A8E";

            DocsvisionDocumentHistory docHistory = new DocsvisionDocumentHistory(secHistoryId, dvParentDoc);
            return docHistory.ToJSON();         
        }

        
        private static bool SetFieldValue(RowData rowData, string fieldName, string fieldValue)
        {
            try
            {
                rowData[fieldName] = fieldValue;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool SetFieldValue(RowData rowData, string fieldName, bool fieldValue)
        {
            try
            {
                rowData[fieldName] = fieldValue;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static DateTime EndOfDate(DateTime dt)
        {
            return dt.Date.AddDays(1).AddSeconds(-1);
        }
    }        
    }
