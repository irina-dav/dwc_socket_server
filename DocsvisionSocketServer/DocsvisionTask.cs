using DocsVision.Platform.ObjectManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardDefs = DocsVision.BackOffice.CardLib.CardDefs;

namespace DocsvisionSocketServer
{
    class DocsvisionTask
    {
        private readonly CardData cardData = null;      

        public DocsvisionTask(CardData cardData)
        {
            this.cardData = cardData;
        }

        public string State
        {
            get
            {
                string state = "";
                RowData rdSystem = cardData.Sections[DocsVision.BackOffice.CardLib.CardDefs.CardTask.System.ID].FirstRow;
                string stateId = rdSystem["State"].ToString();
                state = DocsvisionSessionManager.RefStates.Sections[CardDefs.RefStates.States.ID].
                    GetRow(new Guid(stateId)).
                    ChildSections[CardDefs.RefStates.StateNames.ID].FirstRow["Name"].ToString();

                return state;
            }
        }

        public string PerformerEmployee
        {
            get
            {
                string performerEmployee = "";
                SubSectionData secPerformers = cardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow.ChildSections[CardDefs.CardTask.Performers.ID];
                SectionData secStaffEmployees = DocsvisionSessionManager.RefStaff.Sections[CardDefs.RefStaff.Employees.ID];
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
                SectionData secStaffGroups = DocsvisionSessionManager.RefStaff.Sections[CardDefs.RefStaff.AlternateHierarchy.ID];
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

        public string Kind
        {
            get
            {
                string kind = "";
                RowData rdSystem = cardData.Sections[CardDefs.CardTask.System.ID].FirstRow;
                string kindId = rdSystem["Kind"].ToString();
                kind = DocsvisionSessionManager.RefKinds.Sections[CardDefs.RefKinds.CardKinds.ID].
                    GetRow(new Guid(kindId))["Name"].ToString();

                return kind;
            }
        }

        public CardData ParentDoc
        {
            get
            {
                RowData rdMainInfo = cardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow;
                Guid referenceList_id = new Guid(rdMainInfo["ReferenceList"].ToString());
                CardData referenceList_cardData = DocsvisionSessionManager.Session.CardManager.GetCardData(referenceList_id);
                RowData referenceFirstRow_rd = referenceList_cardData.Sections[CardDefs.CardReferenceList.References.ID].FirstRow;

                Guid parentDoc_id = new Guid(referenceFirstRow_rd["Card"].ToString());
                CardData parentDoc_cardData = DocsvisionSessionManager.Session.CardManager.GetCardData(parentDoc_id);

                return parentDoc_cardData;
            }
        }
    }
}
