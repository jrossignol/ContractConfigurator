using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Behaviour for destroying one or more vessels.
    /// </summary>
    public class DestroyVessel : TriggeredBehaviour
    {
        class VesselDestroyer : MonoBehaviour
        {
            const float delay = 2.0f;
            float triggerTime;

            IEnumerable<Vessel> vessels = new List<Vessel>();

            void Start()
            {
            }

            public void AddVessels(IEnumerable<Vessel> newVessels)
            {
                triggerTime = Time.fixedTime + delay;
                vessels = vessels.Union(newVessels);
            }

            void Update()
            {
                if (Time.fixedTime > triggerTime)
                {
                    // Destroy the vessels
                    foreach (Vessel vessel in vessels)
                    {
                        if (vessel != null)
                        {
                            vessel.Die();
                        }
                    }

                    // This works around a KSP bug in KSCVesselMarker where the dead vessels appear at KSC and can be recovered.
                    KSCVesselMarkers markers = UnityEngine.Object.FindObjectOfType<KSCVesselMarkers>();
                    MethodInfo refresh = typeof(KSCVesselMarkers).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).
                        Where(m => m.Name == "RefreshMarkers").First();
                    refresh.Invoke(markers, new object[] {});

                    // All done
                    Destroy(this);
                }
            }
        }

        private List<string> vessels;

        public DestroyVessel()
        {
        }

        public DestroyVessel(State onState, List<string> vessels, List<string> parameter)
            : base(onState, parameter)
        {
            this.vessels = vessels;
        }

        protected override void TriggerAction()
        {
            // Set the vessels to be destroyed after a delay
            if (MapView.MapCamera.gameObject.GetComponent<VesselDestroyer>() == null)
            {
                MapView.MapCamera.gameObject.AddComponent<VesselDestroyer>();
            }
            MapView.MapCamera.gameObject.GetComponent<VesselDestroyer>().AddVessels(vessels.Select(
                v => ContractVesselTracker.Instance.GetAssociatedVessel(v)));
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);
            vessels = ConfigNodeUtil.ParseValue<List<string>>(configNode, "vessel", new List<string>());
        }

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);
            foreach (string v in vessels)
            {
                configNode.AddValue("vessel", v);
            }
        }
    }
}
