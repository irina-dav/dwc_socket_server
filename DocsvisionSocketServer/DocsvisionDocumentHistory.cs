using DocsVision.Platform.ObjectManager;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocsvisionSocketServer
{
    public class DocsvisionDocumentHistory
    {
        private List<HistoryRow> historyRows = new List<HistoryRow>();

        public DocsvisionDocumentHistory(string secHistoryId, DocsvisionDocument dvDoc)
        {
            SectionData sdCustHistory = dvDoc.GetSectionData(secHistoryId);
            IEnumerable<RowData> newRdc = sdCustHistory.Rows.OrderBy(r => r["SysRowTimestamp"]);
            //foreach (RowData item in newRdc)
            foreach (RowData row in newRdc)
            {
                historyRows.Add(new HistoryRow(row));
            }
            /*
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
                            */


        }
        public JArray ToJSON()
        {
            JArray jArray = new JArray();
            foreach (HistoryRow row in historyRows)
            {
                jArray.Add(new JObject
                    {
                        { "employeeName", row.employeeName },
                        { "employeePosition", row.employeePosition},
                        { "employeeOrg", row.employeeOrg},
                        { "comment", row.comment},
                        { "result", row.result},
                        { "date", row.date}
                    });
            }
            return jArray;
        }
    }

    class HistoryRow
    {
        public string employeeName;
        public string employeePosition;
        public string employeeOrg;
        public string comment;
        public string result;
        public string date;

        public HistoryRow(RowData row)
        {
            //string employeeName = "", employeePosition = "", employeeOrg, comment = "", result = "", date = "";
            RowData rdEmployee = null;
            string custPerformerId = DocsvisionHelpers.GetRowDataFieldString(row, "custPerformerId");
            if (custPerformerId != "")
            {
                rdEmployee = DocsvisionHelpers.GetEmployeeRowData(custPerformerId);
            }
            else
            {
                DocsvisionTask dvTask = new DocsvisionTask(DocsvisionHelpers.GetRowDataFieldString(row, "custTaskId"));
                //CardData taskCardData = Session.CardManager.GetCardData(Guid.Parse(item["custTaskId"].ToString()));
                //RowData rdMainInfo = taskCardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow;
                string completedUserId = dvTask.GetMainInfoFieldString("CompletedUser");
                if (completedUserId != "")
                    rdEmployee = DocsvisionHelpers.GetEmployeeRowData(completedUserId);
            }
            if (rdEmployee != null)
            {
                this.employeeName = DocsvisionHelpers.GetRowDataFieldString(rdEmployee, "DisplayString");
                this.employeePosition = DocsvisionHelpers.GetRowDataFieldString(rdEmployee, "PositionName");
                this.employeeOrg = DocsvisionHelpers.GetEmployeeOrgName(rdEmployee);
            }
            this.comment = DocsvisionHelpers.GetRowDataFieldString(row, "custComment");
            this.result = DocsvisionHelpers.GetRowDataFieldString(row, "custState");
            if (this.result == "Согласовано" && this.comment != "")
                this.result = "Согласовано с замечаниями";

            this.date = DocsvisionHelpers.GetRowDataFieldValueDateTime(row, "custDateTime");
        }

    }
}
