using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// BehaviourFactory wrapper for SpawnPassengers ContractBehaviour.
    /// </summary>
    public class SpawnPassengersFactory : BehaviourFactory
    {
        protected int count;
        protected List<string> passengerName;
        protected List<Kerbal> kerbals;
        protected ProtoCrewMember.Gender gender;
        protected ProtoCrewMember.KerbalType kerbalType;
        protected string experienceTrait;
        protected bool legacy = false;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            if (configNode.HasValue("passengerName"))
            {
                LoggingUtil.LogWarning(this, "The passengerName and gender attributes are obsolete since Contract Configurator 1.9.0, use kerbal instead.");
                valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "passengerName", x => passengerName = x, this, new List<string>());
                valid &= ConfigNodeUtil.ParseValue<ProtoCrewMember.Gender>(configNode, "gender", x => gender = x, this, Kerbal.RandomGender());
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "experienceTrait", x => experienceTrait = x, this, Kerbal.RandomExperienceTrait());

                legacy = true;
            }
            else
            {
                valid &= ConfigNodeUtil.ParseValue<List<Kerbal>>(configNode, "kerbal", x => kerbals = x, this, new List<Kerbal>());
                legacy = false;
            }

            valid &= ConfigNodeUtil.ParseValue<int>(configNode, "count", x => count = x, this, 1, x => Validation.GE(x, 1));
            valid &= ConfigNodeUtil.ParseValue<ProtoCrewMember.KerbalType>(configNode, "kerbalType", x => kerbalType = x, this, ProtoCrewMember.KerbalType.Tourist);

            return valid;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            // Set legacy values
            if (legacy)
            {
                kerbals = passengerName.Select(name => new Kerbal(gender, name, experienceTrait)).ToList();
            }

            // Set the kerbal type
            foreach (Kerbal kerbal in kerbals)
            {
                kerbal.kerbalType = kerbalType;
            }

            return new SpawnPassengers(kerbals, count);
        }
    }
}
