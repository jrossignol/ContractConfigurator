using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using KSP.Localization;
using KSP.UI.Screens;
using Expansions;
using ContractConfigurator.Util;

namespace ContractConfigurator
{
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames |
        ScenarioCreationOptions.AddToExistingScienceSandboxGames | ScenarioCreationOptions.AddToNewScienceSandboxGames,
        GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER)]
    public class ScienceReporter : ScenarioModule
    {
        static FieldInfo messageListField = typeof(MessageSystem).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(fi => fi.FieldType == typeof(List<MessageSystemButton>)).First();
        static List<MessageSystemButton> messageList = null;
        static string deployedScienceTag = null;
        static string deployedScienceMessageTitle = null;

        int lastMessageCount = 0;
        List<ScienceSubject> recentScience = new List<ScienceSubject>();
        List<ScienceSubject> trackedSubjects = new List<ScienceSubject>();
        List<ScreenMessage> screenMessagesToRemove = new List<ScreenMessage>();

        public override void OnAwake()
        {
            base.OnAwake();

            // Right now we only look at deployed science, which is a Breaking Ground feature
            if (!ExpansionsLoader.IsExpansionInstalled("Serenity"))
            {
                Destroy(this);
            }
            else
            {
                deployedScienceTag = Localizer.GetStringByTag("#autoLOC_8002254");
                deployedScienceMessageTitle = Localizer.GetStringByTag("#cc.science.deployedScienceReport");
            }
        }

        void Start()
        {
            DontDestroyOnLoad(this);

            GameEvents.onGUIMessageSystemReady.Add(new EventVoid.OnEvent(OnMessageSystemReady));
            GameEvents.OnScienceRecieved.Add(new EventData<float, ScienceSubject, ProtoVessel, bool>.OnEvent(OnScienceReceived));
        }

        void Destroy()
        {
            GameEvents.onGUIMessageSystemReady.Remove(new EventVoid.OnEvent(OnMessageSystemReady));
            GameEvents.OnScienceRecieved.Remove(new EventData<float, ScienceSubject, ProtoVessel, bool>.OnEvent(OnScienceReceived));
        }

        void Update()
        {
            // Detect if there are any new messages
            if (MessageSystem.Instance != null)
            {
                // Derive the internal message list
                if (messageList == null)
                {
                    messageList = (List<MessageSystemButton>)messageListField.GetValue(MessageSystem.Instance);
                }

                // Go through new messages
                if (messageList.Count > lastMessageCount)
                {
                    for (int i = (messageList.Count - lastMessageCount); i-- > 0;)
                    {
                        LoggingUtil.LogVerbose(this, "Message list count = {0}, message {3} = {1} + {2}", messageList.Count, messageList[i].message.message, messageList[i].message.messageTitle, i);
                        if (LocalizationUtil.IsLocalizedString(messageList[i].message.message, deployedScienceTag))
                        {
                            // Pull out the parameters
                            IList<string> parameters = LocalizationUtil.UnLocalizeString(messageList[i].message.message, deployedScienceTag);

                            // Identify the subject
                            ScienceSubject subject = null;
                            foreach (ScienceSubject sub in recentScience)
                            {
                                if (sub.title == parameters[0])
                                {
                                    subject = sub;
                                    break;
                                }
                            }

                            // Subject identified
                            if (subject != null)
                            {
                                LoggingUtil.LogVerbose(this, "Subject identified as {0}", subject.id);

                                // Delete the old message
                                MessageSystem.Instance.DiscardMessage(messageList[i].message.button);

                                // Check for an existing summary level message
                                MessageSystem.Message message = MessageSystem.Instance.FindMessages(m => m.messageTitle == deployedScienceMessageTitle).FirstOrDefault();
                                if (message != null)
                                {
                                    message.IsRead = false;
                                    trackedSubjects.Clear();
                                }
                                trackedSubjects.Add(subject);
                                trackedSubjects.Sort((ss1, ss2) => string.Compare(ss1.title, ss2.title));

                                StringBuilder sb = StringBuilderCache.Acquire();
                                sb.Append(string.Format("<b>{0}</b>:\n", deployedScienceMessageTitle));

                                foreach (ScienceSubject s in trackedSubjects)
                                {
                                    sb.Append(string.Format("    {0}: <color=#6DCFF6><sprite=\"CurrencySpriteAsset\" name=\"Science\" tint=1> {1}</color> / <color=#6DCFF6><sprite=\"CurrencySpriteAsset\" name=\"Science\" tint=1> {2}</color>\n",
                                        s.title, s.science.ToString("F1"), s.scienceCap.ToString("F1")));
                                }

                                if (message != null)
                                {
                                    message.message = sb.ToStringAndRelease();
                                }
                                else
                                {
                                    MessageSystem.Instance.AddMessage(new MessageSystem.Message(deployedScienceMessageTitle, sb.ToStringAndRelease(),
                                        MessageSystemButton.MessageButtonColor.BLUE, MessageSystemButton.ButtonIcons.ALERT));
                                }
                            }
                            else
                            {
                                LoggingUtil.LogWarning(this, "Couldn't identify subject for deployed experiment with title '{0}'", parameters[0]);
                            }
                        }
                    }

                    recentScience.Clear();
                }
                lastMessageCount = messageList.Count;

                // Check for active screen messages
                screenMessagesToRemove.Clear();
                foreach (ScreenMessage message in ScreenMessages.Instance.ActiveMessages)
                {
                    if (LocalizationUtil.IsLocalizedString(message.message, deployedScienceTag))
                    {
                        screenMessagesToRemove.Add(message);
                    }
                }

                // Remove the messages
                foreach (ScreenMessage message in screenMessagesToRemove)
                {
                    ScreenMessages.RemoveMessage(message);
                }
            }
        }

        private void OnScienceReceived(float science, ScienceSubject subject, ProtoVessel protoVessel, bool reverseEngineered)
        {
            recentScience.Add(subject);
        }

        private void OnMessageSystemReady()
        {
            messageList = null;
            lastMessageCount = 0;
        }

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                base.OnLoad(node);

                foreach (string id in node.GetValuesList("subject"))
                {
                    ScienceSubject s = ResearchAndDevelopment.GetSubjectByID(id);
                    if (s != null)
                    {
                        trackedSubjects.Add(s);
                    }
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Error loading ScienceReporter from persistance file!");
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.SCENARIO_MODULE_LOAD, e, "ScienceReporter");
            }
        }

        public override void OnSave(ConfigNode node)
        {
            try
            {
                base.OnSave(node);

                foreach (ScienceSubject s in trackedSubjects)
                {
                    node.AddValue("subject", s.id);
                }
            }
            catch (Exception e)
            {
                LoggingUtil.LogError(this, "Error saving ScienceReporter to persistance file!");
                LoggingUtil.LogException(e);
                ExceptionLogWindow.DisplayFatalException(ExceptionLogWindow.ExceptionSituation.SCENARIO_MODULE_SAVE, e, "ScienceReporter");
            }
        }
    }
}
