using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Custom implementation of the KerbalDeaths parameter
    /// </summary>
    public class KerbalDeathsCustom : ContractConfiguratorParameter, IKerbalNameStorage
    {
        int countMax;
        int count = 0;
        protected List<Kerbal> kerbals = new List<Kerbal>();

        public KerbalDeathsCustom()
            : base()
        {
        }

        public KerbalDeathsCustom(int countMax, IEnumerable<Kerbal> kerbals, string title)
            : base(title)
        {
            this.countMax = countMax;
            this.kerbals = kerbals.ToList();
        
            CreateDelegates();
        }

        protected override string GetParameterTitle()
        {
            string output = null;
            if (string.IsNullOrEmpty(title))
            {
                if (!kerbals.Any())
                {
                    if (countMax == 1)
                    {
                        output = "Kill no Kerbals";
                    }
                    else
                    {
                        output = "Kill no more than " + countMax + " Kerbals";
                    }
                }
                else
                {
                    output = "Do not kill";
                    if (state != ParameterState.Incomplete || ParameterCount == 1)
                    {
                        if (ParameterCount == 1)
                        {
                            hideChildren = true;
                        }

                        output += ": " + ParameterDelegate<ProtoCrewMember>.GetDelegateText(this);
                    }
                }
            }
            else
            {
                output = title;
            }
            return output;
        }

        protected void CreateDelegates()
        {
            // Validate specific kerbals
            foreach (Kerbal kerbal in kerbals)
            {
                AddParameter(new ParameterDelegate<ProtoCrewMember>(kerbal.name, pcm => true));
            }
        }

        protected override void OnRegister()
        {
            GameEvents.onCrewKilled.Add(new EventData<EventReport>.OnEvent(OnCrewKilled));
        }

        protected override void OnUnregister()
        {
            GameEvents.onCrewKilled.Remove(new EventData<EventReport>.OnEvent(OnCrewKilled));
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("countMax", countMax);
            node.AddValue("count", count);

            foreach (Kerbal kerbal in kerbals)
            {
                ConfigNode kerbalNode = new ConfigNode("KERBAL");
                node.AddNode(kerbalNode);

                kerbal.Save(kerbalNode);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            try
            {
                countMax = ConfigNodeUtil.ParseValue<int>(node, "countMax");
                count = ConfigNodeUtil.ParseValue<int>(node, "count");

                foreach (ConfigNode kerbalNode in node.GetNodes("KERBAL"))
                {
                    kerbals.Add(Kerbal.Load(kerbalNode));
                }

                CreateDelegates();
            }
            finally
            {
                ParameterDelegate<ProtoCrewMember>.OnDelegateContainerLoad(node);
            }
        }

        private void OnCrewKilled(EventReport report)
        {
            if (report.eventType != FlightEvents.CREW_KILLED)
            {
                return;
            }

            LoggingUtil.LogVerbose(this, "OnCrewKilled");
            LoggingUtil.LogVerbose(this, "    report.sender = " + report.sender);

            if (kerbals.Any())
            {
                if (kerbals.Any(k => k.name == report.sender))
                {
                    SetState(ParameterState.Failed);
                }
            }
            else if (++count >= countMax) 
            {
                SetState(ParameterState.Failed);
            }

        }

        public IEnumerable<string> KerbalNames()
        {
            foreach (Kerbal kerbal in kerbals)
            {
                yield return kerbal.name;
            }
        }
    }
}
