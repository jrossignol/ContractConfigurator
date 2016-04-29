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
    /// Simple mission timer implementation.
    /// </summary>
    public class MissionTimer : ContractConfiguratorParameter
    {
        public class ContractChecker
        {
            MissionTimer missionTimer;

            public ContractChecker(MissionTimer missionTimer)
            {
                this.missionTimer = missionTimer;

                GameEvents.Contract.onCompleted.Add(new EventData<Contract>.OnEvent(OnContractCompleted));
            }

            ~ContractChecker()
            {
                GameEvents.Contract.onCompleted.Remove(new EventData<Contract>.OnEvent(OnContractCompleted));
            }

            protected void OnContractCompleted(Contract contract)
            {
                missionTimer.OnContractCompleted(contract);
            }
        }

        public enum StartCriteria
        {
            CONTRACT_ACCEPTANCE,
            NEXT_LAUNCH,
            PARAMETER_COMPLETION
        }

        public enum EndCriteria
        {
            CONTRACT_COMPLETION,
            PARAMETER_COMPLETION
        }

        protected StartCriteria startCriteria;
        protected EndCriteria endCriteria;
        protected string startParameter = "";
        protected string endParameter = "";

        private double lastUpdate = 0.0;
        private double startTime = 0.0;
        private double endTime = 0.0;

        private ContractChecker checker;

        public MissionTimer()
            : base(null)
        {
        }

        public MissionTimer(StartCriteria startCriteria, EndCriteria endCriteria, string startParameter, string endParameter, string title)
            : base(title)
        {
            this.startCriteria = startCriteria;
            this.endCriteria = endCriteria;
            this.startParameter = startParameter;
            this.endParameter = endParameter;
            
            disableOnStateChange = false;
            checker = new ContractChecker(this);
        }

        protected override string GetParameterTitle()
        {
            double end = endTime == 0.0 ? Planetarium.GetUniversalTime() : endTime;
            string prefix = string.IsNullOrEmpty(title) ? "Mission Timer:" : title;

            string output;
            if (startTime == 0.0)
            {
                output = prefix + " 00:00:00";
            }
            else
            {
                output = prefix + " " + DurationUtil.StringValue(end - startTime, true, (endTime != 0.0));
            }

            return output;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("startCriteria", startCriteria);
            node.AddValue("endCriteria", endCriteria);
            if (!string.IsNullOrEmpty(startParameter))
            {
                node.AddValue("startParameter", startParameter);
            }
            if (!string.IsNullOrEmpty(endParameter))
            {
                node.AddValue("endParameter", endParameter);
            }
            node.AddValue("startTime", startTime);
            node.AddValue("endTime", endTime);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            startCriteria = ConfigNodeUtil.ParseValue<StartCriteria>(node, "startCriteria");
            endCriteria = ConfigNodeUtil.ParseValue<EndCriteria>(node, "endCriteria");
            startParameter = ConfigNodeUtil.ParseValue<string>(node, "startParameter", "");
            endParameter = ConfigNodeUtil.ParseValue<string>(node, "endParameter", "");
            startTime = Convert.ToDouble(node.GetValue("startTime"));
            endTime = Convert.ToDouble(node.GetValue("endTime"));

            if (Root.ContractState == Contract.State.Active || Root.ContractState == Contract.State.Offered)
            {
                checker = new ContractChecker(this);
            }
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccepted));
            GameEvents.onLaunch.Add(new EventData<EventReport>.OnEvent(OnLaunch));
            GameEvents.Contract.onParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            ContractConfigurator.OnParameterChange.Add(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccepted));
            GameEvents.onLaunch.Remove(new EventData<EventReport>.OnEvent(OnLaunch));
            GameEvents.Contract.onParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
            ContractConfigurator.OnParameterChange.Remove(new EventData<Contract, ContractParameter>.OnEvent(OnParameterChange));
        }

        protected void OnContractAccepted(Contract contract)
        {
            // Set the end time
            if (contract == Root && startCriteria == StartCriteria.CONTRACT_ACCEPTANCE)
            {
                SetStartTime();
            }
        }

        public void OnContractCompleted(Contract contract)
        {
            // Set the end time
            if (contract == Root && endCriteria == EndCriteria.CONTRACT_COMPLETION)
            {
                SetEndTime();
            }
        }

        protected void OnLaunch(EventReport er)
        {
            if (startCriteria == StartCriteria.NEXT_LAUNCH && startTime == 0.0)
            {
                SetStartTime();
            }
        }

        protected void OnParameterChange(Contract c, ContractParameter p)
        {
            if (c != Root)
            {
                return;
            }

            if (p.ID == startParameter && startCriteria == StartCriteria.PARAMETER_COMPLETION && startTime == 0.0)
            {
                SetStartTime();
            }
            if (p.ID == endParameter && endCriteria == EndCriteria.PARAMETER_COMPLETION && endTime == 0.0)
            {
                SetEndTime();
            }
        }

        private void SetStartTime()
        {
            startTime = Planetarium.GetUniversalTime();

            // Once the timer starts, we immediately go to the completed state
            SetState(ParameterState.Complete);
        }

        private void SetEndTime()
        {
            endTime = Planetarium.GetUniversalTime();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (startTime == 0.0 || endTime != 0.0)
            {
                return;
            }

            // Every time the clock ticks over, make an attempt to update the contract window
            // title.  We do this because otherwise the window will only ever read the title once,
            // so this is the only way to get our fancy timer to work.
            if (Planetarium.GetUniversalTime() - lastUpdate > 1.0f)
            {
                // Force a call to GetTitle to update the contracts app
                GetTitle();
            }
        }
    }
}
