using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;

namespace ContractConfigurator.Util
{
    /// <summary>
    /// Utility class with version checking functionality.
    /// </summary>
    public static class Version
    {
        public static bool RemoteTechCheckDone = false;
        public static bool ResearchBodiesCheckDone = false;
        public static Assembly RemoteTechAssembly;
        public static Assembly CC_RemoteTechAssembly;
        public static Assembly ResearchBodiesAssembly;

        /// <summary>
        /// Verify the loaded assembly meets a minimum version number.
        /// </summary>
        /// <param name="name">Assembly name</param>
        /// <param name="version">Minium version</param>
        /// <param name="silent">Silent mode</param>
        /// <returns>The assembly if the version check was successful.  If not, logs and error and returns null.</returns>
        public static Assembly VerifyAssemblyVersion(string name, string version, bool silent = false)
        {
            // Logic courtesy of DMagic
            var assemblies = AssemblyLoader.loadedAssemblies.Where(a => a.assembly.GetName().Name == name);
            var assembly = assemblies.FirstOrDefault();
            if (assembly != null)
            {
                if (assemblies.Count() > 1)
                {
                    LoggingUtil.LogWarning(typeof(ContractConfigurator), StringBuilderCache.Format("Multiple assemblies with name '{0}' found!", name));
                }

                string receivedStr;

                // First try the informational version
                var ainfoV = Attribute.GetCustomAttribute(assembly.assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                if (ainfoV != null)
                {
                    receivedStr = ainfoV.InformationalVersion;
                }
                // If that fails, use the product version
                else
                {
                    receivedStr = FileVersionInfo.GetVersionInfo(assembly.assembly.Location).ProductVersion;
                }
                // If that still fails, fall back on AssemblyVersion
                if (string.IsNullOrEmpty(receivedStr) || receivedStr == " ")
                {
                    receivedStr = assembly.assembly.GetName().Version.ToString();
                }

                System.Version expected = ParseVersion(version);
                System.Version received = ParseVersion(receivedStr);

                if (received >= expected)
                {
                    LoggingUtil.LogVerbose(typeof(ContractConfigurator), "Version check for '" + name + "' passed.  Minimum required is " + version + ", version found was " + receivedStr);
                    return assembly.assembly;
                }
                else
                {
                    LoggingUtil.Log(silent ? LoggingUtil.LogLevel.DEBUG : LoggingUtil.LogLevel.ERROR, typeof(Version), "Version check for '" + name + "' failed!  Minimum required is " + version + ", version found was " + receivedStr);
                    return null;
                }
            }
            else
            {
                LoggingUtil.Log(silent ? LoggingUtil.LogLevel.VERBOSE : LoggingUtil.LogLevel.ERROR, typeof(Version), "Couldn't find assembly for '" + name + "'!");
                return null;
            }
        }

        public static System.Version ParseVersion(string version)
        {
            Match m = Regex.Match(version, @"^[vV]?(\d+)(.(\d+)(.(\d+)(.(\d+))?)?)?");
            int major = m.Groups[1].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[1].Value);
            int minor = m.Groups[3].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[3].Value);
            int build = m.Groups[5].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[5].Value);
            int revision = m.Groups[7].Value.Equals("") ? 0 : Convert.ToInt32(m.Groups[7].Value);

            return new System.Version(major, minor, build, revision);
        }

        /// <summary>
        /// Verifies that the RemoteTech version the player has is compatible.
        /// </summary>
        /// <returns>Whether the check passed.</returns>
        public static bool VerifyRemoteTechVersion()
        {
            string minVersion = "1.6.4";
            if (RemoteTechAssembly == null || !RemoteTechCheckDone)
            {
                RemoteTechAssembly = Version.VerifyAssemblyVersion("RemoteTech", minVersion);
                CC_RemoteTechAssembly = AssemblyLoader.loadedAssemblies.SingleOrDefault(a => a.assembly.GetName().Name == "CC_RemoteTech").assembly;
                RemoteTechCheckDone = true;
            }
            return RemoteTechAssembly != null;
        }

        /// <summary>
        /// Verifies that the ReesearchBodies version the player has is compatible.
        /// </summary>
        /// <returns>Whether the check passed.</returns>
        public static bool VerifyResearchBodiesVersion()
        {
            string minVersion = "1.8";
            if (ResearchBodiesAssembly == null && !ResearchBodiesCheckDone)
            {
                ResearchBodiesAssembly = Version.VerifyAssemblyVersion("ResearchBodies", minVersion, true);
                ResearchBodiesCheckDone = true;
            }

            // Check the wrapper is initalized, while we're here
            if (ResearchBodiesAssembly != null && !RBWrapper.APIRBReady)
            {
                // Initialize the Research Bodies wrapper
                bool rbInit = RBWrapper.InitRBWrapper();
                if (rbInit)
                {
                    LoggingUtil.LogInfo(typeof(ContractConfigurator), "Successfully initialized Research Bodies wrapper.");
                }
                else
                {
                    LoggingUtil.LogDebug(typeof(ContractConfigurator), "Couldn't initialize Research Bodies wrapper.");
                }
            }

            return ResearchBodiesAssembly != null;
        }
    }
}
