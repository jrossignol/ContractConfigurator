using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Contracts;
using Contracts.Agents;
using KSP.UI;
using KSP.UI.Screens;

namespace ContractConfigurator.Util
{
    /// <summary>
    /// Special MonoBehaviour to replace portions of the stock mission control UI.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class MissionControlUI : MonoBehaviour
    {
        public static string RequirementHighlightColor = "F9F9F6";

        public class GroupContainer
        {
            public ContractGroup group;
            public Agent agent;
            public bool expanded = false;
            public List<GroupContainer> childGroups = new List<GroupContainer>();
            public List<ContractContainer> childContracts = new List<ContractContainer>();
            public MCListItem mcListItem;

            public GroupContainer(ContractGroup group)
            {
                this.group = group;

                // Set the agent from the group
                if (group.agent != null)
                {
                    agent = group.agent;
                }
                // Fallback to trying to find the most appropriate agent
                else
                {
                    ContractType contractType = ContractType.AllValidContractTypes.Where(ct => ct != null && ct.group == group).FirstOrDefault();
                    agent = contractType != null ? contractType.agent : null;

                }
            }

            public void Toggle()
            {
                expanded = !expanded;
                SetState(expanded);

                // Reset background images
                UIRadioButton radioButton = mcListItem.GetComponent<UIRadioButton>();
                radioButton.stateTrue.normal = radioButton.stateTrue.highlight = radioButton.stateTrue.pressed = radioButton.stateTrue.disabled = (expanded ? groupExpandedActive : groupUnexpandedActive);
                radioButton.stateFalse.normal = radioButton.stateFalse.highlight = radioButton.stateFalse.pressed = radioButton.stateFalse.disabled = (expanded ? groupExpandedInactive : groupUnexpandedInactive);
                mcListItem.GetComponent<Image>().sprite = expanded ? (radioButton.CurrentState == UIRadioButton.State.True ? groupExpandedActive : groupExpandedInactive) :
                    (radioButton.CurrentState == UIRadioButton.State.True ? groupUnexpandedActive : groupUnexpandedInactive);
            }

            private void SetState(bool expanded)
            {
                foreach (GroupContainer childGroup in childGroups)
                {
                    childGroup.mcListItem.gameObject.SetActive(expanded);
                    childGroup.SetState(expanded && childGroup.expanded);
                }

                foreach (ContractContainer childContract in childContracts)
                {
                    childContract.mcListItem.gameObject.SetActive(expanded);
                }
            }
        }

        public class ContractContainer
        {
            public Contract contract;
            public ContractType contractType;
            public MissionControl.MissionSelection missionSelection;
            public MCListItem mcListItem;
            public int indent;
            public UIStateImage statusImage;

            public string OrderKey
            {
                get
                {
                    // TODO - order key
                    return contract == null ? contractType.genericTitle : contract.Title;
                }
            }

            public ContractContainer(ConfiguredContract contract)
            {
                this.contract = contract;
                contractType = contract.contractType;
            }

            public ContractContainer(Contract contract)
            {
                this.contract = contract;
                contractType = null;
            }

            public ContractContainer(ContractType contractType)
            {
                contract = null;
                this.contractType = contractType;
            }
        }

        static Texture2D uiAtlas;
        static UnityEngine.Sprite itemEnabled;
        static UnityEngine.Sprite itemDisabled;
        static UnityEngine.Sprite[] prestigeSprites = new UnityEngine.Sprite[3];
        static UIStateImage.ImageState[] itemStatusStates = new UIStateImage.ImageState[4];
        static UnityEngine.Sprite groupUnexpandedInactive;
        static UnityEngine.Sprite groupUnexpandedActive;
        static UnityEngine.Sprite groupExpandedInactive;
        static UnityEngine.Sprite groupExpandedActive;

        public static MissionControlUI Instance;
        public int ticks = 0;

        private UIRadioButton selectedButton;

        public void Awake()
        {
            Instance = this;

            // Set up persistent stuff
            if (uiAtlas == null)
            {
                uiAtlas = GameDatabase.Instance.GetTexture("ContractConfigurator/ui/MissionControl", false);
                itemEnabled = UnityEngine.Sprite.Create(uiAtlas, new Rect(101, 205, 26, 50), new Vector2(13, 25), 100.0f, 0, SpriteMeshType.Tight, new Vector4(16, 6, 6, 6));
                itemDisabled = UnityEngine.Sprite.Create(uiAtlas, new Rect(101, 153, 26, 50), new Vector2(13, 25), 100.0f, 0, SpriteMeshType.Tight, new Vector4(16, 6, 6, 6));
                prestigeSprites[0] = UnityEngine.Sprite.Create(uiAtlas, new Rect(58, 223, 35, 11), new Vector2(17.5f, 5.5f));
                prestigeSprites[1] = UnityEngine.Sprite.Create(uiAtlas, new Rect(58, 234, 35, 11), new Vector2(17.5f, 5.5f));
                prestigeSprites[2] = UnityEngine.Sprite.Create(uiAtlas, new Rect(58, 245, 35, 11), new Vector2(17.5f, 5.5f));

                // Set up item status image state array
                itemStatusStates[0] = new UIStateImage.ImageState();
                itemStatusStates[1] = new UIStateImage.ImageState();
                itemStatusStates[2] = new UIStateImage.ImageState();
                itemStatusStates[3] = new UIStateImage.ImageState();
                itemStatusStates[0].name = "Offered";
                itemStatusStates[1].name = "Active";
                itemStatusStates[2].name = "Completed";
                itemStatusStates[3].name = "Unavailable";
                itemStatusStates[0].sprite = UnityEngine.Sprite.Create(uiAtlas, new Rect(118, 20, 1, 1), new Vector2(0.5f, 0.5f));
                itemStatusStates[1].sprite = UnityEngine.Sprite.Create(uiAtlas, new Rect(118, 20, 10, 10), new Vector2(5f, 5f));
                itemStatusStates[2].sprite = UnityEngine.Sprite.Create(uiAtlas, new Rect(118, 10, 10, 10), new Vector2(5f, 5f));
                itemStatusStates[3].sprite = UnityEngine.Sprite.Create(uiAtlas, new Rect(118, 0, 10, 10), new Vector2(5f, 5f));

                // Set up group status image state array
                groupExpandedActive = UnityEngine.Sprite.Create(uiAtlas, new Rect(0, 156, 97, 52), new Vector2(78f, 25f), 100.0f, 0, SpriteMeshType.Tight, new Vector4(81, 6, 6, 6));
                groupExpandedInactive = UnityEngine.Sprite.Create(uiAtlas, new Rect(0, 104, 97, 52), new Vector2(78f, 25f), 100.0f, 0, SpriteMeshType.Tight, new Vector4(81, 6, 6, 6));
                groupUnexpandedActive = UnityEngine.Sprite.Create(uiAtlas, new Rect(0, 52, 97, 52), new Vector2(78f, 25f), 100.0f, 0, SpriteMeshType.Tight, new Vector4(81, 6, 6, 6));
                groupUnexpandedInactive = UnityEngine.Sprite.Create(uiAtlas, new Rect(0, 0, 97, 52), new Vector2(78f, 25f), 100.0f, 0, SpriteMeshType.Tight, new Vector4(81, 6, 6, 6));
            }
        }

        public void Update()
        {
            // Wait for the mission control to get loaded
            if (MissionControl.Instance == null)
            {
                ticks = 0;
                return;
            }

            if (ticks++ == 0)
            {
                // Replace the handlers with our own
                MissionControl.Instance.toggleDisplayModeAvailable.onValueChanged.RemoveAllListeners();
                MissionControl.Instance.toggleDisplayModeAvailable.onValueChanged.AddListener(new UnityAction<bool>(OnClickAvailable));
                MissionControl.Instance.btnAccept.onClick.RemoveAllListeners();
                MissionControl.Instance.btnAccept.onClick.AddListener(new UnityAction(OnClickAccept));
                MissionControl.Instance.btnDecline.onClick.RemoveAllListeners();
                MissionControl.Instance.btnDecline.onClick.AddListener(new UnityAction(OnClickDecline));
                MissionControl.Instance.btnCancel.onClick.RemoveAllListeners();
                MissionControl.Instance.btnCancel.onClick.AddListener(new UnityAction(OnClickCancel));

                // Very harsh way to disable the onContractsListChanged in the stock mission control
                GameEvents.Contract.onContractsListChanged = new EventVoid("onContractsListChanged");
                GameEvents.Contract.onContractsListChanged.Add(OnContractsListChanged);

                // Contract state change handlers
                GameEvents.Contract.onOffered.Add(new EventData<Contract>.OnEvent(OnContractOffered));

                // Set to the available view
                OnClickAvailable(true);
            }
        }

        protected void OnContractsListChanged()
        {
        }

        protected void OnContractOffered(Contract c)
        {
            LoggingUtil.LogVerbose(this, "OnContractOffered");

            ConfiguredContract cc = c as ConfiguredContract;
            if (cc != null)
            {
                ContractContainer foundMatch = null;
                GroupContainer currentGroup = null;

                List<UIListData<KSP.UI.UIListItem>>.Enumerator enumerator = MissionControl.Instance.scrollListContracts.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    KSP.UI.UIListItem item = enumerator.Current.listItem;
                    ContractContainer container = item.Data as ContractContainer;
                    if (container != null)
                    {
                        if (container.contractType == cc.contractType)
                        {
                            // Upgrade the contract type line item to a contract
                            if (container.contract == null)
                            {
                                container.contract = cc;
                                SetupContractItem(container);
                                break;
                            }
                            // Keep track of the list item - we'll add immediately after it
                            else
                            {
                                foundMatch = container;
                            }
                        }
                        continue;
                    }

                    GroupContainer groupContainer = item.Data as GroupContainer;
                    if (groupContainer != null)
                    {
                        currentGroup = groupContainer;
                    }
                }

                // Got a match, do an addition
                if (foundMatch != null)
                {
                    ContractContainer container = new ContractContainer(cc);
                    CreateContractItem(container, foundMatch.indent, foundMatch.mcListItem.container);
                    if (currentGroup != null)
                    {
                        currentGroup.childContracts.Add(container);
                    }
                }
            }
            else
            {
                // TODO - handling of non-contract configurator types
            }
        }

        protected void OnContractDeclined(Contract c)
        {
            ConfiguredContract cc = c as ConfiguredContract;
            if (cc != null)
            {

            }
            else
            {
                // TODO - handling of non-contract configurator types
            }
        }

        public void OnClickAvailable(bool selected)
        {
            LoggingUtil.LogVerbose(this, "OnClickAvailable");

            if (!selected)
            {
                return;
            }

            // Set the state on the MissionControl object
            MissionControl.Instance.displayMode = MissionControl.DisplayMode.Available;
            MissionControl.Instance.toggleArchiveGroup.gameObject.SetActive(false);
            MissionControl.Instance.scrollListContracts.Clear(true);

            // Create the top level contract groups
            CreateGroupItem(null);
            foreach (ContractGroup group in ContractGroup.AllGroups.Where(g => g != null && g.parent == null && ContractType.AllValidContractTypes.Any(ct => g.BelongsToGroup(ct))).
                OrderBy(g => g.displayName))
            {
                CreateGroupItem(group);
            }

            // TODO - groupings for non-CC types
        }

        protected GroupContainer CreateGroupItem(ContractGroup group, int indent = 0)
        {
            MCListItem mcListItem = UnityEngine.Object.Instantiate<MCListItem>(MissionControl.Instance.PrfbMissionListItem);
            GroupContainer groupContainer = new GroupContainer(group);
            mcListItem.container.Data = groupContainer;
            groupContainer.mcListItem = mcListItem;

            // Set up the list item with the group details
            mcListItem.title.text = "<color=#fefa87>" + (group == null ? "Contract Configurator" : group.displayName) + "</color>";
            if (groupContainer.agent != null)
            {
                mcListItem.logoSprite.texture = groupContainer.agent.LogoScaled;
            }
            mcListItem.difficulty.gameObject.SetActive(false);

            // Force to the default state
            mcListItem.GetComponent<Image>().sprite = groupUnexpandedInactive;

            // Add the list item to the UI, and add indent
            MissionControl.Instance.scrollListContracts.AddItem(mcListItem.container, true);
            SetIndent(mcListItem, indent);

            // Create as unexpanded
            if (indent != 0)
            {
                mcListItem.gameObject.SetActive(false);
            }

            // Set the callbacks
            mcListItem.radioButton.onFalseBtn.AddListener(new UnityAction<UIRadioButton, UIRadioButton.CallType, PointerEventData>(OnDeselectGroup));
            mcListItem.radioButton.onTrueBtn.AddListener(new UnityAction<UIRadioButton, UIRadioButton.CallType, PointerEventData>(OnSelectGroup));

            // Add any child groups
            if (group != null)
            {
                foreach (ContractGroup child in ContractGroup.AllGroups.Where(g => g != null && g.parent == group && ContractType.AllValidContractTypes.Any(ct => g.BelongsToGroup(ct))).
                    OrderBy(g => g.displayName))
                {
                    groupContainer.childGroups.Add(CreateGroupItem(child, indent + 1));
                }
            }

            // Add contracts
            foreach (ContractContainer contractContainer in GetContracts(group).OrderBy(c => c.OrderKey))
            {
                CreateContractItem(contractContainer, indent + 1);
                groupContainer.childContracts.Add(contractContainer);
            }

            return groupContainer;
        }

        protected void CreateContractItem(ContractContainer cc, int indent = 0, KSP.UI.UIListItem previous = null)
        {
            // Set up list item
            MCListItem mcListItem = UnityEngine.Object.Instantiate<MCListItem>(MissionControl.Instance.PrfbMissionListItem);
            mcListItem.logoSprite.gameObject.SetActive(false);
            mcListItem.container.Data = cc;
            cc.mcListItem = mcListItem;
            cc.indent = indent;

            // Set up the radio button to the custom sprites for contracts
            UIRadioButton radioButton = mcListItem.GetComponent<UIRadioButton>();
            radioButton.stateTrue.normal = radioButton.stateTrue.highlight = radioButton.stateTrue.pressed = radioButton.stateTrue.disabled = itemEnabled;
            radioButton.stateFalse.normal = radioButton.stateFalse.highlight = radioButton.stateFalse.pressed = radioButton.stateFalse.disabled = itemDisabled;
            mcListItem.GetComponent<Image>().sprite = itemDisabled;

            // Fix up the position/sizing of the text element
            GameObject textObject = mcListItem.gameObject.GetChild("Text");
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(textRect.anchoredPosition.x - 60, textRect.anchoredPosition.y);
            textRect.sizeDelta = new Vector2(textRect.sizeDelta.x + 60 - 20, textRect.sizeDelta.y);

            // Set up the difficulty/prestige stars
            mcListItem.difficulty.states[0].sprite = prestigeSprites[0];
            mcListItem.difficulty.states[1].sprite = prestigeSprites[1];
            mcListItem.difficulty.states[2].sprite = prestigeSprites[2];

            // Create an icon to show the status
            GameObject statusImage = new GameObject("StatusImage");
            RectTransform statusRect = statusImage.AddComponent<RectTransform>();
            statusRect.anchoredPosition = new Vector2(16.0f, 0f);
            statusRect.anchorMin = new Vector2(0, 0.5f);
            statusRect.anchorMax = new Vector2(0, 0.5f);
            statusRect.sizeDelta = new Vector2(10f, 10f);
            statusImage.AddComponent<CanvasRenderer>();
            cc.statusImage = statusImage.AddComponent<UIStateImage>();
            cc.statusImage.states = itemStatusStates;
            cc.statusImage.image = statusImage.AddComponent<Image>();
            statusImage.transform.SetParent(mcListItem.transform);

            // Finalize difficulty UI
            RectTransform diffRect = mcListItem.difficulty.GetComponent<RectTransform>();
            diffRect.anchoredPosition = new Vector2(-20.5f, -12.5f);
            diffRect.sizeDelta = new Vector2(35, 11);

            // Set the callbacks
            mcListItem.radioButton.onFalseBtn.AddListener(new UnityAction<UIRadioButton, UIRadioButton.CallType, PointerEventData>(OnDeselectContract));
            mcListItem.radioButton.onTrueBtn.AddListener(new UnityAction<UIRadioButton, UIRadioButton.CallType, PointerEventData>(OnSelectContract));

            // Do other setup
            SetupContractItem(cc);

            // Create as unexpanded
            if (indent != 0)
            {
                mcListItem.gameObject.SetActive(false);
            }

            // Add the list item to the UI, and add indent
            if (previous == null)
            {
                MissionControl.Instance.scrollListContracts.AddItem(mcListItem.container, true);
            }
            else
            {
                int index = MissionControl.Instance.scrollListContracts.GetIndex(previous);
                MissionControl.Instance.scrollListContracts.InsertItem(mcListItem.container, index);
            }
            SetIndent(mcListItem, indent);

            LayoutElement layoutElement = mcListItem.GetComponent<LayoutElement>();
            layoutElement.preferredHeight /= 2;
        }

        protected void SetupContractItem(ContractContainer cc)
        {
            // Set up the list item with the contract details
            SetContractTitle(cc.mcListItem, cc);

            // Add callback data
            cc.missionSelection = new MissionControl.MissionSelection(true, cc.contract, cc.mcListItem.container);

            // Setup with contract
            if (cc.contract != null)
            {
                // Set difficulty
                cc.mcListItem.difficulty.gameObject.SetActive(true);
                cc.mcListItem.difficulty.SetState((int)cc.contract.Prestige);

                // Set status
                cc.statusImage.SetState(cc.contract.ContractState == Contract.State.Active ? "Active" : cc.contract.ContractState == Contract.State.Completed ? "Completed" : "Offered");
            }
            // Setup without contract
            else
            {
                Contract.ContractPrestige? prestige = GetPrestige(cc.contractType);
                if (prestige != null)
                {
                    cc.mcListItem.difficulty.SetState((int)prestige.Value);
                }
                else
                {
                    cc.mcListItem.difficulty.gameObject.SetActive(false);
                }

                // TODO - choose
                cc.statusImage.SetState(cc.contractType.maxCompletions != 0 && cc.contractType.ActualCompletions() >= cc.contractType.maxCompletions ? "Completed" : "Unavailable");
            }
        }

        protected Contract.ContractPrestige? GetPrestige(ContractType contractType)
        {
            if (contractType.dataNode.IsDeterministic("prestige"))
            {
                if (contractType.prestige.Count == 1)
                {
                    return contractType.prestige.First();
                }
            }
            return null;
        }

        protected IEnumerable<ContractContainer> GetContracts(ContractGroup group)
        {
            foreach (ContractType contractType in ContractType.AllValidContractTypes.Where(ct => ct.group == group))
            {
                // Return any configured contracts for the group
                bool any = false;
                foreach (ConfiguredContract contract in ConfiguredContract.CurrentContracts)
                {
                    if (contract.contractType == contractType)
                    {
                        any = true;
                        yield return new ContractContainer(contract);
                    }
                }
                // If there are none, then return the contract type
                if (!any)
                {
                    yield return new ContractContainer(contractType);
                }
            }
        }

        protected void SetIndent(MCListItem mcListItem, int indent)
        {
            // Don't bother messing around if there is no indent
            if (indent == 0)
            {
                return;
            }

            // Re-order the hierarchy to add spacers for indented items
            GameObject go = new GameObject("GroupContainer");
            go.transform.parent = mcListItem.transform.parent;
            go.AddComponent<RectTransform>();
            go.AddComponent<CanvasRenderer>();
            go.AddComponent<HorizontalLayoutGroup>();

            // Create a spacer sized based on the indent
            GameObject spacer = new GameObject("Spacer");
            spacer.AddComponent<RectTransform>();
            LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.minWidth = indent * 12;
            ContentSizeFitter spacerFitter = spacer.AddComponent<ContentSizeFitter>();
            spacerFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;

            // Re-parent the spacer and list item
            spacer.transform.SetParent(go.transform);
            mcListItem.transform.SetParent(go.transform);

            // Perform some surgury on the list item to set its preferred width to the correct value
            LayoutElement le = mcListItem.GetComponent<LayoutElement>();
            le.preferredWidth = 316 - indent * 12;
            le.flexibleWidth = 1;
            ContentSizeFitter mcListItemFitter = mcListItem.gameObject.AddComponent<ContentSizeFitter>();
            mcListItemFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        protected void OnSelectContract(UIRadioButton button, UIRadioButton.CallType callType, PointerEventData data)
        {
            LoggingUtil.LogVerbose(this, "OnSelectContract");

            if (callType != UIRadioButton.CallType.USER)
            {
                return;
            }

            ContractContainer cc = (ContractContainer)button.GetComponent<KSP.UI.UIListItem>().Data;

            MissionControl.Instance.panelView.gameObject.SetActive(true);
            MissionControl.Instance.logoRenderer.gameObject.SetActive(true);
            selectedButton = button;
            Contract.ContractPrestige? prestige = null;
            if (cc.contract != null)
            {
                MissionControl.Instance.selectedMission = cc.missionSelection;
                MissionControl.Instance.UpdateInfoPanelContract(cc.contract);
                prestige = cc.contract.Prestige;
            }
            else
            {
                UpdateInfoPanelContractType(cc.contractType);
                prestige = GetPrestige(cc.contractType);
            }

            if (prestige == Contracts.Contract.ContractPrestige.Exceptional)
            {
                MissionControl.Instance.UpdateInstructor(MissionControl.Instance.avatarController.animTrigger_selectHard, MissionControl.Instance.avatarController.animLoop_excited);
            }
            else if (prestige == Contracts.Contract.ContractPrestige.Significant)
            {
                MissionControl.Instance.UpdateInstructor(MissionControl.Instance.avatarController.animTrigger_selectNormal, MissionControl.Instance.avatarController.animLoop_default);
            }
            else
            {
                MissionControl.Instance.UpdateInstructor(MissionControl.Instance.avatarController.animTrigger_selectEasy, MissionControl.Instance.avatarController.animLoop_default);
            }
        }

        protected void OnDeselectContract(UIRadioButton button, UIRadioButton.CallType callType, PointerEventData data)
        {
            if (callType != UIRadioButton.CallType.USER)
            {
                return;
            }

            MissionControl.Instance.panelView.gameObject.SetActive(false);
            MissionControl.Instance.ClearInfoPanel();
            MissionControl.Instance.selectedMission = null;
            selectedButton = null;
        }


        protected void OnSelectGroup(UIRadioButton button, UIRadioButton.CallType callType, PointerEventData data)
        {
            LoggingUtil.LogVerbose(this, "OnSelectGroup");

            if (callType != UIRadioButton.CallType.USER)
            {
                return;
            }

            GroupContainer groupContainer = (GroupContainer)button.GetComponent<KSP.UI.UIListItem>().Data;

            if (groupContainer.agent != null)
            {
                MissionControl.Instance.panelView.gameObject.SetActive(true);
                MissionControl.Instance.logoRenderer.gameObject.SetActive(true);

                MissionControl.Instance.UpdateInfoPanelAgent(groupContainer.agent);
                MissionControl.Instance.btnAgentBack.gameObject.SetActive(false);
            }
            else
            {
                MissionControl.Instance.panelView.gameObject.SetActive(false);
                MissionControl.Instance.logoRenderer.gameObject.SetActive(false);
            }

            groupContainer.Toggle();

        }

        protected void OnDeselectGroup(UIRadioButton button, UIRadioButton.CallType callType, PointerEventData data)
        {
            LoggingUtil.LogVerbose(this, "OnDeselectGroup");

            if (callType != UIRadioButton.CallType.USER)
            {
                return;
            }

            MissionControl.Instance.panelView.gameObject.SetActive(false);
            MissionControl.Instance.ClearInfoPanel();

            GroupContainer groupContainer = (GroupContainer)button.GetComponent<KSP.UI.UIListItem>().Data;
            groupContainer.Toggle();
        }

        protected void SetContractTitle(MCListItem mcListItem, ContractContainer cc)
        {
            // Set up the list item with the contract details
            string color = cc.contract == null ? "A9A9A9" : cc.contract.ContractState == Contract.State.Active ? "96df41" : "fefa87";
            string title = cc.contract == null ? cc.contractType.genericTitle : cc.contract.Title; // TODO - proper title for contract type
            mcListItem.title.text = "<color=#" + color + ">" + title + "</color>";
            if (cc.contract != null)
            {
                mcListItem.difficulty.SetState((int)cc.contract.Prestige);
            }
            else
            {
                // TODO - contract type prestige
            }
        }

        /// <summary>
        /// Updates the information panel to show the given contract type
        /// </summary>
        /// <param name="contractType"></param>
        protected void UpdateInfoPanelContractType(ContractType contractType)
        {
            MissionControl.Instance.UpdateInfoPanelContract(null);

            // Set up buttons
            MissionControl.Instance.btnAccept.gameObject.SetActive(false);
            MissionControl.Instance.btnDecline.gameObject.SetActive(false);
            MissionControl.Instance.btnCancel.gameObject.SetActive(false);
            MissionControl.Instance.btnAgentBack.gameObject.SetActive(false);

            // Set up agent
            string agentText = "";
            if (contractType.agent != null)
            {
                MissionControl.Instance.logoRenderer.texture = contractType.agent.Logo;
                agentText = "\n\n<b><color=#DB8310>Agent:</color></b>\n" + contractType.agent.Name;
            }
            else
            {
                MissionControl.Instance.logoRenderer.gameObject.SetActive(false);
            }

            // Set up text
            MissionControl.Instance.textContractInfo.text = "<b><color=#DB8310>Contract:</color></b>\n" + contractType.genericTitle + agentText;
            MissionControl.Instance.contractTextRect.verticalNormalizedPosition = 1f;
            MissionControl.Instance.textDateInfo.text = "";

            // Set up main text area
            MissionControlText(contractType);
        }

        protected void MissionControlText(ContractType contractType)
        {
            string text = "<b><color=#DB8310>Briefing:</color></b>\n\n";
            text += "<color=#CCCCCC>" + contractType.genericDescription + "</color>\n\n";

            text += "<b><color=#DB8310>Pre-Requisites:</color></b>\n\n";

            // Do text for max completions
            if (contractType.maxCompletions != 0)
            {
                int completionCount = contractType.ActualCompletions();
                bool met = completionCount < contractType.maxCompletions;
                text += RequirementLine("May only be completed " + (contractType.maxCompletions == 1 ? "once" : contractType.maxCompletions + " times"), met,
                    "has been completed " + (completionCount == 1 ? "once" : completionCount + " times"));
            }

            // Force check requirements for this contract
            CheckRequirements(contractType.Requirements);

            text += ContractRequirementText(contractType.Requirements);

            MissionControl.Instance.contractText.text = text;
        }

        protected string ContractRequirementText(IEnumerable<ContractRequirement> requirements, string indent = "")
        {
            string text = "";
            foreach (ContractRequirement requirement in requirements)
            {
                if (requirement.enabled)
                {
                    bool met = requirement.lastResult != null && requirement.lastResult.Value;
                    text += RequirementLine(indent + requirement.Title, met);

                    if (!requirement.hideChildren)
                    {
                        text += ContractRequirementText(requirement.ChildRequirements, indent + "    ");
                    }
                }
            }
            return text;
        }

        protected string RequirementLine(string text, bool met, string unmetReason = "")
        {
            string color = met ? "#8BED8B" : "#FFEA04";
            string output = "<b><color=#BEC2AE>" + text + ": </color></b><color=" + color + ">" + (met ? "Met" : "Unmet") + "</color>";
            if (!string.IsNullOrEmpty(unmetReason) && !met)
            {
                output += " <color=#CCCCCC>(" + unmetReason + ")</color>\n";
            }
            else
            {
                output += "\n";
            }
            return output;
        }


        protected void CheckRequirements(IEnumerable<ContractRequirement> requirements)
        {
            foreach (ContractRequirement requirement in requirements)
            {
                try
                {
                    requirement.lastResult = requirement.invertRequirement ^ requirement.RequirementMet(null);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                CheckRequirements(requirement.ChildRequirements);
            }
        }

        private void OnClickAccept()
        {
            LoggingUtil.LogVerbose(this, "OnClickAccept");

            // Accept the contract
            MissionControl.Instance.selectedMission.contract.Accept();
            MissionControl.Instance.UpdateInstructor(MissionControl.Instance.avatarController.animTrigger_accept, MissionControl.Instance.avatarController.animLoop_default);

            // Update the contract
            SetContractTitle(selectedButton.GetComponent<MCListItem>(), new ContractContainer(MissionControl.Instance.selectedMission.contract));
            OnSelectContract(selectedButton, UIRadioButton.CallType.USER, null);
        }

        private void OnClickDecline()
        {
            LoggingUtil.LogVerbose(this, "OnClickDecline");

            // Decline the contract
            MissionControl.Instance.selectedMission.contract.Decline();
            MissionControl.Instance.selectedMission = null;
            MissionControl.Instance.panelView.gameObject.SetActive(false);
            MissionControl.Instance.ClearInfoPanel();
            MissionControl.Instance.UpdateInstructor(MissionControl.Instance.avatarController.animTrigger_decline, MissionControl.Instance.avatarController.animLoop_default);

            // Redraw
            selectedButton = null;
            // TODO - better performance by using OnDeclined callback to target specific item
            OnClickAvailable(true);
        }

        private void OnClickCancel()
        {
            LoggingUtil.LogVerbose(this, "OnClickCancel");

            // Cancel the contract
            MissionControl.Instance.selectedMission.contract.Cancel();
            MissionControl.Instance.selectedMission = null;
            MissionControl.Instance.panelView.gameObject.SetActive(false);
            MissionControl.Instance.ClearInfoPanel();
            MissionControl.Instance.UpdateInstructor(MissionControl.Instance.avatarController.animTrigger_cancel, MissionControl.Instance.avatarController.animLoop_default);

            // Redraw
            selectedButton = null;
            // TODO - better performance by using OnDeclined callback to target specific item
            OnClickAvailable(true);
        }
    }

    public static class TransformExtns
    {
        public static Transform FindDeepChild(this Transform parent, string name)
        {
            var result = parent.Find(name);
            if (result != null)
                return result;
            foreach (Transform child in parent)
            {
                result = child.FindDeepChild(name);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static void Dump(this GameObject go, string indent = "")
        {
            foreach (Component c in go.GetComponents<Component>())
            {
                Debug.Log(indent + c);
                if (c is KerbalInstructor)
                {
                    return;
                }
            }

            foreach (Transform c in go.transform)
            {
                c.gameObject.Dump(indent + "    ");
            }
        }
    }
}
