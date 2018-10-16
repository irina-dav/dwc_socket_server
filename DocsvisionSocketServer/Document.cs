using DocsVision.Platform.ObjectManager;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardDefs = DocsVision.BackOffice.CardLib.CardDefs;

namespace DocsvisionSocketServer
{
    public class Document : DocsvisionObject
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
                return Helpers.GetRowDataFieldString(rdNumber, "Number");
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
                 {"Subject", GetPropertyFieldString("custSubject")},
                 {"Number", GetPropertyFieldString("custNumber")},
                 {"Partner", GetPropertyPartnerName("custPartner")},
                 {"Date", GetPropertyFieldDateTime("custDate")},
                 {"Type", GetPropertyItemName("custType")},
                 {"Form", GetPropertyItemName("custForm")},
                 {"Currency", GetPropertyItemName("custCurrency")},
                 {"Sum", GetPropertyFieldString("custSum")},
                 {"Performer", GetPropertyEmployeeName("custPerformer")},
                 {"SysNumber", DocNumber},
                 {"Reason", GetPropertyFieldString("custReason")},
                 {"AdditionalComment", GetPropertyFieldString("custAdditionalComment") }
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
                json.Add("Name", GetPropertyFieldString("custName"));
                json.Add("Type", GetPropertyItemName("custDocType"));
                json.Add("Developer", GetPropertyEmployeeName("custDeveloper"));
                json.Add("Initiator", GetPropertyEmployeeName("custInitiator"));
            }
            else if (Kind == "Запрос на изменение")
            {
                json.Add("Name", GetPropertyFieldString("custName"));
                json.Add("Comment", GetPropertyFieldString("custComment"));
                json.Add("Comment2", GetPropertyFieldString("custComment2"));
                json.Add("Initiator", GetPropertyEmployeeName("custInitiator"));
            }

            return json;
        }
    }

}
