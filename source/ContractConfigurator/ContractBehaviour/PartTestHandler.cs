using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Behaviour for awarding experience to a crew. 
    /// </summary>
    public class PartTestHandler : ContractBehaviour
    {
        public PartTestHandler()
        {
        }

        protected override void OnRegister()
        {
            base.OnRegister();

            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onFlightReady.Add(new EventVoid.OnEvent(OnFlightReady));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();

            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(OnVesselChange));
            GameEvents.onFlightReady.Remove(new EventVoid.OnEvent(OnFlightReady));
        }

        private void OnFlightReady()
        {
            LoggingUtil.LogVerbose(this, "OnFlightReady");

            HandleVessel(FlightGlobals.ActiveVessel);
        }

        private void OnVesselChange(Vessel target)
        {
            LoggingUtil.LogVerbose(this, "OnVesselChange");

            HandleVessel(target);
        }

        private void HandleVessel(Vessel target)
        {
            if (target == null)
            {
                return;
            }

            IEnumerable<string> partsToTest = PartsToTest(contract).Select(p => p.title);

            foreach (Part part in target.parts.Where(p => partsToTest.Contains(p.partInfo.title)))
            {
                foreach (PartModule pm in part.Modules)
                {
                    ModuleTestSubject mts = pm as ModuleTestSubject;
                    if (mts != null)
                    {
                        mts.isTestSubject = true;

                        mts.Events["RunTestEvent"].active = true;
                    }
                }
            }
        }

        protected override void OnParameterStateChange(ContractParameter param)
        {
            LoggingUtil.LogVerbose(this, "OnParameterStateChange");

            PartTest partTestParam = param as PartTest;
            if (partTestParam != null)
            {
                HandlePartChange(FlightGlobals.ActiveVessel, partTestParam.tgtPartInfo);
            }
        }

        protected override void OnFinished()
        {
            LoggingUtil.LogVerbose(this, "OnFinished");

            foreach (AvailablePart part in AllParts(contract))
            {
                HandlePartChange(FlightGlobals.ActiveVessel, part);
            }
        }

        protected void HandlePartChange(Vessel targetVessel, AvailablePart targetPart)
        {
            if (targetVessel == null)
            {
                return;
            }

            IEnumerable<string> partsToTest = ContractSystem.Instance.Contracts.
                Where(c => c.ContractState == Contract.State.Active).
                SelectMany(PartsToTest).
                Select(p => p.title);
            bool isActive = partsToTest.Contains(targetPart.title);
            LoggingUtil.LogVerbose(this, "   part " + targetPart.title + ", active = " + isActive);

            foreach (Part part in targetVessel.parts.Where(p => partsToTest.Contains(p.partInfo.title)))
            {
                foreach (PartModule pm in part.Modules)
                {
                    ModuleTestSubject mts = pm as ModuleTestSubject;
                    if (mts != null)
                    {
                        mts.isTestSubject = isActive;
                        mts.Events["RunTestEvent"].active = isActive;
                    }
                }
            }
        }

        private IEnumerable<AvailablePart> AllParts(Contract c)
        {
            foreach (PartTest pt in c.GetAllDescendents().Select(p => p as PartTest).Where(p => p != null))
            {
                yield return pt.tgtPartInfo;
            }
        }

        private IEnumerable<AvailablePart> PartsToTest(Contract c)
        {
            foreach (PartTest pt in c.GetAllDescendents().Select(p => p as PartTest).Where(p => p != null && p.State == ParameterState.Incomplete))
            {
                yield return pt.tgtPartInfo;
            }
        }
    }
}
