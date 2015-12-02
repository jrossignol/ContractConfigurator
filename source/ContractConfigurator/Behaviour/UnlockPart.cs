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
    /// Behaviour for unlocking a part on contract/parameter completion/failure.
    /// </summary>
    public class UnlockPart : ContractBehaviour
    {
        protected List<AvailablePart> parts;
        protected bool unlockTech;

        public UnlockPart()
            : base()
        {
        }

        public UnlockPart(IEnumerable<AvailablePart> parts, bool unlockTech)
        {
            this.parts = parts.ToList();
            this.unlockTech = unlockTech;
        }

        protected override void OnCompleted()
        {
            foreach (AvailablePart part in parts)
            {
                DoUnlock(part);
            }
        }

        protected void DoUnlock(AvailablePart part)
        {
            ProtoTechNode ptn = ResearchAndDevelopment.Instance.GetTechState(part.TechRequired);

            // The tech may be null - we need to create it
            if (ptn == null)
            {
                ptn = new ProtoTechNode();
                ptn.state = RDTech.State.Unavailable;
                ptn.techID = part.TechRequired;
                ptn.scienceCost = 9999; // ignored
            }

            if (unlockTech)
            {
                ptn.state = RDTech.State.Available;
            }

            if (!HighLogic.CurrentGame.Parameters.Difficulty.BypassEntryPurchaseAfterResearch && !ptn.partsPurchased.Contains(part))
            {
                ptn.partsPurchased.Add(part);
            }
            else
            {
                ptn.partsPurchased = new List<AvailablePart>();
            }

            ResearchAndDevelopment.Instance.SetTechState(part.TechRequired, ptn);
        }

        protected override void OnSave(ConfigNode configNode)
        {
            foreach (AvailablePart part in parts)
            {
                configNode.AddValue("part", part.name);
            }
            configNode.AddValue("unlockTech", unlockTech);
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            parts = ConfigNodeUtil.ParseValue<List<AvailablePart>>(configNode, "part");
            unlockTech = ConfigNodeUtil.ParseValue<bool>(configNode, "unlockTech");
        }
    }
}
