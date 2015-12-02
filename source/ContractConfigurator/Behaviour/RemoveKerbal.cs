using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Class for removing Kerbals
    /// </summary>
    public class RemoveKerbalBehaviour : ContractBehaviour
    {
        protected List<Kerbal> kerbals;

        public RemoveKerbalBehaviour() {}

        public RemoveKerbalBehaviour(List<Kerbal> kerbals)
        {
            this.kerbals = kerbals;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            foreach (Kerbal kerbal in kerbals)
            {
                ConfigNode kerbalNode = new ConfigNode("KERBAL");
                node.AddNode(kerbalNode);

                kerbal.Save(kerbalNode);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            foreach (ConfigNode kerbalNode in node.GetNodes("KERBAL"))
            {
                kerbals.Add(Kerbal.Load(kerbalNode));
            }
        }

        protected override void OnCompleted()
        {
            RemoveKerbals();
        }

        protected override void OnCancelled()
        {
            RemoveKerbals();
        }

        protected override void OnDeadlineExpired()
        {
            RemoveKerbals();
        }

        protected override void OnDeclined()
        {
            RemoveKerbals();
        }

        protected override void OnGenerateFailed()
        {
            RemoveKerbals();
        }

        protected override void OnOfferExpired()
        {
            RemoveKerbals();
        }

        protected override void OnWithdrawn()
        {
            RemoveKerbals();
        }

        private void RemoveKerbals()
        {
            LoggingUtil.LogDebug(this, "Removing kerbals...");

            foreach (Kerbal kerbal in kerbals)
            {
                RemoveKerbal(kerbal);
            }
            kerbals.Clear();
        }

        public static void RemoveKerbal(Kerbal kerbal)
        {
            if (kerbal.pcm != null)
            {
                RemoveKerbal(kerbal.pcm);
                kerbal._pcm = null;
            }
        }

        public static void RemoveKerbal(ProtoCrewMember pcm)
        {
            LoggingUtil.LogVerbose(typeof(RemoveKerbalBehaviour), "    Removing " + pcm.name + "...");

            Vessel vessel = FlightGlobals.Vessels.Where(v => v.GetVesselCrew().Contains(pcm)).FirstOrDefault();
            if (vessel != null)
            {
                // If it's an EVA make them disappear...
                if (vessel.isEVA)
                {
                    FlightGlobals.Vessels.Remove(vessel);
                }
                else
                {
                    foreach (Part p in vessel.parts)
                    {
                        if (p.protoModuleCrew.Contains(pcm))
                        {
                            p.RemoveCrewmember(pcm);
                            break;
                        }
                    }
                }
            }

            // Remove the kerbal from the roster
            HighLogic.CurrentGame.CrewRoster.Remove(pcm.name);
        }
    }
}
