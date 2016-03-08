using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;
using ContractConfigurator.ExpressionParser;
namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// BehaviourFactory wrapper for CopyCraftFile ContractBehaviour.
    /// </summary>
    public class ChangeKerbalTypeFactory : BehaviourFactory
    {
        protected List<ChangeKerbalType.KerbalInfo> kerbalInfo;
        protected TriggeredBehaviour.State onState;
        protected List<string> parameter = new List<string>();

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<TriggeredBehaviour.State>(configNode, "onState", x => onState = x, this, TriggeredBehaviour.State.CONTRACT_SUCCESS);
            if (onState == TriggeredBehaviour.State.PARAMETER_COMPLETED || onState == TriggeredBehaviour.State.PARAMETER_FAILED)
            {
                valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "parameter", x => parameter = x, this);
            }

            kerbalInfo = new List<ChangeKerbalType.KerbalInfo>();
            int index = 0;
            foreach (ConfigNode child in ConfigNodeUtil.GetChildNodes(configNode, "KERBAL_INFO"))
            {
                string kerbInfoNode = "KERBAL_INFO" + index++;
                DataNode childDataNode = new DataNode(kerbInfoNode, dataNode, this);

                try
                {
                    ConfigNodeUtil.SetCurrentDataNode(childDataNode);
                    ChangeKerbalType.KerbalInfo kerb = new ChangeKerbalType.KerbalInfo();
                    kerbalInfo.Add(kerb);

                    valid &= ConfigNodeUtil.ParseValue<Kerbal>(child, "kerbal", x => kerb.kerbal = x, this);
                    valid &= ConfigNodeUtil.ParseValue<ProtoCrewMember.KerbalType?>(child, "kerbalType", x => kerb.kerbalType = x, this, (ProtoCrewMember.KerbalType?)null);
                    valid &= ConfigNodeUtil.ParseValue<string>(child, "trait", x => kerb.trait = x, this, "");
                }
                finally
                {
                    ConfigNodeUtil.SetCurrentDataNode(dataNode);
                }
            }
            valid &= ConfigNodeUtil.ValidateMandatoryChild(configNode, "KERBAL_INFO", this);

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new ChangeKerbalType(kerbalInfo, onState, parameter);
        }
    }
}
