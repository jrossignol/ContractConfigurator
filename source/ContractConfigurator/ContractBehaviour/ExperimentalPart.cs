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
        protected AvailablePart part;
        protected bool add;
        protected bool remove;

        public ExperimentalPart()
            : base()
        {
        }

        public ExperimentalPart(AvailablePart part, bool add, bool remove)
        {
            this.part = part;
            this.add = add;
            this.remove = remove;
        }

        protected override void OnAccepted()
        {
            if (add)
            {
                ResearchAndDevelopment.AddExperimentalPart(part);
            }
        }

        protected override void OnCancelled()
        {
            if (add)
            {
                ResearchAndDevelopment.RemoveExperimentalPart(part);
            }
        }

        protected override void OnDeadlineExpired()
        {
            if (add)
            {
                ResearchAndDevelopment.RemoveExperimentalPart(part);
            }
        }

        protected override void OnFailed()
        {
            if (add)
            {
                ResearchAndDevelopment.RemoveExperimentalPart(part);
            }
        }

        protected override void OnCompleted()
        {
            if (remove)
            {
                ResearchAndDevelopment.RemoveExperimentalPart(part);
            }
        }

        protected override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("part", part.name);
            configNode.AddValue("add", add);
            configNode.AddValue("remove", remove);
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            part = ConfigNodeUtil.ParseValue<AvailablePart>(configNode, "part");
            add = ConfigNodeUtil.ParseValue<bool>(configNode, "add");
            remove = ConfigNodeUtil.ParseValue<bool>(configNode, "remove");
        }
    }
}
