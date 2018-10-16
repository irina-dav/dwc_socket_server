using DocsVision.Platform.ObjectManager;
using DocsVision.Platform.ObjectManager.SearchModel;
using DocsVision.Platform.ObjectManager.SystemCards;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocsvisionSocketServer
{
  

    public sealed class SavedSearch
    {
        private static Properties.Settings settings = Properties.Settings.Default;

        public static readonly SavedSearch ApprovingContracts = new SavedSearch(settings.Search_ApprovingContracts, "ApprovingContracts");
        public static readonly SavedSearch ApprovingDocuments = new SavedSearch(settings.Search_ApprovingDocuments, "ApprovingDocuments");
        public static readonly SavedSearch AcquaintanceDocuments = new SavedSearch(settings.Search_AcquaintanceDocuments, "AcquaintanceDocuments");
        public static readonly SavedSearch FinApprovingContracts = new SavedSearch(settings.Search_FinApprovingContracts, "FinApprovingContracts");
        public static readonly SavedSearch FinApprovingDocuments = new SavedSearch(settings.Search_FinApprovingDocuments, "FinApprovingDocuments");
        public static readonly SavedSearch FinAcquaintanceDocuments = new SavedSearch(settings.Search_FinAcquaintanceDocuments, "FinAcquaintanceDocuments");

        public static List<SavedSearch> SearchesActiveTasks = new List<SavedSearch>()
        {
            AcquaintanceDocuments,
            ApprovingContracts,
            ApprovingDocuments,
        };

        public string SavedSeacrhId { get; private set; }
        public string Name { get; private set; }

        public SavedSearch(string savedSeacrhId, string name)
        {
            SavedSeacrhId = savedSeacrhId;
            Name = name;
        }
    }

    class TaskList
    {
        private List<Task> tasks = new List<Task>();

        private static UserSession Session => SessionManager.Session;

        private const string SEARCH_CARD_TYPE = "{05E4BE46-6304-42A7-A780-FD07F7541AF0}";


        public TaskList(SavedSearch savedSearch, string account, DateTime date1, DateTime date2)
        {
            CardDataCollection cdColl = SearchFinishedTasks(savedSearch.SavedSeacrhId, account, Helpers.EndOfDate(date1), Helpers.EndOfDate(date2));
            FetchTasksFromCardDataColl(cdColl);
        }


        public TaskList(SavedSearch savedSearch, string account)
        {
            CardDataCollection cdColl = SearchActiveTasks(savedSearch.SavedSeacrhId, account);
            FetchTasksFromCardDataColl(cdColl);
        }


        private void FetchTasksFromCardDataColl(CardDataCollection cdColl)
        {
            foreach (CardData cd in cdColl)
            {
                tasks.Add(new Task(cd));
            }
        }


        public int Count
        {
            get
            {
                return tasks.Count();
            }
        }


        public JArray ToJSON()
        {
            JArray jArray = new JArray();
            foreach (Task dvTask in tasks)
            {              
                jArray.Add(dvTask.TaskInfoToJSON());
            }
            return jArray;
        }
       

        private CardDataCollection SearchActiveTasks(string savedSearchId, string account)
        {
            SearchQuery query = CreateSearchQueryFormSaved(savedSearchId);
            query.Parameters["paramalias1"].Value = Helpers.BuildAccountDomain(account);

            CardDataCollection cdColl = Session.CardManager.FindCards(query.GetXml());

            return cdColl;
        }


        private CardDataCollection SearchFinishedTasks(string savedSearchId, string account, DateTime date1, DateTime date2)
        {
            SearchQuery query = CreateSearchQueryFormSaved(savedSearchId);
            query.Parameters.First(p => p.Name == "account").Value = Helpers.BuildAccountDomain(account);
            query.Parameters.First(p => p.Name == "date1").Value = date1;
            query.Parameters.First(p => p.Name == "date2").Value = date2;

            CardDataCollection cdColl = Session.CardManager.FindCards(query.GetXml());

            return cdColl;
        }


        private SearchQuery CreateSearchQueryFormSaved(string savedSearchId)
        {
            SearchCard searchCard = (SearchCard)Session.CardManager.GetDictionary(new Guid(SEARCH_CARD_TYPE));
            SavedSearchQuery savedQuery = searchCard.GetQuery(new Guid(savedSearchId));
            return savedQuery.Export();
        }

        
    }
}
