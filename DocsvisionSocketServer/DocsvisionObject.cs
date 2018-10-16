using DocsVision.Platform.ObjectManager;
using CardDefs = DocsVision.BackOffice.CardLib.CardDefs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DocsvisionSocketServer
{
    public class DocsvisionObject
    {
        protected CardData cardData = null;
        protected RowData rdProp = null;       
        protected RowData rdSystem = null;
        protected RowData rdMainInfo = null;

        private static UserSession Session => SessionManager.Session;

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


        public string Id
        {
            get
            {
                return cardData.Id.ToString("B");
            }
        }


        public string Description
        {
            get
            {
                return cardData.Description;
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


        public string GetMainInfoFieldString(string fieldName)
        {
            return Helpers.GetRowDataFieldString(rdMainInfo, fieldName);
        }


        public string GetPropertyFieldString(string fieldName)
        {
            return Helpers.GetRowDataFieldString(rdProp, fieldName);
        }

        public string GetMainInfoFieldDateTime(string fieldName)
        {
            return Helpers.GetRowDataFieldValueDateTime(rdMainInfo, fieldName);
        }


        public string GetPropertyFieldDateTime(string fieldName)
        {
            return Helpers.GetRowDataFieldValueDateTime(rdProp, fieldName);
        }


        public string GetPropertyPartnerName(string fieldName)
        {
            string partnerId = GetPropertyFieldString(fieldName);
            return Helpers.GetPartnerName(partnerId);
        }

        public string GetPropertyItemName(string fieldName)
        {
            string itemId = GetPropertyFieldString(fieldName);
            return Helpers.GetItemName(itemId);           
        }


        public string GetPropertyEmployeeName(string fieldName)
        {
            string employeeId = GetPropertyFieldString(fieldName);
            return Helpers.GetEmployeeDisplayName(employeeId);
        }
    }
}
