using System;
using DocsVision.Platform.ObjectManager;
using DocsVision.Platform.ObjectManager.Metadata;
using DocsVision.Platform.ObjectManager.SearchModel;

namespace DocsvisionSocketServer
{
    class Helpers
    {
        private static UserSession Session => SessionManager.Session;

        private static Properties.Settings settings = Properties.Settings.Default;

        public static string GetFieldValueString(RowData rowData, string fieldName)
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

        public static Guid GeFieldValueGuid(RowData rowData, string fieldName)
        {
            try
            {
                return new Guid(GetFieldValueString(rowData, fieldName));
            }
            catch
            {
                return Guid.Empty;
            }
        }

        public static string GetFieldValueFormattedDateTime(RowData rowData, string fieldName)
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

        public static RowData GetEmployeeRowData(string employeeId)
        {
            return SessionManager.SecStaffEmployees.GetRow(new Guid(employeeId));
        }

        public static RowData GetEmployeeRowDataByAccount(string account)
        {
            account = BuildAccountDomain(account);
            SectionQuery query = Session.CreateSectionQuery();
            query.ConditionGroup.Conditions.AddNew("AccountName", FieldType.Unistring, ConditionOperation.Equals, account);
            RowData rdEmployee = SessionManager.SecStaffEmployees.FindRows(query.GetXml())[0];
            return rdEmployee;
        }

        public static string GetEmployeeOrgName(RowData rdEmployee)
        {
            string depId = rdEmployee["ParentRowID"].ToString();
            RowData rdDep = SessionManager.SecStaffUnits.GetRow(new Guid(depId));
            while (Guid.Parse(rdDep["ParentTreeRowID"].ToString()).Equals(Guid.Empty) == false)
            {
                rdDep = SessionManager.SecStaffUnits.GetRow(new Guid(rdDep["ParentTreeRowID"].ToString()));
            }
            return GetFieldValueString(rdDep, "Telex");
        }

        public static string GetEmployeeDisplayName(string employeeId)
        {      
            RowData rdEmployee = GetEmployeeRowData(employeeId);
            return GetFieldValueString(rdEmployee, "DisplayString");
        }

        public static string GetPartnerName(string partnerId)
        {
            RowData rdPartner = SessionManager.SecPartnersCompanies.GetRow(new Guid(partnerId));
            return GetFieldValueString(rdPartner, "Name");
        }

        public static string GetItemName(string itemId)
        {
            RowData rdUniItem = SessionManager.SecUniItems.GetRow(new Guid(itemId));
            return GetFieldValueString(rdUniItem, "Name");
        }

        public static bool SetFieldValue(RowData rowData, string fieldName, string fieldValue)
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

        public static bool SetFieldValue(RowData rowData, string fieldName, bool fieldValue)
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

        public static string BuildAccountDomain(string account)
        {
            return $"{settings.Domain}\\{account}";
        }

        public static DateTime EndOfDate(DateTime dt)
        {
            return dt.Date.AddDays(1).AddSeconds(-1);
        }
    }
}