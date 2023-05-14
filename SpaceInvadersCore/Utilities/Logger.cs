using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceInvadersCore.Utilities
{
    //  █        ███     ████    ████   █████   ████
    //  █       █   █   █       █       █       █   █
    //  █       █   █   █       █       █       █   █
    //  █       █   █   █       █       ████    ████
    //  █       █   █   █  ██   █  ██   █       █ █
    //  █       █   █   █   █   █   █   █       █  █
    //  █████    ███     ████    ████   █████   █   █
    
    /// <summary>
    /// A very rudimentary logger, that allows spawning of multiple logs with little effort
    /// </summary>
    internal static class Logger
    {
        /// <summary>
        /// Tracks open logs.
        /// </summary>
        private readonly static Dictionary<string, StreamWriter> Logs = new();

        /// <summary>
        /// Logs text to a specific log.
        /// </summary>
        /// <param name="logName"></param>
        /// <param name="text"></param>
        internal static void Log(string logName, string text)
        {
            logName = logName.ToUpper();
            
            if (!Logs.ContainsKey(logName))
            {
                Logs[logName] = new(Path.Combine(@"c:\temp\" + logName + ".log"))
                {
                    AutoFlush = true
                };
            }

            Logs[logName].WriteLine(text);
            Logs[logName].Flush();            
        }

        /// <summary>
        /// Logs variable parameters to a specific log.
        /// </summary>
        /// <param name="logName"></param>
        /// <param name="text"></param>
        /// <param name="parameters"></param>
        internal static void Log(string logName, string text, params object[] parameters)
        {
            logName = logName.ToUpper();
            
            Log(logName, string.Format(text, parameters));
        }

        /// <summary>
        /// Closes a specific log.
        /// </summary>
        /// <param name="logName"></param>
        internal static void CloseLog(string logName)
        {
            if (Logs.TryGetValue(logName, out StreamWriter? value))
            {
                value.Close();
                Logs[logName].Dispose();
                Logs.Remove(logName);
            }
        }

        /// <summary>
        /// Closes all logs.
        /// </summary>
        internal static void CloseAll()
        {
            foreach (var log in Logs.Values)
            {
                log.Close();
                log.Dispose();
            }

            Logs.Clear();
        }
    }
}
