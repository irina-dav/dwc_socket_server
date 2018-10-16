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
    class TaskFiles
    {
        public static UserSession Session => SessionManager.Session;

        List<RowData> files_rdc = new List<RowData>();
        List<FileRow> filesRows = new List<FileRow>();

        public TaskFiles(Task dvTask)
        {
            Document dvParentDoc = dvTask.ParentDocument;
            if (dvTask.Kind == "Ознакомление")
            {                
                string refListId = dvParentDoc.GetMainInfoFieldString("ReferenceList");
                CardData cardReferenceList_cd = Session.CardManager.GetCardData(new Guid(refListId));
                foreach (RowData reference_rd in cardReferenceList_cd.Sections[CardDefs.CardReferenceList.References.ID].Rows)
                {
                    if (!Guid.Parse(reference_rd["Type"].ToString()).Equals(Guid.Parse("99B86870-FCAE-4714-A6C3-C731151E2590")))
                        continue;
                    string cardId = reference_rd["Card"].ToString();
                    CardData card_cd = Session.CardManager.GetCardData(new Guid(cardId));
                    files_rdc.AddRange(card_cd.Sections[CardDefs.CardDocument.Files.ID].Rows.Cast<RowData>());
                }
            }
            else
            {
                files_rdc.AddRange(dvParentDoc.GetSectionData(CardDefs.CardDocument.Files.ID.ToString()).Rows.Cast<RowData>());
            }

            foreach (RowData file_rd in files_rdc)
            {
                filesRows.Add(new FileRow(file_rd));
            }
        }

        public JArray ToJSON()
        {
            JArray jArray = new JArray();
            foreach (FileRow row in filesRows)
            {
                jArray.Add(new JObject
                {
                    { "FileID", row.fileId },
                    { "FileName", row.fileName },
                });
            }
            return jArray;
        }

    }

    class FileRow
    {
        public string fileId;
        public string fileName;

        public FileRow(RowData row)
        {
            Guid versionedFileCard_id = new Guid(row["FileId"].ToString());
            CardData versionedFileCard_cardData = SessionManager.Session.CardManager.GetCardData(versionedFileCard_id);
            RowData mainInfo_rd = versionedFileCard_cardData.Sections[new Guid("2FDE03C2-FF87-4E42-A8C2-7CED181977FB")].FirstRow;
            this.fileName = mainInfo_rd["Name"].ToString();
            RowData version_rd = versionedFileCard_cardData.Sections[new Guid("F831372E-8A76-4ABC-AF15-D86DC5FFBE12")].Rows.OrderBy(r => int.Parse(r["Version"].ToString())).First();
            this.fileId = version_rd["FileID"].ToString();
        }
    }
}