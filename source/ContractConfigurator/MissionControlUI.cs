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
using Contracts.Templates;
using FinePrint.Contracts;

namespace ContractConfigurator.Util
{
    /// <summary>
    /// Special MonoBehaviour to replace portions of the stock mission control UI.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class MissionControlUI : MonoBehaviour
    {
        static FieldInfo childUIListField = typeof(UIList).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(fi => fi.FieldType == typeof(UIList<KSP.UI.UIListItem>)).First();
        static FieldInfo listDataField = typeof(UIList<KSP.UI.UIListItem>).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(fi => fi.FieldType == typeof(List<UIListData<KSP.UI.UIListItem>>)).First();
        public static string RequirementHighlightColor = "F9F9F6";

        public class Container
        {
            public Transform listItemTransform;
            public MCListItem mcListItem;
        }

        public class GroupContainer : Container
        {
            public GroupContainer parent;
            public ContractGroup group;
            public Type stockContractType;
            public Agent agent;
            public bool expanded = false;
            public List<GroupContainer> childGroups = new List<GroupContainer>();
            public List<ContractContainer> childContracts = new List<ContractContainer>();
            public int availableContracts;
            public bool unread = false;

            static Dictionary<string, string> contractNames = new Dictionary<string, string>();

            static GroupContainer()
            {
                //
                // Set up the mapping of known contract type names
                //

                // Stock stuff
                contractNames[typeof(CollectScience).Name] = "Collect Science";
                contractNames[typeof(ExploreBody).Name] = "Exploration";
                contractNames[typeof(GrandTour).Name] = "Grand Tour";
                contractNames[typeof(PartTest).Name] = "Part Test";
                contractNames[typeof(PlantFlag).Name] = "Flag Planting";
                contractNames[typeof(RecoverAsset).Name] = "Rescue and Recovery";
                contractNames[typeof(ARMContract).Name] = "Asteroid Recovery";
                contractNames[typeof(BaseContract).Name] = "Base Construction";
                contractNames[typeof(ISRUContract).Name] = "ISRU";
                contractNames[typeof(SatelliteContract).Name] = "Satellites";
                contractNames[typeof(StationContract).Name] = "Stations";
                contractNames[typeof(SurveyContract).Name] = "Surveys";
                contractNames[typeof(TourismContract).Name] = "Tourism";
                contractNames[typeof(WorldFirstContract).Name] = "World-Firsts Achievements";

                // DMagic Orbital Science (by name instead of type)
                contractNames["DMAnomalyContract"] = "Anomalies";
                contractNames["DMAsteroidSurveyContract"] = "Asteroid Survey";
                contractNames["DMMagneticSurveyContract"] = "Magnetic Survey";
                contractNames["DMReconContract"] = "Reconnaisance Survey";
                contractNames["DMSurveyContract "] = "Orbital Survey";
            }

            public GroupContainer(ContractGroup group)
            {
                this.group = group;

                // Set the agent from the group
                if (group != null && group.agent != null)
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

            public GroupContainer(Type stockContractType)
            {
                this.stockContractType = stockContractType;

                // Find the right agent
                if (stockContractType.Assembly.FullName.Contains("DMagic"))
                {
                    agent = GetAgent("DMagic");
                }
                else if (stockContractType == typeof(CollectScience))
                {
                    agent = GetAgent("Research & Development Department");
                }
                else if (stockContractType == typeof(WorldFirstContract))
                {
                    agent = GetAgent("Kerbin World-Firsts Record-Keeping Society");
                }
            }

            private Agent GetAgent(string name)
            {
                foreach (Agent agent in AgentList.Instance.Agencies)
                {
                    if (agent.Name == name)
                    {
                        return agent;
                    }
                }
                return null;
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

            public void SetState(bool expanded)
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

            public string DisplayName()
            {
                if (stockContractType != null)
                {
                    return DisplayName(stockContractType);
                }
                else if (group != null)
                {
                    return group.displayName;
                }
                else
                {
                    return "Contract Configurator";
                }
            }

            public static string DisplayName(Type type)
            {
                return contractNames.ContainsKey(type.Name) ? contractNames[type.Name] : type.Name;
            }

            public string OrderKey()
            {
                return stockContractType != null ? DisplayName(stockContractType) : group == null ? "aaa" : group.sortKey;
            }

            public static string OrderKey(Contract c)
            {
                ConfiguredContract cc = c as ConfiguredContract;
                if (cc != null)
                {
                    string key = cc.contractType.group == null ? "aaa" : cc.contractType.group.sortKey;
                    for (ContractGroup group = cc.contractType.group; group != null; group = group.parent)
                    {
                        if (group.parent != null)
                        {
                            key = group.parent.sortKey + "." + key;
                        }
                    }
                    return key;
                }
                else
                {
                    return DisplayName(c.GetType());
                }
            }
        }

        public class ContractContainer : Container
        {
            public Contract contract;
            public ContractType contractType;
            public MissionControl.MissionSelection missionSelection;
            public int indent;
            public UIStateImage statusImage;
            public GroupContainer parent;
            public GroupContainer root
            {
                get
                {
                    GroupContainer result = parent;
                    while (result != null && result.parent != null)
                    {
                        result = result.parent;
                    }
                    return result;
                }
            }

            public string OrderKey
            {
                get
                {
                    return contractType != null ? contractType.sortKey : contract.Title;
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

                // Disable GameEvent handlers
                GameEvents.Contract.onContractsListChanged.Remove(new EventVoid.OnEvent(OnContractsListChanged));
                GameEvents.Contract.onOffered.Remove(new EventData<Contract>.OnEvent(OnContractOffered));
                GameEvents.Contract.onDeclined.Remove(new EventData<Contract>.OnEvent(OnContractDeclined));
                GameEvents.Contract.onFinished.Remove(new EventData<Contract>.OnEvent(OnContractFinished));

                return;
            }

            if (ticks++ == 0)
            {
                int[] widths = new int[] { 57, 92, 77, 89};

                // Get the available/active/complete groups
                GameObject sortGroup = MissionControl.Instance.gameObject.GetChild("Sorting Group");
                GameObject toggleAvailable = sortGroup.GetChild("Toggle Available");
                GameObject toggleAllObj = UnityEngine.Object.Instantiate<GameObject>(toggleAvailable);
                toggleAllObj.name = "Toggle All";
                toggleAllObj.transform.SetParent(sortGroup.transform);
                toggleAllObj.transform.SetAsFirstSibling();

                // Setup the toggle
                Text toggleAllText = toggleAllObj.GetChild("Text").GetComponent<Text>();
                toggleAllText.text = "All";
                Toggle toggleAll = toggleAllObj.GetComponent<Toggle>();
                toggleAll.onValueChanged.AddListener(new UnityAction<bool>(OnClickAll));
                sortGroup.GetComponent<ToggleGroup>().RegisterToggle(toggleAll);

                // Set positioning info
                RectTransform toggleAllRect = toggleAllObj.GetComponent<RectTransform>();
                RectTransform toggleAvailableRect = toggleAvailable.GetComponent<RectTransform>();
                RectTransform toggleActiveRect = MissionControl.Instance.toggleDisplayModeActive.GetComponent<RectTransform>();
                RectTransform toggleArchiveRect = MissionControl.Instance.toggleDisplayModeArchive.GetComponent<RectTransform>();
                float x = toggleAvailableRect.anchoredPosition.x;
                float y = toggleAvailableRect.anchoredPosition.y;
                float h = toggleAvailableRect.sizeDelta.y;
                toggleAllRect.anchoredPosition3D = toggleAvailable.GetComponent<RectTransform>().anchoredPosition3D;
                toggleAllRect.anchoredPosition = new Vector2(x, y);
                toggleAllRect.sizeDelta = new Vector2(widths[0], h);
                x += widths[0];
                toggleAvailableRect.anchoredPosition = new Vector2(x, y);
                toggleAvailableRect.sizeDelta = new Vector2(widths[1], h);
                x += widths[1];
                toggleActiveRect.anchoredPosition = new Vector2(x, y);
                toggleActiveRect.sizeDelta = new Vector2(widths[2], h);
                x += widths[2];
                toggleArchiveRect.anchoredPosition = new Vector2(x, y);
                toggleArchiveRect.sizeDelta = new Vector2(widths[3], h);

                // Set Positioning of child elements
                foreach (RectTransform rect in new RectTransform[] { toggleAllRect, toggleAvailableRect, toggleActiveRect, toggleArchiveRect })
                {
                    RectTransform bgRect = rect.gameObject.GetChild("Background").GetComponent<RectTransform>();
                    bgRect.sizeDelta = rect.sizeDelta;
                    RectTransform checkRect = bgRect.gameObject.GetChild("Checkmark").GetComponent<RectTransform>();
                    checkRect.anchoredPosition = new Vector2(checkRect.anchoredPosition.x + (105 - rect.sizeDelta.x) / 2.0f, checkRect.anchoredPosition.y);
                }


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
                GameEvents.Contract.onContractsListChanged.Add(new EventVoid.OnEvent(OnContractsListChanged));

                // Contract state change handlers
                GameEvents.Contract.onOffered.Add(new EventData<Contract>.OnEvent(OnContractOffered));
                GameEvents.Contract.onDeclined.Add(new EventData<Contract>.OnEvent(OnContractDeclined));
                GameEvents.Contract.onFinished.Add(new EventData<Contract>.OnEvent(OnContractFinished));

                // Set to the all view
                toggleAll.isOn = true;
                toggleAvailable.GetComponent<Toggle>().isOn = false;
                OnClickAll(true);
            }
        }

        protected void OnContractsListChanged()
        {
        }

        protected void OnContractOffered(Contract c)
        {
            LoggingUtil.LogVerbose(this, "OnContractOffered");

            // Add to the unread list.  This gets done elsewhere, but the events fire in the wrong order for us to take advantage of it
            ContractPreLoader.Instance.unreadContracts.Add(c.ContractGuid);

            ConfiguredContract cc = c as ConfiguredContract;
            ContractContainer container = null;
            if (cc != null)
            {
                ContractContainer foundMatch = null;

                List<UIListData<KSP.UI.UIListItem>>.Enumerator enumerator = MissionControl.Instance.scrollListContracts.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    KSP.UI.UIListItem item = enumerator.Current.listItem;
                    container = item.Data as ContractContainer;
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
                }

                // Got a match, do an addition
                if (foundMatch != null)
                {
                    container = new ContractContainer(cc);
                    container.parent = foundMatch.parent;
                    container.parent.childContracts.Add(container);
                    CreateContractItem(container, foundMatch.indent, foundMatch.mcListItem.container);

                    // Show/hide based on state of group line
                    container.mcListItem.gameObject.SetActive(container.parent.expanded && container.parent.mcListItem.gameObject.activeSelf);
                }
            }
            else
            {
                List<UIListData<KSP.UI.UIListItem>>.Enumerator enumerator = MissionControl.Instance.scrollListContracts.GetEnumerator();
                Type contractType = c.GetType();
                GroupContainer closestGroup = null;
                ContractContainer lastEntry = null;
                ContractContainer foundMatch = null;
                while (enumerator.MoveNext())
                {
                    KSP.UI.UIListItem item = enumerator.Current.listItem;
                    // Check for a group to use for the position later if we need to add a group
                    GroupContainer groupContainer = item.Data as GroupContainer;
                    if (groupContainer != null)
                    {
                        if (closestGroup == null || string.Compare(groupContainer.DisplayName(), GroupContainer.DisplayName(contractType)) < 0)
                        {
                            closestGroup = groupContainer;
                        }
                    }

                    ContractContainer otherContainer = item.Data as ContractContainer;
                    if (otherContainer != null)
                    {
                        // Track the last entry in case we need to do a group insert
                        if (otherContainer.root == closestGroup)
                        {
                            lastEntry = otherContainer;
                        }

                        if (otherContainer.parent != null && otherContainer.parent.stockContractType == contractType)
                        {
                            foundMatch = otherContainer;
                        }
                    }
                }

                // Add the new item to the end of the grouping
                if (foundMatch != null)
                {
                    container = new ContractContainer(c);
                    container.parent = foundMatch.parent;
                    container.parent.childContracts.Add(container);
                    CreateContractItem(container, foundMatch.indent, foundMatch.mcListItem.container);

                    // Show/hide based on state of group line
                    container.mcListItem.gameObject.SetActive(container.parent.expanded && container.parent.mcListItem.gameObject.activeSelf);
                }
                // Need to create a group if it wasn't found (this will create the contract too
                else
                {
                    GroupContainer groupContainer = new GroupContainer(contractType);
                    CreateGroupItem(groupContainer, 0, lastEntry.mcListItem.container);
                }
            }

            // Update group details
            if (container != null)
            {
                SetupParentGroups(container);
            }
        }

        protected void OnContractDeclined(Contract c)
        {
            ConfiguredContract cc = c as ConfiguredContract;
            if (cc != null)
            {
                // TODO - proper decline handling
            }
            else
            {
                // TODO - handling of non-contract configurator types
            }

            // TODO - better performance by using OnDeclined callback to target specific item
            OnClickAll(true);
        }

        protected void OnContractFinished(Contract c)
        {
            ConfiguredContract cc = c as ConfiguredContract;
            if (cc != null)
            {
                // TODO - proper decline handling
            }
            else
            {
                // TODO - handling of non-contract configurator types
            }

            // TODO - better performance by using OnDeclined callback to target specific item
            OnClickAll(true);
        }

        public IEnumerable<GroupContainer> GetGroups()
        {
            // Grouping for CC types
            foreach (ContractGroup group in ContractGroup.AllGroups.Where(g => g != null && g.parent == null && ContractType.AllValidContractTypes.Any(ct => g.BelongsToGroup(ct))))
            {
                yield return new GroupContainer(group);
            }

            // Groupings for non-CC types
            foreach (Type subclass in ContractConfigurator.GetAllTypes<Contract>().Where(t => t != null && !t.Name.StartsWith("ConfiguredContract")))
            {
                if (ContractDisabler.IsEnabled(subclass))
                {
                    yield return new GroupContainer(subclass);
                }
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

            foreach (Contract contract in ContractSystem.Instance.Contracts.Union(ContractPreLoader.Instance.PendingContracts().OfType<Contract>()).
                Where(c => c.ContractState == Contract.State.Offered).
                OrderBy(c => GroupContainer.OrderKey(c)))
            {
                MissionControl.Instance.AddItem(contract, true, string.Empty);
            }
        }

        public void OnClickAll(bool selected)
        {
            LoggingUtil.LogVerbose(this, "OnClickAll");

            if (!selected)
            {
                return;
            }

            // Set the state on the MissionControl object
            MissionControl.Instance.displayMode = MissionControl.DisplayMode.Available;
            MissionControl.Instance.toggleArchiveGroup.gameObject.SetActive(false);
            MissionControl.Instance.scrollListContracts.Clear(true);

            // Create the top level contract groups
            CreateGroupItem(new GroupContainer((ContractGroup)null));
            foreach (GroupContainer groupContainer in GetGroups().OrderBy(cg => cg.OrderKey()))
            {
                CreateGroupItem(groupContainer);
            }
        }

        protected GroupContainer CreateGroupItem(GroupContainer groupContainer, int indent = 0, KSP.UI.UIListItem previous = null)
        {
            MCListItem mcListItem = UnityEngine.Object.Instantiate<MCListItem>(MissionControl.Instance.PrfbMissionListItem);
            mcListItem.container.Data = groupContainer;
            groupContainer.mcListItem = mcListItem;

            // Set up the list item with the group details
            if (groupContainer.agent != null)
            {
                mcListItem.logoSprite.texture = groupContainer.agent.LogoScaled;
            }
            mcListItem.difficulty.gameObject.SetActive(false);

            // Force to the default state
            mcListItem.GetComponent<Image>().sprite = groupUnexpandedInactive;


            // Add the list item to the UI, and add indent
            if (previous == null)
            {
                MissionControl.Instance.scrollListContracts.AddItem(mcListItem.container, true);

                groupContainer.listItemTransform = mcListItem.transform;
                SetIndent(mcListItem, indent);
            }
            else
            {
                InsertIntoList(groupContainer, indent, previous);
            }

            // Create as unexpanded
            if (indent != 0)
            {
                mcListItem.gameObject.SetActive(false);
            }

            // Set the callbacks
            mcListItem.radioButton.onFalseBtn.AddListener(new UnityAction<UIRadioButton, UIRadioButton.CallType, PointerEventData>(OnDeselectGroup));
            mcListItem.radioButton.onTrueBtn.AddListener(new UnityAction<UIRadioButton, UIRadioButton.CallType, PointerEventData>(OnSelectGroup));

            bool hasChildren = false;

            // Add any child groups
            if (groupContainer.group != null)
            {
                foreach (ContractGroup child in ContractGroup.AllGroups.Where(g => g != null && g.parent == groupContainer.group && ContractType.AllValidContractTypes.Any(ct => g.BelongsToGroup(ct))).
                    OrderBy(g => g.displayName))
                {
                    hasChildren = true;
                    GroupContainer childContainer = new GroupContainer(child);
                    childContainer.parent = groupContainer;
                    groupContainer.childGroups.Add(CreateGroupItem(childContainer, indent + 1));
                }
            }

            // Add contracts
            Container lastContainer = groupContainer;
            foreach (ContractContainer contractContainer in GetContracts(groupContainer).OrderBy(c => c.OrderKey))
            {
                contractContainer.parent = groupContainer;
                groupContainer.childContracts.Add(contractContainer);

                hasChildren = true;
                CreateContractItem(contractContainer, indent + 1, previous != null ? lastContainer.mcListItem.container : null);
                lastContainer = contractContainer;
            }

            // Remove groups with nothing underneath them
            if (!hasChildren)
            {
                MissionControl.Instance.scrollListContracts.RemoveItem(mcListItem.container, true);
            }

            // Get the main text object
            GameObject textObject = mcListItem.title.gameObject;
            RectTransform textRect = textObject.GetComponent<RectTransform>();

            // Make non-static modifications 
            SetupGroupItem(groupContainer);

            // Adjust the main text up a little
            textRect.anchoredPosition = new Vector2(textRect.anchoredPosition.x, textRect.anchoredPosition.y + 6);

            return groupContainer;
        }

        protected void SetupParentGroups(ContractContainer contractContainer)
        {
            for (GroupContainer groupContainer = contractContainer.parent; groupContainer != null; groupContainer = groupContainer.parent)
            {
                SetupGroupItem(groupContainer);
            }
        }

        protected void SetupParentGroups(GroupContainer mainGroup)
        {
            for (GroupContainer groupContainer = mainGroup.parent; groupContainer != null; groupContainer = groupContainer.parent)
            {
                SetupGroupItem(groupContainer);
            }
        }

        protected void SetupGroupItem(GroupContainer groupContainer)
        {
            // Assume read
            groupContainer.unread = false;

            // Count the available contracts
            int available = 0;
            foreach (ContractContainer contractContainer in groupContainer.childContracts)
            {
                if (contractContainer.contract != null)
                {
                    if (contractContainer.contract.ContractState == Contract.State.Offered)
                    {
                        available++;
                    }
                    if (ContractPreLoader.Instance.unreadContracts.Contains(contractContainer.contract.ContractGuid))
                    {
                        groupContainer.unread = true;
                    }
                }
            }
            foreach (GroupContainer childContainer in groupContainer.childGroups)
            {
                available += childContainer.availableContracts;
                if (childContainer.unread)
                {
                    groupContainer.unread = true;
                }
            }
            groupContainer.availableContracts = available;

            // Get the main text object
            GameObject textObject = groupContainer.mcListItem.title.gameObject;
            RectTransform textRect = textObject.GetComponent<RectTransform>();

            // Create or find a text object to display the number of contracts
            Transform transform = groupContainer.mcListItem.title.transform.parent.FindChild("AvailableText");
            GameObject availableTextObject = null;
            if (transform != null)
            {
                availableTextObject = transform.gameObject;
            }
            else
            {
                availableTextObject = UnityEngine.Object.Instantiate<GameObject>(groupContainer.mcListItem.title.gameObject);
                availableTextObject.name = "AvailableText";
                availableTextObject.transform.SetParent(groupContainer.mcListItem.title.transform.parent);
                RectTransform availableTextRect = availableTextObject.GetComponent<RectTransform>();
                availableTextRect.anchoredPosition3D = textRect.anchoredPosition3D;
                availableTextRect.sizeDelta = new Vector2(availableTextRect.sizeDelta.x + 4, availableTextRect.sizeDelta.y - 4);
            }

            // Setup the available text
            Text availableText = availableTextObject.GetComponent<Text>();
            availableText.alignment = TextAnchor.LowerRight;
            availableText.text = "<color=#" + (groupContainer.availableContracts == 0 ? "CCCCCC" : "8BED8B") + ">Offered: " + groupContainer.availableContracts + "</color>";
            availableText.fontSize = groupContainer.mcListItem.title.fontSize - 3;

            // Setup the group text
            groupContainer.mcListItem.title.text = "<color=#fefa87>" + groupContainer.DisplayName() + "</color>";
            if (groupContainer.unread)
            {
                groupContainer.mcListItem.title.text = "<b>" + groupContainer.mcListItem.title.text + "</b>";
            }
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

                cc.listItemTransform = mcListItem.transform;
                SetIndent(mcListItem, indent);
            }
            else
            {
                InsertIntoList(cc, indent, previous);
            }

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
                // Set difficulty
                Contract.ContractPrestige? prestige = GetPrestige(cc.contractType);
                if (prestige != null)
                {
                    cc.mcListItem.difficulty.SetState((int)prestige.Value);
                }
                else
                {
                    cc.mcListItem.difficulty.gameObject.SetActive(false);
                }

                // Set status
                cc.statusImage.SetState(cc.contractType.maxCompletions != 0 && cc.contractType.ActualCompletions() >= cc.contractType.maxCompletions ? "Completed" : "Unavailable");
            }
        }

        protected void InsertIntoList(Container container, int indent, KSP.UI.UIListItem previous)
        {
            // Ugly bit of reflection to add to the internals of the list - otherwise the list refresh will mess up the indenting
            int index = MissionControl.Instance.scrollListContracts.GetIndex(previous);
            UIList<KSP.UI.UIListItem> childUIList = (UIList<KSP.UI.UIListItem>)childUIListField.GetValue(MissionControl.Instance.scrollListContracts);
            List<UIListData<KSP.UI.UIListItem>> listData = (List<UIListData<KSP.UI.UIListItem>>)listDataField.GetValue(childUIList);
            listData.Insert(index+1, new UIListData<KSP.UI.UIListItem>((KSP.UI.UIListItem)null, container.mcListItem.container));
            container.mcListItem.container.transform.SetParent(childUIList.listAnchor);
            container.mcListItem.container.transform.localPosition = new Vector3(container.mcListItem.container.transform.localPosition.x, container.mcListItem.container.transform.localPosition.y, 0.0f);

            container.listItemTransform = container.mcListItem.transform;
            SetIndent(container.mcListItem, indent);

            for (int i = 0; i < listData.Count; i++)
            {
                ((Container)listData[i].listItem.Data).listItemTransform.SetSiblingIndex(i);
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

        protected IEnumerable<ContractContainer> GetContracts(GroupContainer groupContainer)
        {
            if (groupContainer.stockContractType != null)
            {
                foreach (Contract contract in ContractSystem.Instance.Contracts.Where(c => c.GetType() == groupContainer.stockContractType))
                {
                    yield return new ContractContainer(contract);
                }
            }
            else
            {
                foreach (ContractType contractType in ContractType.AllValidContractTypes.Where(ct => ct.group == groupContainer.group))
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
            ((Container)mcListItem.container.Data).listItemTransform = go.transform;

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

            // Perform some surgery on the list item to set its preferred width to the correct value
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

            // Mark as read
            if (cc.contract != null)
            {
                if (ContractPreLoader.Instance.unreadContracts.Contains(cc.contract.ContractGuid))
                {
                    ContractPreLoader.Instance.unreadContracts.Remove(cc.contract.ContractGuid);
                }
                SetContractTitle(cc.mcListItem, cc);
                SetupParentGroups(cc);
            }

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

            // Mark the contracts as unread without changing their display state
            foreach (ContractContainer contractContainer in groupContainer.childContracts)
            {
                if (contractContainer.contract != null && ContractPreLoader.Instance.unreadContracts.Contains(contractContainer.contract.ContractGuid))
                {
                    ContractPreLoader.Instance.unreadContracts.Remove(contractContainer.contract.ContractGuid);
                }
            }
            SetupGroupItem(groupContainer);
            SetupParentGroups(groupContainer);
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
            string title = cc.contract == null ? cc.contractType.genericTitle : cc.contract.Title;
            mcListItem.title.text = "<color=#" + color + ">" + title + "</color>";
            if (cc.contract != null && ContractPreLoader.Instance.unreadContracts.Contains(cc.contract.ContractGuid))
            {
                mcListItem.title.text = "<b>" + mcListItem.title.text + "</b>";
            }

            // Setup prestige
            if (cc.contract != null)
            {
                mcListItem.difficulty.SetState((int)cc.contract.Prestige);
            }
            else
            {
                // Set difficulty
                Contract.ContractPrestige? prestige = GetPrestige(cc.contractType);
                if (prestige != null)
                {
                    cc.mcListItem.difficulty.SetState((int)prestige.Value);
                }
                else
                {
                    cc.mcListItem.difficulty.gameObject.SetActive(false);
                }
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

            // Do check of required values
            List<string> titles = new List<string>();
            Dictionary<string, bool> titlesMet = new Dictionary<string, bool>();
            foreach (KeyValuePair<string, ContractType.DataValueInfo> pair in contractType.dataValues)
            {
                string name = pair.Key;
                if (pair.Value.required && !contractType.dataNode.IsDeterministic(name) && !pair.Value.hidden && !pair.Value.IsIgnoredType())
                {
                    bool met = true;
                    try
                    {
                        contractType.CheckRequiredValue(name);
                    }
                    catch
                    {
                        met = false;
                    }

                    string title = string.IsNullOrEmpty(pair.Value.title) ? "Key " + name + " must have a value" : pair.Value.title;
                    if (titlesMet.ContainsKey(title))
                    {
                        titlesMet[title] &= met;
                    }
                    else
                    {
                        titlesMet[title] = met;
                        titles.Add(title);
                    }
                }
            }

            // Do the actual add
            foreach (string title in titles)
            {
                text += RequirementLine(title, titlesMet[title]);
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
