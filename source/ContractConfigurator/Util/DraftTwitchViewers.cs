using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

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
        private Queue<string> nameQueue = new Queue<string>();
        private int routinesRunning = 0;

        private float nextAttempt = 0.0f;

        private const float failureDelay = 45;
        private const int draftLimit = 5;

        void Awake()
        {
            DontDestroyOnLoad(this);
            Instance = this;

            // Do a version check
            Assembly dtvAssembly = Util.Version.VerifyAssemblyVersion("DraftTwitchViewers", "2.0.0", true);
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

            GameEvents.onGameSceneLoadRequested.Add(new EventData<GameScenes>.OnEvent(OnGameSceneLoad));
        }

        void Destroy()
        {
            Instance = null;

            GameEvents.onGameSceneLoadRequested.Remove(new EventData<GameScenes>.OnEvent(OnGameSceneLoad));
        }

        void OnGameSceneLoad(GameScenes scene)
        {
            Debug.Log("CC.DTV: Loading scene: " + scene);
        }

        void Update()
        {
            if (HighLogic.LoadedScene != GameScenes.MAINMENU && nameQueue.Count() + routinesRunning < draftLimit && nextAttempt < Time.time)
            {
                // Start the coroutine
                object success = (Action<string>)(OnSuccess);
                object failure = (Action<string>)(OnFailure);
                IEnumerator enumerator = (IEnumerator)Instance.draftMethod.Invoke(null, new object[] { success, failure, false, "Any" });
                Instance.StartCoroutine(enumerator);

                routinesRunning++;
            }
        }

        public static string KerbalName(string defaultName)
        {
            Debug.Log("DraftTwitchViewers.KerbalName");
            if (Instance != null && Instance.nameQueue.Any())
            {
                Debug.Log("    " + Instance.nameQueue.Peek());
                return Instance.nameQueue.Dequeue();
            }
            else
            {
                Debug.Log("    " + defaultName + " (default)");
                return defaultName;
            }

        }

        public static void OnSuccess(string name)
        {
            Debug.Log("DTV Success: " + name);

            // Queue the name
            Instance.routinesRunning--;
            Instance.nameQueue.Enqueue(name);
        }

        public static void OnFailure(string errorMessage)
        {
            Debug.Log("DTV Error: " + errorMessage);

            // Wait a bit before trying again
            Instance.routinesRunning--;
            Instance.nextAttempt = Time.time + failureDelay;
        }
    }
}
