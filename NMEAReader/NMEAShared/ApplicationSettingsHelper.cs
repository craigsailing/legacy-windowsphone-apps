using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.IsolatedStorage;
using System.Xml;
using Microsoft.Phone.Shell;

namespace NMEAShared
{
    /// <summary>
    /// 
    /// </summary>
    public static class ApplicationSettingsHelper
    {
        private static IsolatedStorageSettings _settings = IsolatedStorageSettings.ApplicationSettings;

        public static bool SaveOnChange { get; set; }

        public static bool IsFirstRun
        {
            get
            {
                if (_settings.Contains("FirstRun") == true)
                {
                    return false;
                }
                else
                {
                    StoreSetting("FirstRun", "true");
                    return true;
                }
            }
        }

        public static void StoreSetting(string settingName, string value)
        {
            StoreSetting<string>(settingName, value);
        }

        public static void StoreSetting(string settingName, int value)
        {
            StoreSetting<int>(settingName, value);
        }
           
        public static void StoreSetting<T>(string settingName, T value)
        {
            if (!_settings.Contains(settingName))
                _settings.Add(settingName, value);
            else
                _settings[settingName] = value;

            //if (SaveOnChange == true)
                Save();
        }

        public static bool TryGetSetting<T>(string settingName, out T value)
        {
            return _settings.TryGetValue<T>(settingName, out value);
        }

        public static void Save() { _settings.Save(); }

        public static string Version()
        {
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
            {
                XmlResolver = new XmlXapResolver()
            };
            using (XmlReader xmlReader = XmlReader.Create("WMAppManifest.xml", xmlReaderSettings))
            {
                xmlReader.ReadToDescendant("App");
                return xmlReader.GetAttribute("Version");
            }
        }

        public static void RunUnderLock(bool enalbled)
        {
            if (enalbled)
            {
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
                PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Disabled;
            }
            else
            {
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
                //PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Enabled;
            }
        }
    }
}
