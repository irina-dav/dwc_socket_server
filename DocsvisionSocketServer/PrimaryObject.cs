using System;
using Newtonsoft.Json.Linq;
using DocsVision.Platform.ObjectManager;
using CardDefs = DocsVision.BackOffice.CardLib.CardDefs;

namespace DocsvisionSocketServer
{
    public class PrimaryObject
    {
        protected CardData cardData = null;
        protected RowData rdProp = null;       
        protected RowData rdSystem = null;
        protected RowData rdMainInfo = null;

        protected static UserSession Session => SessionManager.Session;

        public string Id => cardData.Id.ToString("B");

        public string Description => cardData.Description;

        public string Kind
        {
            get
            {
                string kind = "";
                string kindId = rdSystem["Kind"].ToString();
                kind = SessionManager.RefKinds.Sections[CardDefs.RefKinds.CardKinds.ID].
                    GetRow(new Guid(kindId))["Name"].ToString();

                return kind;
            }
        }

        public string State
        {
            get
            {
                string state = "";
                string stateId = rdSystem["State"].ToString();
                state = SessionManager.RefStates.Sections[CardDefs.RefStates.States.ID].
                    GetRow(new Guid(stateId)).
                    ChildSections[CardDefs.RefStates.StateNames.ID].FirstRow["Name"].ToString();

                return state;
            }
        }

        virtual public JObject ToJSON()
        {
            return new JObject();
        }
    
        public SectionData GetSectionData(string sectionId)
        {
            return cardData.Sections[new Guid(sectionId)];
        }

        public string GetMainFieldValueString(string fieldName)
        {
            return Helpers.GetFieldValueString(rdMainInfo, fieldName);
        }

        public string GetPropertyFieldValueString(string fieldName)
        {
            return Helpers.GetFieldValueString(rdProp, fieldName);
        }

        public string GetMainFieldValueFormattedDateTime(string fieldName)
        {
            return Helpers.GetFieldValueFormattedDateTime(rdMainInfo, fieldName);
        }

        public string GetPropertyFieldValueFormattedDateTime(string fieldName)
        {
            return Helpers.GetFieldValueFormattedDateTime(rdProp, fieldName);
        }

        public string GetPropertyPartnerName(string fieldName)
        {
            string partnerId = GetPropertyFieldValueString(fieldName);
            return Helpers.GetPartnerName(partnerId);
        }

        public string GetPropertyItemName(string fieldName)
        {
            string itemId = GetPropertyFieldValueString(fieldName);
            return Helpers.GetItemName(itemId);           
        }

        public string GetPropertyEmployeeName(string fieldName)
        {
            string employeeId = GetPropertyFieldValueString(fieldName);
            return Helpers.GetEmployeeDisplayName(employeeId);
        }
    }
}
