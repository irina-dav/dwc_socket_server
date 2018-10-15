using DocsVision.Platform.ObjectManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardDefs = DocsVision.BackOffice.CardLib.CardDefs;

namespace DocsvisionSocketServer
{
    class DocsvisionTask: DocsvisionObject
    {
        //private readonly CardData cardData = null;
        private DocsvisionDocument parentDocument = null;

        private void setUp(CardData cardData)
        {
            this.cardData = cardData;
            this.rdSystem = cardData.Sections[CardDefs.CardTask.System.ID].FirstRow;
            this.rdMainInfo = cardData.Sections[CardDefs.CardTask.MainInfo.ID].FirstRow;
            this.rdProp = cardData.Sections[new Guid("{E1DB203C-EAB1-4084-A971-A0F47FBA56FE}")].FirstRow;
        }

        public DocsvisionTask(CardData cardData)
        {
            setUp(cardData);
        }

        public DocsvisionTask(string taskId)
        {
            CardData cd = DocsvisionSessionManager.Session.CardManager.GetCardData(new Guid(taskId));
            setUp(cd);
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

        private CardData ParentDocCardData
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

        public DocsvisionDocument ParentDocument
        {
            get
            {
                if (this.parentDocument == null)
                {
                    DocsvisionDocument dvDoc = new DocsvisionDocument(ParentDocCardData);
                    if (dvDoc.Kind == "Договор")
                        this.parentDocument = new Contract(ParentDocCardData);
                    else
                        this.parentDocument = new DeloDoc(ParentDocCardData);
                }
                return this.parentDocument;
            }
        }
    }
}
