using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMEAShared
{
    public class Analytics
    {
        static bool trace = true;
        
        public static void Start(string appID)
        {
            if (Debugger.IsAttached)
            {
                trace = false;
            }

            if (trace)
            {
                FlurryWP8SDK.Api.StartSession(appID);
            }
        }

        public static void LogException(String message, Exception exception)
        {
            if (trace)
            {
                FlurryWP8SDK.Api.LogError(message, exception);
            }
        }

        public static void LogEvent(string eventId, List<FlurryWP8SDK.Models.Parameter> parameters)
        {
            if (trace)
            {
                FlurryWP8SDK.Api.LogEvent(eventId, parameters);
            }
        }

        public static void LogEvent(string eventId)
        {
            if (trace)
            {
                FlurryWP8SDK.Api.LogEvent(eventId);
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
