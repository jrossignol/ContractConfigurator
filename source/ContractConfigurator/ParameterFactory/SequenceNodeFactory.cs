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
    public class SequenceNodeFactory : ParameterFactory
    {
        public override ContractParameter Generate(Contract contract)
        {
            return new Parameters.SequenceNode(title);
        }
    }
}
