using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP;
using Contracts;
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

        /// <summary>
        /// Gets all the parameter's descendents
        /// </summary>
        /// <param name="p">Contract parameter</param>
        /// <returns>Enumerator of descendents</returns>
        public static IEnumerable<ContractParameter> GetAllDescendents(this ContractParameter p)
        {
            foreach (ContractParameter child in p.AllParameters)
            {
                if (child != p)
                {
                    yield return child;
                    foreach (ContractParameter descendent in child.GetAllDescendents())
                    {
                        yield return descendent;
                    }
                }
            }
        }
    }
}
