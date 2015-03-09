using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory to provide logic for SequenceNode.
    /// </summary>
    [Obsolete("Obsolete as of Contract Configurator 0.6.7, please use the completeInSequence attribute instead.")]
    public class SequenceNodeFactory : ParameterFactory
    {
        public override ContractParameter Generate(Contract contract)
        {
            LoggingUtil.LogWarning(this, "SequenceNode is obsolete as of Contract Configurator 0.6.7, please use the completeInSequence attribute instead.");
            return new Parameters.SequenceNode(title);
        }
    }
}
