using System.Configuration;
using System.Linq;

namespace DocsvisionSocketServer.Properties {
    internal sealed partial class Settings {
        
        public Settings() {
            LogManager.Write($"Загружаем конфигурационные данные");
            this.SettingsLoaded += (object sender, System.Configuration.SettingsLoadedEventArgs e) =>
            {
                var props = Settings.Default.PropertyValues.Cast<SettingsPropertyValue>().ToList().Where(p=>p.PropertyValue == null);
                if (props.Count() > 0)
                {
                    LogManager.Write("Конфигурационные данные не корректны");
                    System.Console.WriteLine("Приложение будет закрыто(нажмите любую кнопку)...");
                    System.Console.ReadKey();
                    System.Environment.Exit(-1);                    
                }                
            };
        }
              
    }
}
