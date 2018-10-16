using DocsVision.Platform.ObjectManager;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocsvisionSocketServer
{

    public sealed class HistoryType
    {
        public static readonly HistoryType ApprovingContract = new HistoryType("62176671-9806-4488-A3B9-D2D03016E252");       
        public static readonly HistoryType ApprovingDeloDoc = new HistoryType("62176671-9806-4488-A3B9-D2D03016E252");
        public static readonly HistoryType AcquaintanceDeloDoc = new HistoryType("48E4B96B-A083-4B89-9FD0-E3F822A82A8E");
        public string SectionId { get; private set; }
        public HistoryType(string sectionId)
        {
            SectionId = sectionId;
        }
    }


    public class DocumentHistory
    {
        private List<HistoryRow> historyRows = new List<HistoryRow>();

        public DocumentHistory(HistoryType historyType, Document dvDoc)
        {            
            SectionData sdCustHistory = dvDoc.GetSectionData(historyType.SectionId);
            IEnumerable<RowData> newRdc = sdCustHistory.Rows.OrderBy(r => r["SysRowTimestamp"]);
            foreach (RowData row in newRdc)
            {
                historyRows.Add(new HistoryRow(row));
            }          
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
            RowData rdEmployee = null;
            string custPerformerId = Helpers.GetRowDataFieldString(row, "custPerformerId");
            if (custPerformerId != "")
            {
                rdEmployee = Helpers.GetEmployeeRowData(custPerformerId);
            }
            else
            {
                Task dvTask = new Task(Helpers.GetRowDataFieldString(row, "custTaskId"));
                string completedUserId = dvTask.GetMainInfoFieldString("CompletedUser");
                if (completedUserId != "")
                    rdEmployee = Helpers.GetEmployeeRowData(completedUserId);
            }
            if (rdEmployee != null)
            {
                this.employeeName = Helpers.GetRowDataFieldString(rdEmployee, "DisplayString");
                this.employeePosition = Helpers.GetRowDataFieldString(rdEmployee, "PositionName");
                this.employeeOrg = Helpers.GetEmployeeOrgName(rdEmployee);
            }
            this.comment = Helpers.GetRowDataFieldString(row, "custComment");
            this.result = Helpers.GetRowDataFieldString(row, "custState");
            if (this.result == "Согласовано" && this.comment != "")
                this.result = "Согласовано с замечаниями";

            this.date = Helpers.GetRowDataFieldValueDateTime(row, "custDateTime");
        }
    }
}
