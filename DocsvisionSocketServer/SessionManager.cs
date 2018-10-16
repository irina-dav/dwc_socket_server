using DocsVision.Platform.ObjectManager;
using CardDefs = DocsVision.BackOffice.CardLib.CardDefs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocsvisionSocketServer
{
    class SessionManager
    {
        private static Properties.Settings settings = Properties.Settings.Default;

        private static UserSession _session = null;

        private static CardData _refStaff = null;
        private static CardData _refStates = null;
        private static CardData _refKinds = null;
        private static CardData _refPartners = null;
        private static CardData _refUni = null;

        private static SectionData _secStaffEmployees = null;
        private static SectionData _secStaffUnits = null;
        private static SectionData _secPartnersCompanies = null;
        private static SectionData _secUniItems = null;

        private static readonly int MEMORY_MAX_MB = settings.MemoryThresholdMB;

        private static int GetTotalMemoryUsing()
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            return (int)(currentProcess.PrivateMemorySize64 / 1024 / 1024);
        }

        private static DateTime sessionLastUsing;

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
                if (_session == null)
                    _session = Connect();
                try
                {
                    _session.Awake();
                }
                catch (Exception ex)
                {
                    LogManager.WriteException(ex, "Не удалось выполнить Awake сесиии, будет создана новая сессия");
                    _session = Connect();
                }
                sessionLastUsing = DateTime.Now;
                return _session;
            }
        }

        public static CardData RefStaff
        {
            get
            {
                if (_refStaff == null)
                    _refStaff = Session.CardManager.GetDictionaryData(CardDefs.RefStaff.ID);
                return _refStaff;
            }
        }

        public static SectionData SecStaffEmployees
        {
            get
            {
                if (_secStaffEmployees == null)
                    _secStaffEmployees = RefStaff.Sections[CardDefs.RefStaff.Employees.ID];
                return _secStaffEmployees;
            }
        }

        public static SectionData SecStaffUnits
        {
            get
            {
                if (_secStaffUnits == null)
                    _secStaffUnits = RefStaff.Sections[CardDefs.RefStaff.Units.ID];
                return _secStaffUnits;
            }
        }

        public static CardData RefPartners
        {
            get
            {
                if (_refPartners == null)
                    _refPartners = Session.CardManager.GetDictionaryData(CardDefs.RefPartners.ID);
                return _refPartners;
            }
        }

        public static SectionData SecPartnersCompanies
        {
            get
            {
                if (_secPartnersCompanies == null)
                    _secPartnersCompanies = RefPartners.Sections[CardDefs.RefPartners.Companies.ID];
                return _secPartnersCompanies;
            }
        }

        public static CardData RefUni
        {
            get
            {
                if (_refUni == null)
                    _refUni = Session.CardManager.GetDictionaryData(CardDefs.RefBaseUniversal.ID);
                return _refUni;
            }
        }

        public static SectionData SecUniItems
        {
            get
            {
                if (_secUniItems == null)
                    _secUniItems = RefUni.Sections[CardDefs.RefBaseUniversal.Items.ID];
                return _secUniItems;
            }
        }

        public static CardData RefStates
        {
            get
            {
                if (_refStates == null)
                    _refStates = Session.CardManager.GetDictionaryData(CardDefs.RefStates.ID);
                return _refStates;
            }
        }

        public static CardData RefKinds
        {
            get
            {
                if (_refKinds == null)
                    _refKinds = Session.CardManager.GetDictionaryData(CardDefs.RefKinds.ID);
                return _refKinds;
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
                _session.Close();
                _session = null;
                _refStaff = null;
                _secStaffEmployees = null;
                _secStaffUnits = null;
                _refStates = null;
                _refKinds = null;
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
