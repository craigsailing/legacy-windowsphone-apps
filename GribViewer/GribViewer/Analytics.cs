using Microsoft.Phone.Info;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GribViewer
{
    class Analytics
    {
        static bool trace = true;

        /// <summary>
        /// Start call this in Application_Launching and Application_Activated
        /// </summary>
        public static void Start()
        {
            if (Debugger.IsAttached)
            {
                trace = false;
            }

            if (trace)
            {
                FlurryWP8SDK.Api.StartSession("ANALYTICS KEY NEEDED");
                LogEventWithMemmoryData("ApplicationStart");
            }
        }

        /// <summary>
        /// Call in Application_Closing if you want to record metric is on exit
        /// </summary>
        public static void End()
        {
            if (trace)
            {
                var memUsedPeak = DeviceStatus.ApplicationPeakMemoryUsage;
                LogEvent("ApplicationClosing", new KeyValuePair<string, string>("ApplicationPeakMemoryUsage", memUsedPeak.ToString()));
            }
        }

        public static void LogException(String message, Exception exception)
        {
            if (trace)
            {
                FlurryWP8SDK.Api.LogError(message, exception);
            }
        }

        public static void LogEvent(string eventId, KeyValuePair<string, string> paramaterData, bool bTimed = false /*call EndTimedEvent if true*/)
        {
            
            if (trace)
            {
                List<FlurryWP8SDK.Models.Parameter> parameters = new List<FlurryWP8SDK.Models.Parameter>();
                parameters.Add(new FlurryWP8SDK.Models.Parameter(paramaterData.Key, paramaterData.Value));

                LogEvent(eventId, parameters, bTimed);
            }
        }

        public static void LogEvent(string eventId, List<KeyValuePair<string, string>> paramatersData, bool bTimed = false /*call EndTimedEvent if true*/)
        {
            
            if (trace)
            {
                List<FlurryWP8SDK.Models.Parameter> parameters = new List<FlurryWP8SDK.Models.Parameter>();
                foreach (KeyValuePair<string,string> item in paramatersData)
                {
                    parameters.Add(new FlurryWP8SDK.Models.Parameter(item.Key, item.Value));
                }

                LogEvent(eventId, parameters, bTimed);
            }
        }

        private static void LogEvent(string eventId, List<FlurryWP8SDK.Models.Parameter> parameters, bool bTimed = false /*call EndTimedEvent if true*/)
        {
            if (trace)
            {
                FlurryWP8SDK.Api.LogEvent(eventId, parameters, bTimed);
            }
        }
        
        public static void LogEvent(string eventId, bool bTimed = false /*call EndTimedEvent if true*/)
        {
            if (trace)
            {
                FlurryWP8SDK.Api.LogEvent(eventId, bTimed);
            }
        }

        public static void LogEventWithMemmoryData(string eventId)
        {
            var memUsed = DeviceStatus.ApplicationCurrentMemoryUsage;
            LogEvent(eventId, new KeyValuePair<string,string>("ApplicationCurrentMemoryUsage", memUsed.ToString()) );
        }

        public static void EndTimedEvent(string eventId)
        {
            if (trace)
            {
                FlurryWP8SDK.Api.EndTimedEvent(eventId);
            }
        }

        public static void LogPage()
        {
            if (trace)
            {
                FlurryWP8SDK.Api.LogPageView();
            }
        }
    }
}
