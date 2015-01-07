using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// Static class with extensions to stock classes.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Returns the name that should be printed for celestial bodies.  Basically just changes
        /// Mun to "the Mun", because it sounds better.
        /// </summary>
        /// <param name="body">The celestial body to print the name for.</param>
        /// <returns>The name of the body, for printing purpose only.</returns>
        public static string printName(this CelestialBody body)
        {
            return body.name == "Mun" ? "the Mun" : body.name;
        }
    }
}
