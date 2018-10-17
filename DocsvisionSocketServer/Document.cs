using System;

using Newtonsoft.Json.Linq;

using DocsVision.Platform.ObjectManager;
using CardDefs = DocsVision.BackOffice.CardLib.CardDefs;

namespace DocsvisionSocketServer
{
    public class Document : PrimaryObject
    {
        public Document(CardData cardData)
        {
            this.cardData = cardData;
            this.rdMainInfo = cardData.Sections[CardDefs.CardDocument.MainInfo.ID].FirstRow;
            this.rdSystem = cardData.Sections[CardDefs.CardDocument.System.ID].FirstRow;
        }

        public string DocNumber
        {
            get
            {
                RowData rdNumber = cardData.Sections[CardDefs.CardDocument.Numbers.ID].FirstRow;
                return Helpers.GetFieldValueString(rdNumber, "Number");
            }
        }
       
    }


    public class Contract: Document
    {
        public Contract(CardData cardData): base(cardData)
        {
           this.rdProp = cardData.Sections[new Guid("{02214C9B-1B10-49A3-AA1B-CF5932C3B1E9}")].FirstRow;
        }

        override public JObject ToJSON()
        {
            var json = new JObject
            {
                 {"Id", Id},
                 {"Kind", "Договор" },
                 {"Subject", GetPropertyFieldValueString("custSubject")},
                 {"Number", GetPropertyFieldValueString("custNumber")},
                 {"Partner", GetPropertyPartnerName("custPartner")},
                 {"Date", GetPropertyFieldValueFormattedDateTime("custDate")},
                 {"Type", GetPropertyItemName("custType")},
                 {"Form", GetPropertyItemName("custForm")},
                 {"Currency", GetPropertyItemName("custCurrency")},
                 {"Sum", GetPropertyFieldValueString("custSum")},
                 {"Performer", GetPropertyEmployeeName("custPerformer")},
                 {"SysNumber", DocNumber},
                 {"Reason", GetPropertyFieldValueString("custReason")},
                 {"AdditionalComment", GetPropertyFieldValueString("custAdditionalComment") }
             };
            return json;
        }
    }


    public class DeloDoc : Document
    {
        public DeloDoc(CardData cardData) : base(cardData)
        {
            this.rdProp = cardData.Sections[new Guid("{CBA7127E-4C67-4113-ADA9-708E09F95F80}")].FirstRow;
        }

        override public JObject ToJSON()
        {
            var json = new JObject
            {
                {"Id", Id},
                {"Kind", Kind },
                {"Description", Description},
                {"SysNumber", DocNumber },
            };
            if (Kind == "ОРД")
            {
                json.Add("Name", GetPropertyFieldValueString("custName"));
                json.Add("Type", GetPropertyItemName("custDocType"));
                json.Add("Developer", GetPropertyEmployeeName("custDeveloper"));
                json.Add("Initiator", GetPropertyEmployeeName("custInitiator"));
            }
            else if (Kind == "Запрос на изменение")
            {
                json.Add("Name", GetPropertyFieldValueString("custName"));
                json.Add("Comment", GetPropertyFieldValueString("custComment"));
                json.Add("Comment2", GetPropertyFieldValueString("custComment2"));
                json.Add("Initiator", GetPropertyEmployeeName("custInitiator"));
            }

            return json;
        }
    }

}
