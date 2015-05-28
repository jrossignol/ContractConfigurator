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
    /// Behaviour for ulocking a tech node on contract/parameter completion/failure.
    /// Author: Klefenz
    /// </summary>
    public class UnlockTech : ContractBehaviour
    {
        protected List<string> techID;    //the tech to be unlocked

        public UnlockTech()
            : base()
        {
        }

        public UnlockTech(IEnumerable<string> techID)
        {
            this.techID = techID.ToList();
        }

        protected override void OnCompleted()
        {
            foreach (string tech in techID)
            {
                UnlockTechnology(tech);
            }
        }

        protected void UnlockTechnology(string techID)
        {
            ProtoTechNode ptd = new ProtoTechNode();
            ptd.state = RDTech.State.Available;
            ptd.techID = techID;
            ptd.scienceCost = 9999;

            if (HighLogic.CurrentGame.Parameters.Difficulty.BypassEntryPurchaseAfterResearch)
            {
                ptd.partsPurchased = PartLoader.Instance.parts.Where(p => p.TechRequired == techID).ToList();
            }
            else
            {
                ptd.partsPurchased = new List<AvailablePart>();
            }
                
            ResearchAndDevelopment.Instance.SetTechState(techID, ptd);
        }

        protected override void OnSave(ConfigNode configNode)
        {
            foreach (string tech in techID)
            {
                configNode.AddValue("techID", tech);
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            techID = ConfigNodeUtil.ParseValue<List<string>>(configNode, "techID");
        }
    }
}
