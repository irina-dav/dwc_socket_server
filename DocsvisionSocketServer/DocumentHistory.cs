using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using DocsVision.Platform.ObjectManager;

namespace DocsvisionSocketServer
{
    public sealed class HistoryType
    {
        public static readonly HistoryType ApprovingContract = new HistoryType("62176671-9806-4488-A3B9-D2D03016E252");       
        public static readonly HistoryType AcquaintanceDeloDoc = new HistoryType("62176671-9806-4488-A3B9-D2D03016E252");
        public static readonly HistoryType ApprovingDeloDoc = new HistoryType("48E4B96B-A083-4B89-9FD0-E3F822A82A8E");

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
                jArray.Add(row.ToJSON()); 
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
            string custPerformerId = Helpers.GetFieldValueString(row, "custPerformerId");
            if (custPerformerId != "")
            {
                rdEmployee = Helpers.GetEmployeeRowData(custPerformerId);
            }
            else
            {
                Task dvTask = new Task(Helpers.GetFieldValueString(row, "custTaskId"));
                string completedUserId = dvTask.GetMainFieldValueString("CompletedUser");
                if (completedUserId != "")
                    rdEmployee = Helpers.GetEmployeeRowData(completedUserId);
            }
            if (rdEmployee != null)
            {
                employeeName = Helpers.GetFieldValueString(rdEmployee, "DisplayString");
                employeePosition = Helpers.GetFieldValueString(rdEmployee, "PositionName");
                employeeOrg = Helpers.GetEmployeeOrgName(rdEmployee);
            }
            comment = Helpers.GetFieldValueString(row, "custComment");
            result = Helpers.GetFieldValueString(row, "custState");
            if (result == "Согласовано" && comment != "")
                result = "Согласовано с замечаниями";

            date = Helpers.GetFieldValueFormattedDateTime(row, "custDateTime");
        }

        public JObject ToJSON()
        {
            var json = new JObject
            {
                { "employeeName", employeeName},
                { "employeePosition", employeePosition},
                { "employeeOrg", employeeOrg},
                { "comment", comment},
                { "result", result},
                { "date", date}
            };
            return json;
        }
    }
}
