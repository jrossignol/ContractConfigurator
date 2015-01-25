using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ContractConfigurator.SCANsat
{
    /// <summary>
    /// Utilities for interacting with SCANsat.
    /// </summary>
    public static class SCANsatUtil
    {
        public static Assembly SCANsatAssembly { get; private set; }

        /// <summary>
        /// Validates the given SCANname and sees if SCANsat has a SCANtype that matches it.
        /// </summary>
        /// <param name="SCANname">The SCANname to validate.</param>
        /// <returns>Whether the validation succeeded, outputs an error if it did not.</returns>
        public static bool ValidateSCANname(string SCANname)
        {
            try
            {
                GetSCANtype(SCANname);
            }
            catch (Exception e)
            {
                LoggingUtil.LogException(e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the SCANtype for the given SCANname.
        /// </summary>
        /// <param name="SCANname">The name of the SCAN type</param>
        /// <returns>The integer SCANtype</returns>
        public static int GetSCANtype(string SCANname)
        {
            Type scanUtil = SCANsatAssembly.GetType("SCANsat.SCANUtil");

            // Get and invoke the method
            MethodInfo methodGetSCANtype = scanUtil.GetMethod("GetSCANtype");
            return (int)methodGetSCANtype.Invoke(null, new object[] { SCANname });
        }

        /// <summary>
        /// Wrapper for SCANutil.GetCoverage
        /// </summary>
        public static double GetCoverage(int SCANtype, CelestialBody body)
        {
            Type scanUtil = SCANsatAssembly.GetType("SCANsat.SCANUtil");

            // Get and invoke the method
            MethodInfo methodGetCoverage = scanUtil.GetMethod("GetCoverage");
            return (double)methodGetCoverage.Invoke(null, new object[] { SCANtype, body });
        }

        /// <summary>
        /// Wrapper for SCANutil.isCovered
        /// </summary>
        public static bool IsCovered(double lat, double lon, int SCANtype, CelestialBody body)
        {
            Type scanUtil = SCANsatAssembly.GetType("SCANsat.SCANUtil");

            // Get and invoke the method
            MethodInfo methodIsCovered = scanUtil.GetMethod("isCovered",
                new Type[] {typeof(double), typeof(double), typeof(CelestialBody), typeof(int)});
            return (bool)methodIsCovered.Invoke(null, new object[] { lat, lon, body, SCANtype });
        }

        /// <summary>
        /// Verifies that the SCANsat version the player has is compatible.
        /// </summary>
        /// <returns>Whether the check passed.</returns>
        public static bool VerifySCANsatVersion()
        {
            string minVersion = "v9.0";
            if (SCANsatAssembly == null)
            {
                SCANsatAssembly = ContractConfigurator.VerifyAssemblyVersion("SCANsat", minVersion);
            }
            return SCANsatAssembly != null;
        }
    }
}
