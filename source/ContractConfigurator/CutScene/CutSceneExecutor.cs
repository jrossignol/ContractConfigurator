using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ContractConfigurator.CutScene
{
    /// <summary>
    /// This will run the cutscenes.
    /// </summary>
    public class CutSceneExecutor : MonoBehaviour
    {
        private enum State
        {
            IDLE,
            STARTING_CUTSCENE_FADE_OUT,
            STARTING_CUTSCENE_FADE_IN,
            IN_CUTSCENE,
            ENDING_CUTSCENE_FADE_OUT,
            ENDING_CUTSCENE_FADE_IN
        }
        public CutSceneDefinition cutSceneDefinition;

        private CutSceneAction currentAction = null;
        private int currentIndex;
        private State state = State.IDLE;

        private const float FADE_TIME = 1.75f;
        private const float EXTRA_TIME = 0.25f;
        private float fadeTimer;
        private Rect fadeRect;
        private Texture2D fadeTexture;

        private bool reshowUI = true;

        public bool CanDestroy
        {
            get
            {
                return state == State.IDLE;
            }
        }

        public void Start()
        {
            GameEvents.onGameSceneSwitchRequested.Add(new EventData<GameEvents.FromToAction<GameScenes, GameScenes>>.OnEvent(GameSceneSwitch));
        }

        public void OnDestroy()
        {
            GameEvents.onGameSceneSwitchRequested.Remove(new EventData<GameEvents.FromToAction<GameScenes, GameScenes>>.OnEvent(GameSceneSwitch));
            if (state != State.IDLE)
            {
                EnableInputs();
                if (currentAction != null)
                {
                    currentAction.OnDestroy();
                }
            }
        }

        void GameSceneSwitch(GameEvents.FromToAction<GameScenes, GameScenes> fta)
        {
            reshowUI = false;
            Destroy(this);
        }

        void FixedUpdate()
        {
            if (currentAction != null)
            {
                currentAction.FixedUpdate();
            }
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            // Draw the fade in/out
            if (state == State.STARTING_CUTSCENE_FADE_IN || state == State.STARTING_CUTSCENE_FADE_OUT ||
                state == State.ENDING_CUTSCENE_FADE_IN || state == State.ENDING_CUTSCENE_FADE_OUT ||
                state == State.IN_CUTSCENE && fadeTimer > 0.0)
            {
                Graphics.DrawTexture(fadeRect, fadeTexture, new Rect(0.0f, 0.0f, 1f, 1f), 0, 0, 0, 0,
                    Color.Lerp(Color.black, Color.clear, (FADE_TIME - fadeTimer) / FADE_TIME));
            }

            // Draw the letterboxing
            if (state == State.STARTING_CUTSCENE_FADE_IN || state == State.IN_CUTSCENE || state == State.ENDING_CUTSCENE_FADE_OUT)
            {
                float chopSize = Screen.height - Screen.width / cutSceneDefinition.aspectRatio;
                if (chopSize > 0)
                {
                    Rect topRect = new Rect(0, 0, Screen.width, chopSize / 2.0f);
                    Rect bottomRect = new Rect(0, Screen.height - chopSize / 2.0f, Screen.width, chopSize / 2.0f);

                    Graphics.DrawTexture(topRect, fadeTexture);
                    Graphics.DrawTexture(bottomRect, fadeTexture);
                }
            }
        }

        void Update()
        {
            if (state == State.IDLE)
            {
                return;
            }

            if (state == State.STARTING_CUTSCENE_FADE_OUT)
            {
                fadeTimer += Time.deltaTime;
                if (fadeTimer > FADE_TIME + EXTRA_TIME)
                {
                    state = State.IN_CUTSCENE;
                    fadeTimer = FADE_TIME;

                    // Switch to the first cutscene camera
                    cutSceneDefinition.cameras.First().MakeActive();

                    // Invoke the first action
                    currentIndex = 0;
                    currentAction = cutSceneDefinition.actions.First();
                    currentAction.InvokeAction();
                }

                return;
            }

            if (state == State.IN_CUTSCENE)
            {
                fadeTimer -= Time.deltaTime;
            }

            if (state == State.ENDING_CUTSCENE_FADE_OUT)
            {
                fadeTimer += Time.deltaTime;
                if (fadeTimer > FADE_TIME + EXTRA_TIME)
                {
                    state = State.ENDING_CUTSCENE_FADE_IN;
                    fadeTimer = FADE_TIME;
                }

                return;
            }

            if (state == State.ENDING_CUTSCENE_FADE_IN)
            {
                fadeTimer -= Time.deltaTime;
                if (fadeTimer < 0)
                {
                    Destroy(gameObject);
                }

                return;
            }

            if (state != State.IN_CUTSCENE)
            {
                return;
            }

            while (currentAction != null)
            {
                currentAction.Update();

                // Move to the next action if necessary
                if (currentAction.async || currentAction.ReadyForNextAction())
                {
                    currentAction.OnDestroy();

                    if (++currentIndex < cutSceneDefinition.actions.Count)
                    {
                        Debug.Log("CutScene: Invoking action " + currentAction);
                        currentAction = cutSceneDefinition.actions[currentIndex];
                        currentAction.InvokeAction();
                    }
                    else
                    {
                        currentAction = null;
                        state = State.ENDING_CUTSCENE_FADE_OUT;
                        fadeTimer = 0;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public void ExecuteCutScene()
        {
            LoggingUtil.LogVerbose(this, "Execute cut scene '" + cutSceneDefinition.name + "'");

            state = State.STARTING_CUTSCENE_FADE_OUT;
            fadeTimer = 0.0f;
            fadeRect = new Rect(0, 0, Screen.width, Screen.height);
            fadeTexture = new Texture2D(1, 1);
            fadeTexture.SetPixel(0, 0, Color.black);
            fadeTexture.Apply();

            DisableInputs();
        }

        public void DisableInputs()
        {
            // Hide the UI
            GameEvents.onHideUI.Fire();

            // Prevent inputs
            ControlTypes locks = ControlTypes.All ^ ControlTypes.PAUSE ^ ControlTypes.QUICKLOAD;
            InputLockManager.SetControlLock(locks, "CutSceneExecutor");
        }

        public void EnableInputs()
        {
            // Show the UI
            if (reshowUI)
            {
                GameEvents.onShowUI.Fire();
            }

            // Re-enable inputs
            InputLockManager.RemoveControlLock("CutSceneExecutor");
        }
    }

}
