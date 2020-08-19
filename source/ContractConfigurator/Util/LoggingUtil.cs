using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
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
                if (captureLog != value)
                {
                    captureLog = value;
                    _capturedLog = StringBuilderCache.Acquire(1024);
                }
            }
        }
        public static string capturedLog
        {
            get
            {
                string s = _capturedLog.ToStringAndRelease();
                CaptureLog = false;
                return s;
            }
        }
        private static StringBuilder _capturedLog = StringBuilderCache.Acquire(1024);

        private static Dictionary<string, LogLevel> specificLogLevels = new Dictionary<string, LogLevel>();

        /// <summary>
        /// Loads debugging configurations.
        /// </summary>
        /// 
        public static void LoadDebuggingConfig()
        {
            UnityEngine.Debug.Log("[INFO] ContractConfigurator.LoggingUtil: Loading DebuggingConfig node.");
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
                        LoggingUtil.logLevel = (LoggingUtil.LogLevel)Enum.Parse(typeof(LoggingUtil.LogLevel), debuggingConfig.GetValue("logLevel"), true);
                        LoggingUtil.LogInfo(typeof(LoggingUtil), StringBuilderCache.Format("Set LogLevel = {0}", LoggingUtil.logLevel));
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
                                    UnityEngine.Debug.LogWarning(StringBuilderCache.Format("[WARNING] Error loading types from assembly {0}: {1}", a.FullName, e.Message));
                                }
                            }

                            if (type != null)
                            {
                                LoggingUtil.LogLevel logLevel = (LoggingUtil.LogLevel)Enum.Parse(typeof(LoggingUtil.LogLevel), levelExceptionNode.GetValue("logLevel"), true);
                                LoggingUtil.AddSpecificLogLevel(type, logLevel);
                                LoggingUtil.LogDebug(typeof(LoggingUtil), "Added log level override ({0} => {1})", type.Name, logLevel);
                            }
                            else
                            {
                                UnityEngine.Debug.LogWarning(StringBuilderCache.Format("[WARNING] ContractConfigurator.LoggingUtil: Couldn't find Type with name: '{0}'", typeName));
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("[WARNING] ContractConfigurator.LoggingUtil: Couldn't load specific LogLevel node, type or logLevel not given!");
                        }
                    }

                    LoggingUtil.LogInfo(typeof(LoggingUtil), "Debugging config loaded!");
                }
                catch (Exception e)
                {
                    LoggingUtil.ClearSpecificLogLevel();
                    LoggingUtil.logLevel = LoggingUtil.LogLevel.INFO;

                    LoggingUtil.LogWarning(typeof(LoggingUtil), StringBuilderCache.Format("Debugging Config failed to load! Message: '{0}' Set LogLevel to INFO and cleaned specific LogLevels", e.Message));
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

        [Conditional("DEBUG")]
        public static void LogVerbose(System.Object obj, string message, params object[] parameters)
        {
            LoggingUtil.Log(LogLevel.VERBOSE, obj.GetType(), message, parameters);
        }

        [Conditional("DEBUG")]
        public static void LogVerbose(Type type, string message, params Object[] parameters)
        {
            LoggingUtil.Log(LogLevel.VERBOSE, type, message, parameters);
        }

        public static void LogDebug(System.Object obj, string message, params object[] parameters)
        {
            LoggingUtil.Log(LogLevel.DEBUG, obj.GetType(), message, parameters);
        }

        public static void LogDebug(Type type, string message, params object[] parameters)
        {
            LoggingUtil.Log(LogLevel.DEBUG, type, message, parameters);
        }

        public static void LogInfo(System.Object obj, string message, params object[] parameters)
        {
            LoggingUtil.Log(LogLevel.INFO, obj.GetType(), message, parameters);
        }

        public static void LogInfo(Type type, string message, params Object[] parameters)
        {
            LoggingUtil.Log(LogLevel.INFO, type, message, parameters);
        }

        public static void LogWarning(System.Object obj, string message, params object[] parameters)
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
            }
            if (ccFactory != null)
            {
                ccFactory.hasWarnings = true;
            }

            LoggingUtil.Log(LogLevel.WARNING, obj.GetType(), message, parameters);
        }

        public static void LogWarning(Type type, string message, params object[] parameters)
        {
            LoggingUtil.Log(LogLevel.WARNING, type, message, parameters);
        }

        public static void LogError(System.Object obj, string message, params object[] parameters)
        {
            LoggingUtil.Log(LogLevel.ERROR, obj.GetType(), message, parameters);
        }

        public static void LogError(Type type, string message, params object[] parameters)
        {
            LoggingUtil.Log(LogLevel.ERROR, type, message, parameters);
        }

        public static void LogException(Exception e)
        {
            if (captureLog)
            {
                _capturedLog.Append("[EXCEPTION] ");
                CaptureException(e);
            }

            UnityEngine.Debug.LogException(e);
        }

        private static void CaptureException(Exception e)
        {
            if (e.InnerException != null)
            {
                CaptureException(e.InnerException);
                _capturedLog.Append("Rethrow as ");
            }
            _capturedLog.Append(StringBuilderCache.Format("{0}: {1}\n{2}\n", e.GetType(), e.Message, e.StackTrace));
        }

        public static void Log(LogLevel logLevel, System.Object obj, string message, params object[] parameters)
        {
            // Need to handle special warnings for loaded types
            if (logLevel == LogLevel.WARNING)
            {
                LogWarning(obj, message, parameters);
            }
            else
            {
                Log(logLevel, obj != null ? obj.GetType() : null, message, parameters);
            }
        }


        public static void Log(LogLevel logLevel, Type type, string message, params object[] parameters)
        {
            LogLevel logLevelCheckAgainst = LoggingUtil.logLevel;
            if (logLevel <= LogLevel.DEBUG && specificLogLevels.ContainsKey(type.Name))
            {
                logLevelCheckAgainst = (LogLevel)Math.Min((int)logLevelCheckAgainst, (int)specificLogLevels[type.Name]);
            }

            if (logLevel >= logLevelCheckAgainst)
            {
                if (captureLog)
                {
                    _capturedLog.Append(StringBuilderCache.Format("[{0}] {1}: {2}\n", logLevel, type, (parameters.Length > 0 ? StringBuilderCache.Format(message, parameters) : message)));
                }
                message = StringBuilderCache.Format(logLevel <= LogLevel.INFO ? "[{0}] {1}: {2}" : "{1}: {2}", logLevel, type, (parameters.Length > 0 ? StringBuilderCache.Format(message, parameters) : message));

                if (logLevel <= LogLevel.INFO)
                {
                    UnityEngine.Debug.Log(message);
                }
                else if (logLevel == LogLevel.WARNING)
                {
                    UnityEngine.Debug.LogWarning(message);
                }
                else if (logLevel == LogLevel.ERROR)
                {
                    UnityEngine.Debug.LogError(message);
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
