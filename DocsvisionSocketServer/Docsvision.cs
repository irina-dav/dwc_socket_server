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

        private static UserSession _session = null;
        private static CardData _refStaff = null;
        private static CardData _refStates = null;
        private static CardData _refKinds = null;
        private static SectionData _secStaffEmployees = null;
        private static SectionData _secStaffUnits = null;

        private static int MEMORY_MAX_MB = 150;
        private static int GetTotalMemoryUsing()
        {
           var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
           return (int)(currentProcess.PrivateMemorySize64/1024/1024);
        }

        private static DateTime sessionLastUsing;
        private static UserSession Session
        {
            get
            {
                int memoryMB = GetTotalMemoryUsing();
                if (sessionLastUsing < DateTime.Now.AddMinutes(-1) && memoryMB > MEMORY_MAX_MB)
                {                   
                    LogManager.Write($"Объём занимаемой памяти ({memoryMB} МБ) превысил максимальное значение ({MEMORY_MAX_MB} МБ), сессия Docsvision будет пересоздана");
                    Disconnect();                    
                }
                if (_session == null)
                    _session = Connect();
                try
                {
                    _session.Awake();                    
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(ex, "Не удалось выполнить Awake сесиии, будет создана новая сессия");
                    _session = Connect();
                }
                sessionLastUsing = DateTime.Now;
                return _session;
            }
        }

        private static CardData RefStaff
        {
            get
            {
                if (_refStaff == null)
                    _refStaff = Session.CardManager.GetDictionaryData(CardDefs.RefStaff.ID);
                return _refStaff;
            }
        }

        private static SectionData SecStaffEmployees
        {
            get
            {
                if (_secStaffEmployees == null)
                    _secStaffEmployees = RefStaff.Sections[CardDefs.RefStaff.Employees.ID];
                return _secStaffEmployees;
            }
        }

        private static SectionData SecStaffUnits
        {
            get
            {
                if (_secStaffUnits == null)
                    _secStaffUnits = RefStaff.Sections[CardDefs.RefStaff.Units.ID];
                return _secStaffUnits;
            }
        }

        private static CardData RefStates
        {
            get
            {
                if (_refStates == null)
                    _refStates = Session.CardManager.GetDictionaryData(CardDefs.RefStates.ID);
                return _refStates;
            }
        }

        private static CardData RefKinds
        {
            get
            {
                if (_refKinds == null)
                    _refKinds = Session.CardManager.GetDictionaryData(CardDefs.RefKinds.ID);
                return _refKinds;
            }
        }

        private static UserSession Connect()
        {
            try
            {
                LogManager.Write("Подключаемся к серверу Docsvision");
                SessionManager sessionManager = SessionManager.CreateInstance();
                LogManager.Write($"{settings.ConnectionString}, {settings.BaseName}, {settings.User}");
                sessionManager.Connect(settings.ConnectionString, settings.BaseName,settings.User, settings.Pswd);
                UserSession userSession = sessionManager.CreateSession();                
                LogManager.Write("Подключение к серверу Docsvision выполнено");
                return userSession;
            }
            catch (Exception ex)
            {
                LogManager.WriteException(ex);
                return null;
            }
        }

        private static void Disconnect()
        {
            try
            {
                LogManager.Write("Закрываем созданное подключение к серверу Docsvision");
                _session.Close();
                _session = null;
                _refStaff = null;
                _secStaffEmployees = null;
                _secStaffUnits = null;
                _refStates = null;
                _refKinds = null;
                GC.Collect();
                LogManager.Write("Подключение к серверу Docsvision закрыто");
            }
            catch (Exception ex)
            {
                LogManager.Write(ex.ToString());
            };
        }

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
            CardData parentDoc_cardData = GetTaskParentDoc(rdMainInfo);
            string taskKind = GetTaskKind(cardData);

            JObject jObject = new JObject {
                {"TaskInfo", GetJson_TaskInfo(cardData) },
                {"Files", GetJson_FilesInfo(parentDoc_cardData, taskKind) },
                {"History", GetJson_History(parentDoc_cardData, taskKind) },
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
            SetFieldValue(rdMainInfo, "CompletedUser", GetEmployeeRowData_ByAccount(account)["RowID"].ToString());

            SetFieldValue(rdProp, "custResult", result);
            SetFieldValue(rdProp, "custComment", comment);

            return Encoding.UTF8.GetBytes("success");
        }

        private static CardDataCollection SearchActiveTasks(string savedSearchId, string account)
        {
            string SEARCH_CARD_TYPE = "{05E4BE46-6304-42A7-A780-FD07F7541AF0}";

            SearchCard searchCard = (SearchCard)Session.CardManager.GetDictionary(new Guid(SEARCH_CARD_TYPE));
            SavedSearchQuery savedQuery = searchCard.GetQuery(new Guid(savedSearchId));
            SearchQuery query = savedQuery.Export();
            query.Parameters["paramalias1"].Value = "ps\\" + account;

            CardDataCollection cdColl = Session.CardManager.FindCards(query.GetXml());

            return cdColl;
        }

        private static CardDataCollection SearchFinishedTasks(string savedSearchId, string account, DateTime date1, DateTime date2)
        {
            string SEARCH_CARD_TYPE = "{05E4BE46-6304-42A7-A780-FD07F7541AF0}";

            SearchCard searchCard = (SearchCard)Session.CardManager.GetDictionary(new Guid(SEARCH_CARD_TYPE));
            SavedSearchQuery savedQuery = searchCard.GetQuery(new Guid(savedSearchId));
            SearchQuery query = savedQuery.Export();
            query.Parameters.First(p => p.Name == "account").Value = "ps\\" + account;
            query.Parameters.First(p => p.Name == "date1").Value = date1;
            query.Parameters.First(p => p.Name == "date2").Value = date2;

            CardDataCollection cdColl = Session.CardManager.FindCards(query.GetXml());

            return cdColl;
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
            CardData parentDoc_cardData = GetTaskParentDoc(rdMainInfo);
            string parentDoc_Kind = GetDocKind(parentDoc_cardData);
            JObject parentDoc_JsonInfo;
            if (parentDoc_Kind == "Договор")
                parentDoc_JsonInfo = GetJson_ContractInfo(parentDoc_cardData);
            else
                parentDoc_JsonInfo = GetJson_DocumentInfo(parentDoc_cardData);

            var json = new JObject
            {
                { "TaskId", cardData.Id.ToString() },
                { "Kind", GetTaskKind(cardData) },
                { "Desc", cardData.Description },
                { "Name", GetFieldValueString(rdMainInfo, "Name") },
                { "State",  GetTaskState(cardData) },
                { "EndDate",  GetFieldValueDateTime(rdMainInfo, "EndDate") },
                { "PerformerGroup", GetTaskPerformerGroup(cardData) },
                { "PerformerEmployee", GetTaskPerformerEmployee(cardData) },
                { "Notice", "" },
                {
                    "Document", parentDoc_JsonInfo
                }
            };
            return json;
        }

        private static JArray GetJson_FilesInfo(CardData parentDoc_cardData, string taskKind)
        {
            JArray jArray = new JArray();

            List<RowData> files_rdc = new List<RowData>();
            if (taskKind == "Ознакомление")
            {
                RowData mainInfo_rd = parentDoc_cardData.Sections[CardDefs.CardDocument.MainInfo.ID].FirstRow;
                string refListId = mainInfo_rd["ReferenceList"].ToString();
                CardData cardReferenceList_cd = Session.CardManager.GetCardData(new Guid(refListId));
                foreach (RowData reference_rd in cardReferenceList_cd.Sections[CardDefs.CardReferenceList.References.ID].Rows)
                {
                    if (!Guid.Parse(reference_rd["Type"].ToString()).Equals(Guid.Parse("99B86870-FCAE-4714-A6C3-C731151E2590")))
                        continue;
                    string cardId = reference_rd["Card"].ToString();
                    CardData card_cd = Session.CardManager.GetCardData(new Guid(cardId));
                    files_rdc.AddRange(card_cd.Sections[CardDefs.CardDocument.Files.ID].Rows.Cast<RowData>());
                }
            }
            else
            {
                files_rdc.AddRange(parentDoc_cardData.Sections[CardDefs.CardDocument.Files.ID].Rows.Cast<RowData>());
            }

            foreach (RowData file_rd in files_rdc)
            {
                Guid versionedFileCard_id = new Guid(file_rd["FileId"].ToString());
                CardData versionedFileCard_cardData = Session.CardManager.GetCardData(versionedFileCard_id);
                RowData mainInfo_rd = versionedFileCard_cardData.Sections[new Guid("2FDE03C2-FF87-4E42-A8C2-7CED181977FB")].FirstRow;
                string fileName = mainInfo_rd["Name"].ToString();
                RowData version_rd = versionedFileCard_cardData.Sections[new Guid("F831372E-8A76-4ABC-AF15-D86DC5FFBE12")].Rows.OrderBy(r => int.Parse(r["Version"].ToString())).First();
                string fileId = version_rd["FileID"].ToString();

                jArray.Add(new JObject
                {
                    { "FileID", fileId },
                    { "FileName", fileName },
                });
            }
            return jArray;            
        }

        private static JArray GetJson_History(CardData cardData, string taskKind)
        {
            JArray jArray = new JArray();

            string docKind = GetDocKind(cardData);
            string secHistoryId = "";
            if (docKind == "Договор" || taskKind == "Ознакомление")
                secHistoryId = "62176671-9806-4488-A3B9-D2D03016E252";
            else
                secHistoryId = "48E4B96B-A083-4B89-9FD0-E3F822A82A8E";

            SectionData sdCustHistory = cardData.Sections[new Guid(secHistoryId)];
            IEnumerable<RowData> newRdc = sdCustHistory.Rows.OrderBy(r => r["SysRowTimestamp"]);
            foreach (RowData item in newRdc)
            {

                string employeeName = "", employeePosition = "", employeeOrg, comment = "", result = "", date = "";
                RowData rdEmployee = null;
                string custPerformerId = GetFieldValueString(item, "custPerformerId");
                if (custPerformerId != "")
                {
                    rdEmployee = GetEmployeeRowData(custPerformerId);
                }
                else
                {
                    CardData taskCardData = Session.CardManager.GetCardData(Guid.Parse(item["custTaskId"].ToString()));
                    RowData rdMainInfo = taskCardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow;
                    if (rdMainInfo["CompletedUser"] != null)
                        rdEmployee = GetEmployeeRowData(GetFieldValueString(rdMainInfo, "CompletedUser"));
                }
                if (rdEmployee == null)
                    continue;

                employeeName = GetFieldValueString(rdEmployee, "DisplayString");
                employeePosition = GetFieldValueString(rdEmployee, "PositionName");
                employeeOrg = GetEmployeeOrgName(rdEmployee);

                comment = GetFieldValueString(item, "custComment");

                result = GetFieldValueString(item, "custState");
                if (result == "Согласовано" && comment != "")
                    result = "Согласовано с замечаниями";

                date = GetFieldValueDateTime(item, "custDateTime");

                jArray.Add(new JObject
                {
                    { "employeeName", employeeName },
                    { "employeePosition", employeePosition},
                    { "employeeOrg", employeeOrg},
                    { "comment", comment},
                    { "result", result},
                    { "date", date}
                });
            }
            return jArray;
        }

        private static string GetDocKind(CardData cardData)
        {
            RowData rdSystemInfo = cardData.Sections[CardDefs.CardDocument.System.ID].FirstRow;
            return GetFieldValueString(rdSystemInfo, "Kind_Name");
        }

        private static string GetTaskKind(CardData cardData)
        {
            string kind = "";
            RowData rdSystem = cardData.Sections[CardDefs.CardTask.System.ID].FirstRow;
            string kindId = rdSystem["Kind"].ToString();
            kind = RefKinds.Sections[CardDefs.RefKinds.CardKinds.ID].
                GetRow(new Guid(kindId))["Name"].ToString();

            return kind;
        }

        private static JObject GetJson_ContractInfo(CardData cardData)
        {
            RowData rdProps = cardData.Sections[new Guid("{02214C9B-1B10-49A3-AA1B-CF5932C3B1E9}")].FirstRow;
            var json = new JObject
            {
                {"Id", cardData.Id.ToString("B")},
                {"Kind", "Договор" },
                { "Subject", GetFieldValueString(rdProps, "custSubject")},
                {"Number", GetFieldValueString(rdProps, "custNumber")},
                {"Partner", GetFieldPartnerName(rdProps, "custPartner")},
                {"Date", GetFieldValueDateTime(rdProps, "custDate")},
                {"Type", GetFieldItemName(rdProps, "custType")},
                {"Form", GetFieldItemName(rdProps, "custForm")},
                {"Currency", GetFieldItemName(rdProps, "custCurrency")},
                {"Sum", GetFieldValueString(rdProps, "custSum")},
                {"Performer", GetFieldEmployeeName(rdProps, "custPerformer")},
                {"SysNumber", GetDocNumber(cardData)},
                {"Reason", GetFieldValueString(rdProps, "custReason")},
                {"AdditionalComment", GetFieldValueString(rdProps, "custAdditionalComment") }
            };
            return json;
        }

        private static JObject GetJson_DocumentInfo(CardData cardData)
        {
            RowData rdProps = cardData.Sections[new Guid("{CBA7127E-4C67-4113-ADA9-708E09F95F80}")].FirstRow;
            string docKind = GetDocKind(cardData);

            var json = new JObject
            {
                {"Id", cardData.Id.ToString("B")},
                {"Kind", docKind },
                {"Description", cardData.Description},
                {"SysNumber", GetDocNumber(cardData) },               
            };


            var jsonDetail = new JObject();
            if (docKind == "ОРД")
            {
                json.Add("Name", GetFieldValueString(rdProps, "custName"));
                json.Add("Type", GetFieldItemName(rdProps, "custDocType"));
                json.Add("Developer", GetFieldEmployeeName(rdProps, "custDeveloper"));
                json.Add("Initiator", GetFieldEmployeeName(rdProps, "custInitiator"));
            }
            else if (docKind == "Запрос на изменение")
            {
                json.Add("Name", GetFieldValueString(rdProps, "custName"));
                json.Add("Comment", GetFieldValueString(rdProps, "custComment"));
                json.Add("Comment2", GetFieldValueString(rdProps, "custComment2"));
                json.Add("Initiator", GetFieldEmployeeName(rdProps, "custInitiator"));
            }

            return json;
        }

        private static CardData GetTaskParentDoc(RowData rdMainInfo)
        {
            Guid referenceList_id = new Guid(rdMainInfo["ReferenceList"].ToString());
            CardData referenceList_cardData = Session.CardManager.GetCardData(referenceList_id);
            RowData referenceFirstRow_rd = referenceList_cardData.Sections[CardDefs.CardReferenceList.References.ID].FirstRow;

            Guid parentDoc_id = new Guid(referenceFirstRow_rd["Card"].ToString());
            CardData parentDoc_cardData = Session.CardManager.GetCardData(parentDoc_id);

            return parentDoc_cardData;
        }

        private static string GetTaskState(CardData cardData)
        {
            string state = "";
            RowData rdSystem = cardData.Sections[CardDefs.CardTask.System.ID].FirstRow;
            string stateId = rdSystem["State"].ToString();
            state = RefStates.Sections[CardDefs.RefStates.States.ID].
                GetRow(new Guid(stateId)).
                ChildSections[CardDefs.RefStates.StateNames.ID].FirstRow["Name"].ToString();

            return state;
        }

        private static string GetTaskPerformerEmployee(CardData cardData)
        {
            string performerEmployee = "";
            SubSectionData secPerformers = cardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow.ChildSections[CardDefs.CardTask.Performers.ID];
            CardData refStaff = Session.CardManager.GetDictionaryData(CardDefs.RefStaff.ID);
            SectionData secStaffEmployees = refStaff.Sections[CardDefs.RefStaff.Employees.ID];
            try
            {
                var performersIds = secPerformers.Rows.Cast<RowData>().ToList().Select(r => Guid.Parse(r["Employee"].ToString()));
                performerEmployee = string.Join("; ", performersIds.Select(i => secStaffEmployees.GetRow(i)["DisplayString"]));
            }
            catch
            {
                performerEmployee = "не удалось определить";
            }
            return performerEmployee;
        }

        private static string GetTaskPerformerGroup(CardData cardData)
        {
            string performerGroup;
            SubSectionData secSelectedPerformers = cardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow.ChildSections[CardDefs.CardTask.SelectedPerformers.ID];
            CardData refStaff = Session.CardManager.GetDictionaryData(CardDefs.RefStaff.ID);
            SectionData secStaffGroups = refStaff.Sections[CardDefs.RefStaff.AlternateHierarchy.ID];
            try
            {
                if (secSelectedPerformers.FirstRow["Group"] == null)
                    performerGroup = "";
                else
                {
                    RowData rdGroup = secStaffGroups.GetRow(new Guid(secSelectedPerformers.FirstRow["Group"].ToString()));
                    performerGroup = rdGroup["Name"].ToString();
                }
            }
            catch
            {
                performerGroup = "не удалось определить";
            }
            return performerGroup;
        }

        private static RowData GetEmployeeRowData(string employeeId)
        {
            return SecStaffEmployees.GetRow(new Guid(employeeId));
        }

        private static RowData GetEmployeeRowData_ByAccount(string account)
        {
            account = "ps\\" + account;
            SectionQuery query = Session.CreateSectionQuery();
            query.ConditionGroup.Conditions.AddNew("AccountName", FieldType.Unistring, ConditionOperation.Equals, account);
            RowData rdEmployee = SecStaffEmployees.FindRows(query.GetXml())[0];
            return rdEmployee;
        }

        private static string GetEmployeeOrgName(RowData rdEmployee)
        {
            string depId = rdEmployee["ParentRowID"].ToString();
            RowData rdDep = SecStaffUnits.GetRow(new Guid(depId));
            while (Guid.Parse(rdDep["ParentTreeRowID"].ToString()).Equals(Guid.Empty) == false)
            {
                rdDep = SecStaffUnits.GetRow(new Guid(rdDep["ParentTreeRowID"].ToString()));
            }
            return GetFieldValueString(rdDep, "Telex");
        }

        private static string GetDocNumber(CardData cardData)
        {
            RowData rdNumber = cardData.Sections[CardDefs.CardDocument.Numbers.ID].FirstRow;
            return GetFieldValueString(rdNumber, "Number");
        }

        private static string GetFieldValueString(RowData rowData, string fieldName)
        {
            try
            {
                return rowData[fieldName].ToString();
            }
            catch
            {
                return "";
            }
        }

        private static string GetFieldValueDateTime(RowData rowData, string fieldName)
        {
            try
            {
                string value = GetFieldValueString(rowData, fieldName);
                return DateTime.Parse(value).ToString("o");
            }
            catch
            {
                return "";
            }
        }

        private static string GetFieldPartnerName(RowData rowData, string fieldName)
        {
            string partnerName;
            try
            {
                string partnerId = rowData[fieldName].ToString();
                CardData refPartners = Session.CardManager.GetDictionaryData(CardDefs.RefPartners.ID);
                SectionData secCompanies = refPartners.Sections[CardDefs.RefPartners.Companies.ID];
                RowData rdPartner = secCompanies.GetRow(new Guid(partnerId));
                partnerName = rdPartner["Name"].ToString();
            }
            catch
            {
                partnerName = "не удалось определить";
            }
            return partnerName;
        }

        private static string GetFieldItemName(RowData rowData, string fieldName)
        {
            string itemName;
            try
            {
                string itemId = rowData[fieldName].ToString();
                CardData refUni = Session.CardManager.GetDictionaryData(CardDefs.RefBaseUniversal.ID);
                SectionData secItems = refUni.Sections[CardDefs.RefBaseUniversal.Items.ID];
                RowData rdItem = secItems.GetRow(new Guid(itemId));
                itemName = rdItem["Name"].ToString();
            }
            catch
            {
                itemName = "не удалось определить";
            }
            return itemName;
        }

        private static string GetFieldEmployeeName(RowData rowData, string fieldName)
        {
            string employeeName;
            try
            {
                string employeeId = rowData[fieldName].ToString();
                CardData refStaff = Session.CardManager.GetDictionaryData(CardDefs.RefStaff.ID);
                SectionData secEmployees = refStaff.Sections[CardDefs.RefStaff.Employees.ID];
                RowData rdEmployee = secEmployees.GetRow(new Guid(employeeId));
                employeeName = rdEmployee["DisplayString"].ToString();
            }
            catch
            {
                employeeName = "не удалось определить";
            }
            return employeeName;
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
