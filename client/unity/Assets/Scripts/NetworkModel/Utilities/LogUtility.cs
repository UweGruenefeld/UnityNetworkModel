/**
 * 
 * Log Utility of Unity Network Model
 *
 * @file LogUtility.cs
 * @author Uwe Gruenefeld
 * @version 2020-05-05
 *
 **/
using System;
using UnityEngine;

namespace UnityNetworkModel
{
    /// <summary>
    /// Utility class with functions related to Logging
    /// </summary>
    internal static class LogUtility
    {
        /// <summary>
        /// Log message if logType is allowed in debug level
        /// </summary>
        internal static void Log(Injector injector, LogType logType, String message)
        {
            // Check if message should not get logged
            if((ushort)injector.configuration.DEBUGLEVEL < (ushort)logType)
                return;
            
            switch(logType)
            {
                case LogType.ERROR:
                    Debug.LogError("NetworkModel: " + message);
                    break;
                case LogType.WARNING:
                    Debug.LogWarning("NetworkModel: " + message);
                    break;
                case LogType.INFORMATION:
                    Debug.Log("NetworkModel: " +  message);
                    break;
            }
        }

        /// <summary>
        /// Log message
        /// </summary>
        internal static void Log(String message)
        {
            Debug.Log("NetworkModel: " + message);
        }
    }
}