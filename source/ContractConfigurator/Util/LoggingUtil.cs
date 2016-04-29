using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator
{
    public class LoggingUtil
    {
        public enum LogLevel {
            VERBOSE = 0,
            DEBUG = 1,
            INFO = 2,
            WARNING = 3,
            ERROR = 4
        }

        public static LogLevel logLevel {set; get;}
        private static bool captureLog = false;
        public static bool CaptureLog
        {
            get { return captureLog; }
            set
            {
                captureLog = value;
                capturedLog = "";
            }
        }
        public static string capturedLog = "";

        private static Dictionary<string, LogLevel> specificLogLevels = new Dictionary<string, LogLevel>();

        /// <summary>
        /// Loads debugging configurations.
        /// </summary>
        public static void LoadDebuggingConfig()
        {
            Debug.Log("[INFO] ContractConfigurator.LoggingUtil: Loading DebuggingConfig node.");
            // Don't know why .GetConfigNode("CC_DEBUGGING") returns null, using .GetConfigNodes("CC_DEBUGGING") works fine.
            ConfigNode[] debuggingConfigs = GameDatabase.Instance.GetConfigNodes("CC_DEBUGGING");

            if (debuggingConfigs.Length > 0)
            {
                try
                {
                    // Fetch config
                    ConfigNode debuggingConfig = debuggingConfigs[0];

                    // Set LogLevel
                    if (debuggingConfig.HasValue("logLevel"))
                    {
                        LoggingUtil.logLevel = (LoggingUtil.LogLevel)Enum.Parse(typeof(LoggingUtil.LogLevel), debuggingConfig.GetValue("logLevel"));
                        LoggingUtil.LogInfo(typeof(LoggingUtil), "Set LogLevel = " + LoggingUtil.logLevel);
                    }

                    // Fetch specific loglevels for given types
                    foreach (ConfigNode levelExceptionNode in debuggingConfig.GetNodes("ADD_LOGLEVEL_EXCEPTION"))
                    {
                        if (levelExceptionNode.HasValue("type") && levelExceptionNode.HasValue("logLevel"))
                        {
                            // Fetch full type name - just search and find the matching one while
                            // ignoring namespace
                            string typeName = levelExceptionNode.GetValue("type");
                            Type type = null;
                            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                try
                                {
                                    foreach (Type t in a.GetTypes())
                                    {
                                        if (t.Name == typeName || t.Name.StartsWith(typeName + '`'))
                                        {
                                            type = t;
                                            break;
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Debug.LogWarning("[WARNING] Error loading types from assembly " + a.FullName + ": " + e.Message);
                                }
                            }

                            if (type != null)
                            {
                                LoggingUtil.LogLevel logLevel = (LoggingUtil.LogLevel)Enum.Parse(typeof(LoggingUtil.LogLevel), levelExceptionNode.GetValue("logLevel"));
                                LoggingUtil.AddSpecificLogLevel(type, logLevel);
                            }
                            else
                            {
                                Debug.LogWarning("[WARNING] ContractConfigurator.LoggingUtil: Couldn't find Type with name: '" + typeName + "'");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[WARNING] ContractConfigurator.LoggingUtil: Couldn't load specific LogLevel node, type or logLevel not given!");
                        }
                    }

                    LoggingUtil.LogInfo(typeof(LoggingUtil), "DebugingConfig loaded!");
                }
                catch (Exception e)
                {
                    LoggingUtil.ClearSpecificLogLevel();
                    LoggingUtil.logLevel = LoggingUtil.LogLevel.INFO;

                    LoggingUtil.LogWarning(typeof(LoggingUtil), "Debugging Config failed to load! Message: '" + e.Message + "' Set LogLevel to INFO and cleaned specific LogLevels");
                }
            }
            else
            {
                LoggingUtil.logLevel = LoggingUtil.LogLevel.INFO;
                LoggingUtil.LogWarning(typeof(LoggingUtil), "No debugging config found! LogLevel set to INFO");
            }
        }

        public static LogLevel GetLogLevel(Type t)
        {
            return specificLogLevels.ContainsKey(t.Name) ? specificLogLevels[t.Name] : logLevel;
        }


        public static void LogVerbose(System.Object obj, string message)
        {
            LoggingUtil.Log(LogLevel.VERBOSE, obj.GetType(), message);
        }

        public static void LogVerbose(Type type, string message)
        {
            LoggingUtil.Log(LogLevel.VERBOSE, type, message);
        }

        public static void LogDebug(System.Object obj, string message)
        {
            LoggingUtil.Log(LogLevel.DEBUG, obj.GetType(), message);
        }

        public static void LogDebug(Type type, string message)
        {
            LoggingUtil.Log(LogLevel.DEBUG, type, message);
        }

        public static void LogInfo(System.Object obj, string message)
        {
            LoggingUtil.Log(LogLevel.INFO, obj.GetType(), message);
        }

        public static void LogInfo(Type type, string message) {
            LoggingUtil.Log(LogLevel.INFO, type, message);
        }

        public static void LogWarning(System.Object obj, string message)
        {
            // Set the hasWarnings flag
            IContractConfiguratorFactory ccFactory = obj as IContractConfiguratorFactory;
            if (ccFactory == null && captureLog && ConfigNodeUtil.currentDataNode != null)
            {
                ccFactory = ConfigNodeUtil.currentDataNode.Factory;
            }
            else
            {
                DataNode dataNode = obj as DataNode;
                if (dataNode != null)
                {
                    ccFactory = dataNode.Factory;
                }
            }            if (ccFactory != null)
            {
                ccFactory.hasWarnings = true;
            }

            LoggingUtil.Log(LogLevel.WARNING, obj.GetType(), message);
        }

        public static void LogWarning(Type type, string message)
        {
            LoggingUtil.Log(LogLevel.WARNING, type, message);
        }

        public static void LogError(System.Object obj, string message)
        {
            LoggingUtil.Log(LogLevel.ERROR, obj.GetType(), message);
        }

        public static void LogError(Type type, string message)
        {
            LoggingUtil.Log(LogLevel.ERROR, type, message);
        }

        public static void LogException(Exception e)
        {
            if (captureLog)
            {
                capturedLog += "[EXCEPTION] ";
                CaptureException(e);
            }

            Debug.LogException(e);
        }

        private static void CaptureException(Exception e)
        {
            if (e.InnerException != null)
            {
                CaptureException(e.InnerException);
                capturedLog += "Rethrow as ";
            }
            capturedLog += e.GetType() + ": " + e.Message + "\n" + e.StackTrace + "\n";
        }


        public static void Log(LogLevel logLevel, Type type, string message)
        {
            LogLevel logLevelCheckAgainst = LoggingUtil.logLevel;
            if (specificLogLevels.ContainsKey(type.Name))
            {
                logLevelCheckAgainst = (LogLevel)Math.Min((int)logLevelCheckAgainst, (int)specificLogLevels[type.Name]);
            }

            if (logLevel >= logLevelCheckAgainst)
            {
                message = type + ": " + message;

                if (captureLog)
                {
                    capturedLog += "[" + logLevel + "] " + message + "\n";
                }

                if (logLevel <= LogLevel.INFO) {
                    Debug.Log("[" + logLevel + "] " + message);
                }
                else if (logLevel == LogLevel.WARNING) {
                    Debug.LogWarning(message);
                }
                else if (logLevel == LogLevel.ERROR) {
                    Debug.LogError(message);
                }
            }
        }

        private static void AddSpecificLogLevel(Type type, LogLevel logLevel)
        {
            specificLogLevels.Add(type.Name, logLevel);
        }

        private static void ClearSpecificLogLevel()
        {
            specificLogLevels.Clear();
        }
    }
}
