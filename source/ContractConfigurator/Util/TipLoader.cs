using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace ContractConfigurator.Util
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class TipLoader : MonoBehaviour
    {
        public void Update()
        {
            // Delay until the game database has started loading (it's a short delay)
            if (LoadingScreen.Instance != null && GameDatabase.Instance != null && GameDatabase.Instance.root != null)
            {
                LoggingUtil.LogDebug(this, "Adding custom loading tips");

                // Add the Contract Configurator tip
                List<string> contractTips = new List<string>();
                contractTips.Add("Configuring Contracts...");

                // Read tips from root contract groups
                ConfigNode[] contractGroups = GameDatabase.Instance.GetConfigNodes("CONTRACT_GROUP");
                foreach (ConfigNode groupConfig in contractGroups)
                {
                    if (groupConfig.HasValue("tip"))
                    {
                        foreach (string tip in groupConfig.GetValues("tip"))
                        {
                            contractTips.Add(tip);
                        }
                    }
                }

                foreach (LoadingScreen.LoadingScreenState lss in LoadingScreen.Instance.Screens)
                {
                    // Append our custom tips
                    lss.tips = lss.tips.Union(contractTips).ToArray();
                }

                Destroy(this);
            }
        }
    }
}
