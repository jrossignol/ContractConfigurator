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
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class RemoteTechAssistant : MonoBehaviour
    {
        public static EventData<VesselSatellite> OnRemoteTechUpdate = new EventData<VesselSatellite>("OnRemoteTechUpdate");

        private const int UNLOADED_REFRESH_CYCLE = 4; // Refresh every 4 cycles for unloaded ships
        private const int REFRESH_TICKS = 50;
        private int mTick = -1; // Start one tick behind RT
        private int mTickIndex;
        private int mCycle;

        void Start()
        {
            LoggingUtil.LogVerbose(this, "Attempting to start RemoteTechAssistant.");
            // Validate scenes
            if (HighLogic.LoadedScene != GameScenes.FLIGHT && 
                HighLogic.LoadedScene != GameScenes.SPACECENTER && 
                HighLogic.LoadedScene != GameScenes.TRACKSTATION)
            {
                LoggingUtil.LogVerbose(this, "Destroying RemoteTechAssistant.");
                Destroy(this);
            }
            else
            {
                LoggingUtil.LogVerbose(this, "Not destroying RemoteTechAssistant.");
            }
        }

        void FixedUpdate()
        {
            LoggingUtil.LogVerbose(this, "Updating RemoteTech satellites.");

            // RemoteTech not yet initialized
            if (RTCore.Instance == null)
            {
                return;
            }

            // Catch up to our start position
            if (mTick < 0)
            {
                mTick++;
                return;
            }

            int i = mTickIndex;

            // Modified logic from RemoteTech.NetworkManager.OnPhysicsUpdate
            var count = RTCore.Instance.Satellites.Count;
            if (count == 0) return;
            int baseline = (count / REFRESH_TICKS);
            int takeCount = baseline + (((mTick++ % REFRESH_TICKS) < (count - baseline * REFRESH_TICKS)) ? 1 : 0);
            IEnumerable<ISatellite> commandStations = RTCore.Instance.Satellites.FindCommandStations();
            foreach (VesselSatellite s in RTCore.Instance.Satellites.Concat(RTCore.Instance.Satellites).Skip(mTickIndex).Take(takeCount))
            {
                // Increment counter for cycling
                i++;

                // RemoteTech didn't do the update - we do it
                if (!s.SignalProcessor.VesselLoaded && HighLogic.LoadedScene != GameScenes.TRACKSTATION)
                {
                    // Each vessel gets refreshed every UNLOADED_REFRESH_CYCLE passes - but spread
                    // out the vessels across the cycles.
                    if ((i + mCycle) % UNLOADED_REFRESH_CYCLE == 0)
                    {
                        RTCore.Instance.Network.FindPath(s, commandStations);
                    }
                    else
                    {
                        // Don't fire the event
                        continue;
                    }
                }

                // Fire our event
                LoggingUtil.LogVerbose(this, "OnRemoteTechUpdate: " + s);
                OnRemoteTechUpdate.Fire(s);
            }
            mTickIndex += takeCount;
            if (mTickIndex >= RTCore.Instance.Satellites.Count)
            {
                mCycle++;
            }
            mTickIndex = mTickIndex % RTCore.Instance.Satellites.Count;
        }

    }
}
