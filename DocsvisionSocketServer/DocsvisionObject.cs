using DocsVision.Platform.ObjectManager;
using CardDefs = DocsVision.BackOffice.CardLib.CardDefs;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocsvisionSocketServer
{
    public class DocsvisionObject
    {
        protected CardData cardData = null;
        protected RowData rdProp = null;       
        protected RowData rdSystem = null;
        protected RowData rdMainInfo = null;

        private static UserSession Session => DocsvisionSessionManager.Session;

        public string Kind
        {
            get
            {
                string kind = "";
                //RowData rdSystem = cardData.Sections[CardDefs.CardTask.System.ID].FirstRow;
                string kindId = rdSystem["Kind"].ToString();
                kind = DocsvisionSessionManager.RefKinds.Sections[CardDefs.RefKinds.CardKinds.ID].
                    GetRow(new Guid(kindId))["Name"].ToString();

                return kind;
            }
        }

        public string State
        {
            get
            {
                string state = "";
                //RowData rdSystem = cardData.Sections[DocsVision.BackOffice.CardLib.CardDefs.CardTask.System.ID].FirstRow;
                string stateId = rdSystem["State"].ToString();
                state = DocsvisionSessionManager.RefStates.Sections[CardDefs.RefStates.States.ID].
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
        public SectionData GetSectionData(string sectionId)
        {
            return cardData.Sections[new Guid(sectionId)];
        }

        public string GetMainInfoFieldString(string fieldName)
        {
            return DocsvisionHelpers.GetRowDataFieldString(rdMainInfo, fieldName);
        }

        public string GetPropertyFieldString(string fieldName)
        {
            return DocsvisionHelpers.GetRowDataFieldString(rdProp, fieldName);
        }

        public string GetMainInfoFieldDateTime(string fieldName)
        {
            return DocsvisionHelpers.GetRowDataFieldValueDateTime(rdMainInfo, fieldName);
        }

        public string GetPropertyFieldDateTime(string fieldName)
        {
            return DocsvisionHelpers.GetRowDataFieldValueDateTime(rdProp, fieldName);
        }

        public string GetPropertyPartnerName(string fieldName)
        {
            string partnerId = GetPropertyFieldString(fieldName);
            return DocsvisionHelpers.GetPartnerName(partnerId);
        }

        public string GetPropertyItemName(string fieldName)
        {
            string itemId = GetPropertyFieldString(fieldName);
            return DocsvisionHelpers.GetItemName(itemId);           
        }

        public string GetPropertyEmployeeName(string fieldName)
        {
            string employeeId = GetPropertyFieldString(fieldName);
            return DocsvisionHelpers.GetEmployeeDisplayName(employeeId);
        }
    }
}
