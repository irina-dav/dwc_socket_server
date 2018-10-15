using DocsVision.Platform.ObjectManager;
using DocsVision.Platform.ObjectManager.Metadata;
using DocsVision.Platform.ObjectManager.SearchModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocsvisionSocketServer
{
    class DocsvisionHelpers
    {
        public static UserSession Session => DocsvisionSessionManager.Session;

        public static string GetRowDataFieldString(RowData rowData, string fieldName)
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

        public static Guid GetRowDataFieldGuid(RowData rowData, string fieldName)
        {
            try
            {
                return new Guid(GetRowDataFieldString(rowData, fieldName));
            }
            catch
            {
                return Guid.Empty;
            }
        }

        public static string GetRowDataFieldValueDateTime(RowData rowData, string fieldName)
        {
            try
            {
                string value = GetRowDataFieldString(rowData, fieldName);
                return DateTime.Parse(value).ToString("o");
            }
            catch
            {
                return "";
            }
        }

        public static RowData GetEmployeeRowData(string employeeId)
        {
            return DocsvisionSessionManager.SecStaffEmployees.GetRow(new Guid(employeeId));
        }

        public static RowData GetEmployeeRowData_ByAccount(string account)
        {
            account = "ps\\" + account;
            SectionQuery query = Session.CreateSectionQuery();
            query.ConditionGroup.Conditions.AddNew("AccountName", FieldType.Unistring, ConditionOperation.Equals, account);
            RowData rdEmployee = DocsvisionSessionManager.SecStaffEmployees.FindRows(query.GetXml())[0];
            return rdEmployee;
        }

        public static string GetEmployeeOrgName(RowData rdEmployee)
        {
            string depId = rdEmployee["ParentRowID"].ToString();
            RowData rdDep = DocsvisionSessionManager.SecStaffUnits.GetRow(new Guid(depId));
            while (Guid.Parse(rdDep["ParentTreeRowID"].ToString()).Equals(Guid.Empty) == false)
            {
                rdDep = DocsvisionSessionManager.SecStaffUnits.GetRow(new Guid(rdDep["ParentTreeRowID"].ToString()));
            }
            return GetRowDataFieldValueDateTime(rdDep, "Telex");
        }

        public static string GetEmployeeDisplayName(string employeeId)
        {      
            RowData rdEmployee = GetEmployeeRowData(employeeId);
            return GetRowDataFieldValueDateTime(rdEmployee, "DisplayString");
        }

        public static string GetPartnerName(string partnerId)
        {
            RowData rdPartner = DocsvisionSessionManager.SecPartnersCompanies.GetRow(new Guid(partnerId));
            return GetRowDataFieldString(rdPartner, "Name");
        }

        public static string GetItemName(string itemId)
        {
            RowData rdUniItem = DocsvisionSessionManager.SecUniItems.GetRow(new Guid(itemId));
            return GetRowDataFieldString(rdUniItem, "Name");
        }
    }
}