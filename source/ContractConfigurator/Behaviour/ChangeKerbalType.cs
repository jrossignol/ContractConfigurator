using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Behaviour for changing a Kerbal's type
    /// </summary>
    public class ChangeKerbalType : TriggeredBehaviour
    {
        public class KerbalInfo
        {
            public Kerbal kerbal;
            public string trait;
            public ProtoCrewMember.KerbalType? kerbalType;
        }

        protected List<KerbalInfo> kerbalInfo = new List<KerbalInfo>();

        public ChangeKerbalType()
            : base()
        {
        }

        public ChangeKerbalType(List<KerbalInfo> kerbalInfo, State onState, List<string> parameter)
            : base(onState, parameter)
        {
            this.kerbalInfo = kerbalInfo.ToList();
        }

        protected override void TriggerAction()
        {
            foreach (KerbalInfo kerb in kerbalInfo)
            {
                if (kerb.kerbal.pcm != null)
                {
                    if (kerb.kerbalType != null)
                    {
                        LoggingUtil.LogDebug(this, "Setting type  of " + kerb.kerbal.name + " to " + kerb.kerbalType);
                        kerb.kerbal.pcm.type = kerb.kerbalType.Value;
                    }

                    if (!string.IsNullOrEmpty(kerb.trait))
                    {
                        LoggingUtil.LogDebug(this, "Setting trait of " + kerb.kerbal.name + " to " + kerb.trait);
                        KerbalRoster.SetExperienceTrait(kerb.kerbal.pcm, kerb.trait);
                    }
                }
                else
                {
                    LoggingUtil.LogWarning(this, "Couldn't change type of Kerbal " + kerb.kerbal.name + ", no ProtoCrewMember!");
                }
            }
        }

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);

            foreach (KerbalInfo kerb in kerbalInfo)
            {
                ConfigNode child = new ConfigNode("KERBAL_INFO");
                configNode.AddNode(child);

                child.AddValue("kerbal", kerb.kerbal.name);
                if (kerb.kerbalType != null)
                {
                    child.AddValue("kerbalType", kerb.kerbalType.Value);
                }
                if (!string.IsNullOrEmpty(kerb.trait))
                {
                    child.AddValue("trait", kerb.trait);
                }
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);

            foreach (ConfigNode child in configNode.GetNodes("KERBAL_INFO"))
            {
                KerbalInfo kerb = new KerbalInfo();
                kerbalInfo.Add(kerb);

                kerb.kerbal = ConfigNodeUtil.ParseValue<Kerbal>(child, "kerbal");
                kerb.kerbalType = ConfigNodeUtil.ParseValue<ProtoCrewMember.KerbalType?>(child, "kerbalType", null);
                kerb.trait = ConfigNodeUtil.ParseValue<string>(child, "trait", null);
            }
        }
    }
}
