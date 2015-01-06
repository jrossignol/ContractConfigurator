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
    /// Class for handling part related stuff.
    /// </summary>
    public static class PartUtil
    {
        /// <summary>
        /// Gets an enumerator of all parts that have the given module.
        /// </summary>
        /// <param name="parts">Part enumerator to filter on.</param>
        /// <param name="partModule">Part module to filter by.</param>
        /// <returns>A new part enumerator</returns>
        public static IEnumerable<Part> WithModule(this IEnumerable<Part> parts, string partModule)
        {
            foreach (Part p in parts)
            {
                foreach (PartModule pm in p.Modules)
                {
                    if (pm.moduleName == partModule)
                    {
                        yield return p;
                        break;
                    }
                }
            }
        }
    }
}
