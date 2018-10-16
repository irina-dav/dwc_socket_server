using DocsVision.Platform.ObjectManager;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardDefs = DocsVision.BackOffice.CardLib.CardDefs;

namespace DocsvisionSocketServer
{
    class Task: DocsvisionObject
    {
        private DocumentHistory documentHistory = null;
        private TaskFiles taskFiles = null;
        private Document parentDocument = null;


        private void SetUp(CardData cardData)
        {
            this.cardData = cardData;
            this.rdSystem = cardData.Sections[CardDefs.CardTask.System.ID].FirstRow;
            this.rdMainInfo = cardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow;
            this.rdProp = cardData.Sections[new Guid("{E1DB203C-EAB1-4084-A971-A0F47FBA56FE}")].FirstRow;
        }


        public Task(CardData cardData)
        {
            SetUp(cardData);
        }


        public Task(string taskId)
        {
            CardData cd = SessionManager.Session.CardManager.GetCardData(new Guid(taskId));
            SetUp(cd);
        }
  

        override public JObject ToJSON()
        {        
            JObject jObject = new JObject {
                {"TaskInfo", TaskInfoToJSON() },
                {"Files", Files.ToJSON() },
                {"History", History.ToJSON()},
            };

            return jObject;
        }

        public JObject TaskInfoToJSON()
        {
            var jsonTaskInfo = new JObject
            {
                { "TaskId", Id },
                { "Kind", Kind},
                { "Desc", Description },
                { "Name", GetMainInfoFieldString("Name") },
                { "State",  State},
                { "EndDate",   GetMainInfoFieldDateTime("EndDate") },
                { "PerformerGroup", PerformerGroup},
                { "PerformerEmployee", PerformerEmployee},
                { "Notice", "" },
                { "Document",  ParentDocument.ToJSON()}
            };
            return jsonTaskInfo;
        }

        public TaskFiles Files
        {
            get
            {
                if (this.taskFiles == null)
                {
                    this.taskFiles = new TaskFiles(this);
                }
                return this.taskFiles;
            }
        }


        public DocumentHistory History
        {
            get
            {
                if (this.documentHistory == null)
                {
                    this.documentHistory = new DocumentHistory(GetHistoryType(), ParentDocument);
                }
                return this.documentHistory;
            }
        }


        protected HistoryType GetHistoryType()
        {
            HistoryType historyType = null;
            if (ParentDocument.Kind == "Договор")
                historyType = HistoryType.ApprovingContract;
            else if (this.Kind == "Ознакомление")
                historyType = HistoryType.AcquaintanceDeloDoc;
            else
                historyType = HistoryType.ApprovingDeloDoc;
            return historyType;
        }


        public string PerformerEmployee
        {
            get
            {
                string performerEmployee = "";
                SubSectionData secPerformers = cardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow.ChildSections[CardDefs.CardTask.Performers.ID];
                SectionData secStaffEmployees = SessionManager.RefStaff.Sections[CardDefs.RefStaff.Employees.ID];
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
        }


        public string PerformerGroup
        {
            get
            {
                string performerGroup;
                SubSectionData secSelectedPerformers = cardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow.ChildSections[CardDefs.CardTask.SelectedPerformers.ID];
                SectionData secStaffGroups = SessionManager.RefStaff.Sections[CardDefs.RefStaff.AlternateHierarchy.ID];
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
        }


        private CardData ParentDocCardData
        {
            get
            {
                RowData rdMainInfo = cardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow;
                Guid referenceList_id = new Guid(rdMainInfo["ReferenceList"].ToString());
                CardData referenceList_cardData = SessionManager.Session.CardManager.GetCardData(referenceList_id);
                RowData referenceFirstRow_rd = referenceList_cardData.Sections[CardDefs.CardReferenceList.References.ID].FirstRow;

                Guid parentDoc_id = new Guid(referenceFirstRow_rd["Card"].ToString());
                CardData parentDoc_cardData = SessionManager.Session.CardManager.GetCardData(parentDoc_id);

                return parentDoc_cardData;
            }
        }


        public Document ParentDocument
        {
            get
            {
                if (this.parentDocument == null)
                {
                    Document dvDoc = new Document(ParentDocCardData);
                    if (dvDoc.Kind == "Договор")
                        this.parentDocument = new Contract(ParentDocCardData);
                    else
                        this.parentDocument = new DeloDoc(ParentDocCardData);
                }
                return this.parentDocument;
            }
        }


        public void EndTask(string account, string result, string comment)
        {
            Helpers.SetFieldValue(rdMainInfo, "ExecutionStopped", true);
            Helpers.SetFieldValue(rdMainInfo, "EndDateActual", DateTime.Now.ToString());
            Helpers.SetFieldValue(rdMainInfo, "CompletedUser", Helpers.GetEmployeeRowData_ByAccount(account)["RowID"].ToString());

            Helpers.SetFieldValue(rdProp, "custResult", result);
            Helpers.SetFieldValue(rdProp, "custComment", comment);
        }
    }
}
