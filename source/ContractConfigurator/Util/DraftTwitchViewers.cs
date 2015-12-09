using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Contracts;

namespace ContractConfigurator
{
    /// <summary>
    /// Monobehaviour for integration with Draft Twitch Viewers.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class DraftTwitchViewers : MonoBehaviour
    {
        private static DraftTwitchViewers Instance;
        private MethodInfo draftMethod = null;
        private MethodInfo saveMethod = null;
        private Queue<string> recentNames = new Queue<string>();
        private Queue<string> nameQueue = new Queue<string>();
        private HashSet<string> names = new HashSet<string>();
        private int routinesRunning = 0;

        private float nextAttempt = 0.0f;

        private const float failureDelay = 45;
        private const int draftLimit = 10;

        void Awake()
        {
            DontDestroyOnLoad(this);
            Instance = this;

            // Do a version check
            Assembly dtvAssembly = Util.Version.VerifyAssemblyVersion("DraftTwitchViewers", "2.0.1", true);
            if (dtvAssembly == null)
            {
                Destroy(this);
                return;
            }

            Type draftManager = dtvAssembly.GetTypes().Where(t => t.Name.Contains("DraftManager")).FirstOrDefault();
            if (draftManager == null)
            {
                LoggingUtil.LogError(this, "Couldn't get DraftManager from DraftTwitchViewers!");
                Destroy(this);
                return;
            }

            draftMethod = draftManager.GetMethods(BindingFlags.Public | BindingFlags.Static).
                Where(mi => mi.Name == "DraftKerbal").FirstOrDefault();
            if (draftMethod == null)
            {
                LoggingUtil.LogError(this, "Couldn't get DraftKerbal method from DraftTwitchViewers!");
                Destroy(this);
                return;
            }

            saveMethod = draftManager.GetMethods(BindingFlags.Public | BindingFlags.Static).
                Where(mi => mi.Name == "SaveSupressedDraft").FirstOrDefault();
            if (saveMethod == null)
            {
                LoggingUtil.LogError(this, "Couldn't get SaveSupressedDraft method from DraftTwitchViewers!");
                Destroy(this);
                return;
            }

            GameEvents.Contract.onAccepted.Add(new EventData<Contract>.OnEvent(OnContractAccepted));
            ContractPreLoader.OnInitializeValues.Add(new EventVoid.OnEvent(OnPreLoaderInitializeValues));
            ContractPreLoader.OnInitializeFail.Add(new EventVoid.OnEvent(OnPreLoaderInitializeFail));
        }

        void Destroy()
        {
            Instance = null;

            GameEvents.Contract.onAccepted.Remove(new EventData<Contract>.OnEvent(OnContractAccepted));
            ContractPreLoader.OnInitializeValues.Remove(new EventVoid.OnEvent(OnPreLoaderInitializeValues));
            ContractPreLoader.OnInitializeFail.Remove(new EventVoid.OnEvent(OnPreLoaderInitializeFail));
        }

        void Update()
        {
            if (HighLogic.LoadedScene != GameScenes.MAINMENU &&
                ContractSystem.Instance != null && 
                ContractPreLoader.Instance != null &&
                nameQueue.Count() + routinesRunning < draftLimit &&
                nextAttempt < Time.time)
            {
                // Start the coroutine
                object success = (Action<string>)(OnSuccess);
                object failure = (Action<string>)(OnFailure);
                IEnumerator enumerator = (IEnumerator)Instance.draftMethod.Invoke(null, new object[] { success, failure, false, true, "Any" });
                Instance.StartCoroutine(enumerator);

                routinesRunning++;
            }
        }

        public static string KerbalName(string defaultName)
        {
            LoggingUtil.LogVerbose(typeof(DraftTwitchViewers), "KerbalName()");

            if (Instance != null && Instance.nameQueue.Any())
            {
                string name = Instance.nameQueue.Dequeue();
                Instance.recentNames.Enqueue(name);

                LoggingUtil.LogVerbose(typeof(DraftTwitchViewers), "    " + name);
                return name;
            }
            else
            {
                return defaultName;
            }

        }

        public static void OnSuccess(string name)
        {
            LoggingUtil.LogVerbose(typeof(DraftTwitchViewers), "DraftTwitchViewers Success: " + name);

            // Queue the name if it is new
            Instance.routinesRunning--;
            if (Instance.IsUnique(name))
            {
                Instance.nameQueue.Enqueue(name);
                Instance.names.Add(name);
            }
        }

        public static void OnFailure(string errorMessage)
        {
            LoggingUtil.LogVerbose(typeof(DraftTwitchViewers), "DraftTwitchViewers Error: " + errorMessage);

            // Wait a bit before trying again
            Instance.routinesRunning--;
            Instance.nextAttempt = Time.time + failureDelay;
        }

        protected void OnContractAccepted(Contract c)
        {
            LoggingUtil.LogVerbose(typeof(DraftTwitchViewers), "Contract accepted, saving names..");
            IKerbalNameStorage storedNames = c as IKerbalNameStorage;
            if (storedNames != null)
            {
                foreach (string name in storedNames.KerbalNames().Distinct().Where(n => names.Contains(n)))
                {
                    LoggingUtil.LogVerbose(typeof(DraftTwitchViewers), "    saving '" + name + "'");
                    saveMethod.Invoke(null, new object[] { name });
                }
            }
        }

        protected bool IsUnique(string name)
        {
            // First check in the queue
            if (nameQueue.Contains(name))
            {
                return false;
            }

            // Check all active, offered and pending contracts for this name
            foreach (ConfiguredContract contract in ContractSystem.Instance.Contracts.OfType<ConfiguredContract>().
                Where(c=> c != null).Where(c =>
                c.ContractState == Contract.State.Active || c.ContractState == Contract.State.Offered).
                Union(ContractPreLoader.Instance.PendingContracts()))
            {
                foreach (string usedName in contract.KerbalNames())
                {
                    if (name == usedName)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        protected void OnPreLoaderInitializeValues()
        {
            recentNames.Clear();
        }

        protected void OnPreLoaderInitializeFail()
        {
            foreach (string name in recentNames)
            {
                nameQueue.Enqueue(name);
            }
            recentNames.Clear();
        }
    }
}
