using ContractConfigurator.Parameters;
using Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine;
using ContractConfigurator;
using RemoteTech;

namespace ContractConfigurator.RemoteTech
{
    public abstract class RemoteTechParameter : VesselParameter
    {
        private Dictionary<Vessel, float> stateChangeTime = new Dictionary<Vessel, float>();
        private const float REMOTE_TECH_GRACE_TIME = 0.0f;

        private Dictionary<Vessel, bool> loadedVessels = new Dictionary<Vessel, bool>();

        public RemoteTechParameter()
            : base()
        {
        }

        /// <summary>
        /// Wrapper for the return of VesselMeetsCondition.  Gives a grace period before cahnging
        /// parameters from completed to allow RemoteTech subsystems to load up all the data.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <param name="conditionMet">Normal return value of VesselMeetsCondition</param>
        /// <returns>The return value to use for VesselMeetsCondition</returns>
        protected bool VesselConditionWrapper(Vessel vessel, bool conditionMet)
        {
            if (!conditionMet)
            {
                ParameterState currentState = GetState(vessel);
                if (currentState == ParameterState.Complete)
                {
                    // Set the time the apparent state change happened
                    if (!stateChangeTime.ContainsKey(vessel))
                    {
                        stateChangeTime[vessel] = UnityEngine.Time.fixedTime;
                    }

                    // Check if we're within the grace period
                    if (UnityEngine.Time.fixedTime - stateChangeTime[vessel] < REMOTE_TECH_GRACE_TIME)
                    {
                        return true;
                    }
                }
            }

            // Remove when the condition is met or changing to unmet
            if (stateChangeTime.ContainsKey(vessel))
            {
                stateChangeTime.Remove(vessel);
            }

            return conditionMet;
        }

        /// <summary>
        /// Check for whether we are in a valid state to check the given vessel.  Checks if the
        /// RemoteTech logic is initialized.
        /// </summary>
        /// <param name="vessel">The vessel - ignored.</param>
        /// <returns>True only if RemoteTech is initialized.</returns>
        protected override bool CanCheckVesselMeetsCondition(Vessel vessel)
        {
            if (RTCore.Instance != null)
            {
                CheckVesselLoaded(vessel);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks and loads RemoteTech information for a vessel.
        /// </summary>
        /// <param name="vessel">The vessel to check.</param>
        protected void CheckVesselLoaded(Vessel vessel)
        {
            if (loadedVessels.ContainsKey(vessel))
            {
                return;
            }

            // Get the RT satellite
            var satellite = RTCore.Instance.Satellites[vessel.id];
            if (satellite == null)
            {
                return;
            }

            // If it's an unloaded vessel, RemoteTech isn't touching it.  So we need to force load
            if (!satellite.SignalProcessor.VesselLoaded)
            {
                IEnumerable<ISatellite> commandStations = RTCore.Instance.Satellites.FindCommandStations();
                RTCore.Instance.Network.FindPath(satellite, commandStations);
            }

            loadedVessels[vessel] = true;
        }
    }
}
