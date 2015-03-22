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
        /// Gets all the parameter's descendents
        /// </summary>
        /// <param name="p">Contract parameter</param>
        /// <returns>Enumerator of descendents</returns>
        public static IEnumerable<ContractParameter> GetAllDescendents(this IContractParameterHost p)
        {
            for (int i = 0; i < p.ParameterCount; i++)
            {
                ContractParameter child = p.GetParameter(i);
                yield return child;
                foreach (ContractParameter descendent in child.GetAllDescendents())
                {
                    yield return descendent;
                }
            }
        }

        /// <summary>
        /// Gets all the parameter's descendents
        /// </summary>
        /// <param name="p">Contract parameter</param>
        /// <returns>Enumerator of descendents</returns>
        public static IEnumerable<ContractParameter> GetChildren(this IContractParameterHost p)
        {
            for (int i = 0; i < p.ParameterCount; i++)
            {
                yield return p.GetParameter(i);
            }
        }

        /// <summary>
        /// Gets all the kerbals for the given roster.
        /// </summary>
        /// <param name="p">Contract parameter</param>
        /// <returns>Enumerator of descendents</returns>
        public static IEnumerable<ProtoCrewMember> AllKerbals(this KerbalRoster roster)
        {
            for (int i = 0; i < roster.Count; i++)
            {
                yield return roster[i];
            }
        }
    }
}
