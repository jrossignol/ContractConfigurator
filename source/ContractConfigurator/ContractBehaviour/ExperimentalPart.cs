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
    /// Behaviour for adding/removing an experimental part.
    /// </summary>
    public class ExperimentalPart : ContractBehaviour
    {
        protected List<AvailablePart> parts;
        protected bool add;
        protected bool remove;

        public ExperimentalPart()
            : base()
        {
        }

        public ExperimentalPart(List<AvailablePart> parts, bool add, bool remove)
        {
            this.parts = parts;
            this.add = add;
            this.remove = remove;
        }

        protected override void OnAccepted()
        {
            if (add)
            {
                foreach (AvailablePart part in parts)
                {
                    ResearchAndDevelopment.AddExperimentalPart(part);
                }
            }
        }

        protected override void OnCancelled()
        {
            if (add)
            {
                foreach (AvailablePart part in parts)
                {
                    ResearchAndDevelopment.RemoveExperimentalPart(part);
                }
            }
        }

        protected override void OnDeadlineExpired()
        {
            if (add)
            {
                foreach (AvailablePart part in parts)
                {
                    ResearchAndDevelopment.RemoveExperimentalPart(part);
                }
            }
        }

        protected override void OnFailed()
        {
            if (add)
            {
                foreach (AvailablePart part in parts)
                {
                    ResearchAndDevelopment.RemoveExperimentalPart(part);
                }
            }
        }

        protected override void OnCompleted()
        {
            if (remove)
            {
                foreach (AvailablePart part in parts)
                {
                    ResearchAndDevelopment.RemoveExperimentalPart(part);
                }
            }
        }

        protected override void OnSave(ConfigNode configNode)
        {
            foreach (AvailablePart part in parts)
            {
                configNode.AddValue("part", part.name);
            }
            configNode.AddValue("add", add);
            configNode.AddValue("remove", remove);
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            parts = ConfigNodeUtil.ParseValue<List<AvailablePart>>(configNode, "part");
            add = ConfigNodeUtil.ParseValue<bool>(configNode, "add");
            remove = ConfigNodeUtil.ParseValue<bool>(configNode, "remove");
        }
    }
}
