using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KSP;
using Contracts;

namespace ContractConfigurator
{
    /// <summary>
    /// Integration to DMagic's Contracts Window +.
    /// </summary>
    public static class ContractsWindow
    {
        public static Assembly ContractsWindowAssembly { get; private set; }
        private static bool StopChecking = false;

        /// <summary>
        /// Set the contract title.
        /// </summary>
        /// <param name="contract">Contract object</param>
        /// <param name="name">New title</param>
        public static void SetContractTitle(Contract contract, string name)
        {
            if (!VerifyVersion())
            {
                return;
            }

            Type contractUtils = ContractsWindowAssembly.GetType("ContractsWindow.contractUtils");

            // Get and invoke the method
            MethodInfo method = contractUtils.GetMethod("setContractTitle");
            method.Invoke(null, new object[] { contract, name });
        }

        /// <summary>
        /// Set the contract notes.
        /// </summary>
        /// <param name="contract">Contract object</param>
        /// <param name="notes">New notes</param>
        public static void SetContractNotes(Contract contract, string notes)
        {
            if (!VerifyVersion())
            {
                return;
            }

            Type contractUtils = ContractsWindowAssembly.GetType("ContractsWindow.contractUtils");

            // Get and invoke the method
            MethodInfo method = contractUtils.GetMethod("setContractNotes");
            method.Invoke(null, new object[] { contract, notes });
        }

        /// <summary>
        /// Set the parameter title.
        /// </summary>
        /// <param name="param">ContractParameter object</param>
        /// <param name="name">New title</param>
        public static void SetParameterTitle(ContractParameter param, string name)
        {
            if (!VerifyVersion())
            {
                return;
            }

            Type contractUtils = ContractsWindowAssembly.GetType("ContractsWindow.contractUtils");

            // Get and invoke the method
            MethodInfo method = contractUtils.GetMethod("setParameterTitle");
            method.Invoke(null, new object[] { param.Root, param, name });
        }

        /// <summary>
        /// Set the parameter notes.
        /// </summary>
        /// <param name="param">ContractParameter object</param>
        /// <param name="notes">New notes</param>
        public static void SetParameterNotes(ContractParameter param, string notes)
        {
            if (!VerifyVersion())
            {
                return;
            }

            Type contractUtils = ContractsWindowAssembly.GetType("ContractsWindow.contractUtils");

            // Get and invoke the method
            MethodInfo method = contractUtils.GetMethod("setParameterNotes");
            method.Invoke(null, new object[] { param.Root, param, notes });
        }

        /// <summary>
        /// Verifies that the SCANsat version the player has is compatible.
        /// </summary>
        /// <returns>Whether the check passed.</returns>
        private static bool VerifyVersion()
        {
            string minVersion = "v3.4";
            if (ContractsWindowAssembly == null && !StopChecking)
            {
                ContractsWindowAssembly = Util.Version.VerifyAssemblyVersion("ContractsWindow", minVersion, true);
                StopChecking = true;
            }
            return ContractsWindowAssembly != null;
        }

    }
}
