using System;
using DocsVision.Platform.ObjectManager;
using CardDefs = DocsVision.BackOffice.CardLib.CardDefs;

namespace DocsvisionSocketServer
{
    class SessionManager
    {
        private static Properties.Settings settings = Properties.Settings.Default;

        private static UserSession session = null;

        private static CardData refStaff = null;
        private static CardData refStates = null;
        private static CardData refKinds = null;
        private static CardData refPartners = null;
        private static CardData refUni = null;

        private static SectionData secStaffEmployees = null;
        private static SectionData secStaffUnits = null;
        private static SectionData secPartnersCompanies = null;
        private static SectionData secUniItems = null;

        private static DateTime sessionLastUsing;

        private static readonly int MEMORY_MAX_MB = settings.MemoryThresholdMB;
   
        private static int GetTotalMemoryUsing()
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            return (int)(currentProcess.PrivateMemorySize64 / 1024 / 1024);
        }

        public static UserSession Session
        {
            get
            {
                int memoryMB = GetTotalMemoryUsing();
                if (sessionLastUsing < DateTime.Now.AddMinutes(-1) && memoryMB > MEMORY_MAX_MB)
                {
                    LogManager.Write($"Объём занимаемой памяти ({memoryMB} МБ) превысил максимальное значение ({MEMORY_MAX_MB} МБ), сессия Docsvision будет пересоздана");
                    Disconnect();
                }
                if (session == null)
                    session = Connect();
                try
                {
                    session.Awake();
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(ex, "Не удалось выполнить Awake сесиии, будет создана новая сессия");
                    session = Connect();
                }
                sessionLastUsing = DateTime.Now;
                return session;
            }
        }

        public static CardData RefStaff
        {
            get
            {
                if (refStaff == null)
                    refStaff = Session.CardManager.GetDictionaryData(CardDefs.RefStaff.ID);
                return refStaff;
            }
        }

        public static SectionData SecStaffEmployees
        {
            get
            {
                if (secStaffEmployees == null)
                    secStaffEmployees = RefStaff.Sections[CardDefs.RefStaff.Employees.ID];
                return secStaffEmployees;
            }
        }

        public static SectionData SecStaffUnits
        {
            get
            {
                if (secStaffUnits == null)
                    secStaffUnits = RefStaff.Sections[CardDefs.RefStaff.Units.ID];
                return secStaffUnits;
            }
        }

        public static CardData RefPartners
        {
            get
            {
                if (refPartners == null)
                    refPartners = Session.CardManager.GetDictionaryData(CardDefs.RefPartners.ID);
                return refPartners;
            }
        }

        public static SectionData SecPartnersCompanies
        {
            get
            {
                if (secPartnersCompanies == null)
                    secPartnersCompanies = RefPartners.Sections[CardDefs.RefPartners.Companies.ID];
                return secPartnersCompanies;
            }
        }

        public static CardData RefUni
        {
            get
            {
                if (refUni == null)
                    refUni = Session.CardManager.GetDictionaryData(CardDefs.RefBaseUniversal.ID);
                return refUni;
            }
        }

        public static SectionData SecUniItems
        {
            get
            {
                if (secUniItems == null)
                    secUniItems = RefUni.Sections[CardDefs.RefBaseUniversal.Items.ID];
                return secUniItems;
            }
        }

        public static CardData RefStates
        {
            get
            {
                if (refStates == null)
                    refStates = Session.CardManager.GetDictionaryData(CardDefs.RefStates.ID);
                return refStates;
            }
        }

        public static CardData RefKinds
        {
            get
            {
                if (refKinds == null)
                    refKinds = Session.CardManager.GetDictionaryData(CardDefs.RefKinds.ID);
                return refKinds;
            }
        }

        private static UserSession Connect()
        {
            try
            {
                LogManager.Write("Подключаемся к серверу Docsvision");
                DocsVision.Platform.ObjectManager.SessionManager sessionManager = DocsVision.Platform.ObjectManager.SessionManager.CreateInstance();
                LogManager.Write($"{settings.ConnectionString}, {settings.BaseName}, {settings.User}");
                sessionManager.Connect(settings.ConnectionString, settings.BaseName, settings.User, settings.Pswd);
                UserSession userSession = sessionManager.CreateSession();
                LogManager.Write("Подключение к серверу Docsvision выполнено");
                return userSession;
            }
            catch (Exception ex)
            {
                LogManager.WriteException(ex);
                return null;
            }
        }

        private static void Disconnect()
        {
            try
            {
                LogManager.Write("Закрываем созданное подключение к серверу Docsvision");
                session.Close();
                session = null;
                refStaff = null;
                secStaffEmployees = null;
                secStaffUnits = null;
                refStates = null;
                refKinds = null;
                GC.Collect();
                LogManager.Write("Подключение к серверу Docsvision закрыто");
            }
            catch (Exception ex)
            {
                LogManager.Write(ex.ToString());
            };
        }
    }
}
