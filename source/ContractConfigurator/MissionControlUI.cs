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
    /// Special MonoBehaviour to fix up the departments.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class MissionControlUI : MonoBehaviour
    {
        public class ContractContainer
        {
            public Contract contract;
            public ContractType contractType;
            public MissionControl.MissionSelection missionSelection;

            public string OrderKey
            {
                get
                {
                    // TODO - order key
                    return contract == null ? contractType.title : contract.Title;
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

        static FieldInfo selectedMissionField = typeof(MissionControl).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(fi => fi.FieldType == typeof(MissionControl.MissionSelection)).First();
        static FieldInfo avatarControllerField = typeof(MissionControl).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(fi => fi.FieldType == typeof(MCAvatarController)).First();
        static MethodInfo updateInstructorMethod = typeof(MissionControl).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Where(mi => mi.Name == "UpdateInstructor").First();

        public static MissionControlUI Instance;
        public int ticks = 0;

        private MCAvatarController avatarController;
        private MissionControl.MissionSelection selectedMission;
        private UIRadioButton selectedButton;

        public void Awake()
        {
            Instance = this;

            // Set up persistent stuff
            if (uiAtlas == null)
            {
                uiAtlas = GameDatabase.Instance.GetTexture("ContractConfigurator/ui/MissionControl", false);
                itemEnabled = UnityEngine.Sprite.Create(uiAtlas, new Rect(1, 13, 26, 50), new Vector2(13, 25), 100.0f, 0, SpriteMeshType.Tight, new Vector4(16, 6, 6, 6));
                itemDisabled = UnityEngine.Sprite.Create(uiAtlas, new Rect(29, 13, 26, 50), new Vector2(13, 25), 100.0f, 0, SpriteMeshType.Tight, new Vector4(16, 6, 6, 6));
                prestigeSprites[0] = UnityEngine.Sprite.Create(uiAtlas, new Rect(58, 31, 35, 11), new Vector2(17.5f, 5.5f));
                prestigeSprites[1] = UnityEngine.Sprite.Create(uiAtlas, new Rect(58, 42, 35, 11), new Vector2(17.5f, 5.5f));
                prestigeSprites[2] = UnityEngine.Sprite.Create(uiAtlas, new Rect(58, 53, 35, 11), new Vector2(17.5f, 5.5f));
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
                // Reflect on things
                avatarController = (MCAvatarController)avatarControllerField.GetValue(MissionControl.Instance);

                // Replace the handlers with our own
                MissionControl.Instance.toggleDisplayModeAvailable.onValueChanged.RemoveAllListeners();
                MissionControl.Instance.toggleDisplayModeAvailable.onValueChanged.AddListener(new UnityAction<bool>(OnClickAvailable));
                MissionControl.Instance.btnAccept.onClick.RemoveAllListeners();
                MissionControl.Instance.btnAccept.onClick.AddListener(new UnityAction(OnClickAccept));
                MissionControl.Instance.btnDecline.onClick.AddListener(new UnityAction(OnClickDecline));
                MissionControl.Instance.btnCancel.onClick.AddListener(new UnityAction(OnClickCancel));

                // Set to the available view
                OnClickAvailable(true);
            }
        }

        public void OnClickAvailable(bool selected)
        {
            Debug.Log("MissionControlUI.OnClickAvailable");
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

        protected void CreateGroupItem(ContractGroup group, int indent = 0)
        {
            MCListItem mcListItem = UnityEngine.Object.Instantiate<MCListItem>(MissionControl.Instance.PrfbMissionListItem);
            Agent agent = GetAgentFromGroup(group);

            // Set up the list item with the group details
            mcListItem.title.text = "<color=#fefa87>" + (group == null ? "Contract Configurator" : group.displayName) + "</color>";
            if (agent != null)
            {
                mcListItem.logoSprite.texture = agent.LogoScaled;
            }
            mcListItem.difficulty.gameObject.SetActive(false);

            // Add the list item to the UI, and add indent
            MissionControl.Instance.scrollListContracts.AddItem(mcListItem.container, true);
            SetIndent(mcListItem, indent);

            // Add any child groups
            if (group != null)
            {
                foreach (ContractGroup child in ContractGroup.AllGroups.Where(g => g != null && g.parent == group && ContractType.AllValidContractTypes.Any(ct => g.BelongsToGroup(ct))).
                    OrderBy(g => g.displayName))
                {
                    CreateGroupItem(child, indent + 1);
                }
            }

            // Add contracts
            foreach (ContractContainer contract in GetContracts(group).OrderBy(c => c.OrderKey))
            {
                CreateContractItem(contract, indent + 1);
            }
        }

        protected void CreateContractItem(ContractContainer cc, int indent = 0)
        {
            // Set up list item
            MCListItem mcListItem = UnityEngine.Object.Instantiate<MCListItem>(MissionControl.Instance.PrfbMissionListItem);
            mcListItem.logoSprite.gameObject.SetActive(false);

            // Set up the list item with the contract details
            SetContractTitle(mcListItem, cc);

            // Add callback data
            cc.missionSelection = new MissionControl.MissionSelection(true, cc.contract, mcListItem.container);
            mcListItem.container.Data = cc;

            // Set up the radio button to the custom sprites for contracts
            UIRadioButton radioButton = mcListItem.GetComponent<UIRadioButton>();
            radioButton.stateTrue.normal = radioButton.stateTrue.highlight = radioButton.stateTrue.pressed = radioButton.stateTrue.disabled = itemEnabled;
            radioButton.stateFalse.normal = radioButton.stateFalse.highlight = radioButton.stateFalse.pressed = radioButton.stateFalse.disabled = itemDisabled;
            mcListItem.GetComponent<Image>().sprite = itemDisabled;

            // Fix up the position/sizing of the text element
            GameObject textObject = mcListItem.gameObject.GetChild("Text");
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(textRect.anchoredPosition.x - 68, textRect.anchoredPosition.y);
            textRect.sizeDelta = new Vector2(textRect.sizeDelta.x + 68 - 20, textRect.sizeDelta.y);

            // Set up the difficulty/prestige stars
            if (cc.contract != null)
            {
                mcListItem.difficulty.states[0].sprite = prestigeSprites[0];
                mcListItem.difficulty.states[1].sprite = prestigeSprites[1];
                mcListItem.difficulty.states[2].sprite = prestigeSprites[2];
                mcListItem.difficulty.SetState((int)cc.contract.Prestige);
                RectTransform diffRect = mcListItem.difficulty.GetComponent<RectTransform>();
                diffRect.anchoredPosition = new Vector2(-20.5f, -12.5f);
                diffRect.sizeDelta = new Vector2(35, 11);
            }
            // TODO - difficulty for contract type
            else
            {

            }

            // Set the callbacks
            mcListItem.radioButton.onFalseBtn.AddListener(new UnityAction<UIRadioButton, UIRadioButton.CallType, PointerEventData>(OnDeselectContract));
            mcListItem.radioButton.onTrueBtn.AddListener(new UnityAction<UIRadioButton, UIRadioButton.CallType, PointerEventData>(OnSelectContract));

            // Add the list item to the UI, and add indent
            MissionControl.Instance.scrollListContracts.AddItem(mcListItem.container, true);
            SetIndent(mcListItem, indent);

            LayoutElement layoutElement = mcListItem.GetComponent<LayoutElement>();
            layoutElement.preferredHeight /= 2;
        }

        protected Agent GetAgentFromGroup(ContractGroup group)
        {
            // TODO - need to get best agent, first from field in group, otherwise most used agent
            ContractType contractType = ContractType.AllValidContractTypes.Where(ct => ct != null && ct.group == group).FirstOrDefault();
            return contractType != null ? contractType.agent : null;
        }

        protected IEnumerable<ContractContainer> GetContracts(ContractGroup group)
        {
            foreach (ContractType contractType in ContractType.AllValidContractTypes.Where(ct => ct.group == group))
            {
                // Return any configured contracts for the group
                bool any = false;
                foreach (ConfiguredContract contract in ConfiguredContract.CurrentContracts.Where(c => c.contractType == contractType))
                {
                    any = true;
                    yield return new ContractContainer(contract);
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
            Debug.Log("OnSelectContract");
            if (callType != UIRadioButton.CallType.USER)
            {
                return;
            }

            ContractContainer cc = (ContractContainer)button.GetComponent<KSP.UI.UIListItem>().Data;

            // TODO - UI for contract type
            if (cc.contract == null)
            {
                return;
            }

            MissionControl.Instance.panelView.gameObject.SetActive(true);
            selectedMissionField.SetValue(MissionControl.Instance, cc.missionSelection);
            selectedMission = cc.missionSelection;
            selectedButton = button;
            MissionControl.Instance.UpdateInfoPanelContract(cc.missionSelection.contract);

            if (cc.missionSelection.contract.Prestige == Contracts.Contract.ContractPrestige.Exceptional)
            {
                updateInstructorMethod.Invoke(MissionControl.Instance, new object[] { avatarController.animTrigger_selectHard, avatarController.animLoop_excited });
            }
            else if (cc.missionSelection.contract.Prestige == Contracts.Contract.ContractPrestige.Trivial)
            {
                updateInstructorMethod.Invoke(MissionControl.Instance, new object[] { avatarController.animTrigger_selectEasy, avatarController.animLoop_default });
            }
            else
            {
                updateInstructorMethod.Invoke(MissionControl.Instance, new object[] { avatarController.animTrigger_selectNormal, avatarController.animLoop_default });
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
            selectedMissionField.SetValue(MissionControl.Instance, null);
            selectedMission = null;
            selectedButton = null;
        }

        protected void SetContractTitle(MCListItem mcListItem, ContractContainer cc)
        {
            // Set up the list item with the contract details
            string color = cc.contract == null ? "A9A9A9" : cc.contract.ContractState == Contract.State.Active ? "96df41" : "fefa87";
            string title = cc.contract == null ? cc.contractType.title : cc.contract.Title; // TODO - proper title for contract type
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

        private void OnClickAccept()
        {
            Debug.Log("MissionControlUI.OnClickAccept");

            // Accept the contract
            selectedMission.contract.Accept();
            updateInstructorMethod.Invoke(MissionControl.Instance, new object[] { avatarController.animTrigger_accept, avatarController.animLoop_default });

            // Update the contract
            SetContractTitle(selectedButton.GetComponent<MCListItem>(), new ContractContainer(selectedMission.contract));
            OnSelectContract(selectedButton, UIRadioButton.CallType.USER, null);
        }

        private void OnClickDecline()
        {
            Debug.Log("MissionControlUI.OnClickDecline");
            selectedMission = null;
            selectedButton = null;
            OnClickAvailable(true);
        }

        private void OnClickCancel()
        {
            Debug.Log("MissionControlUI.OnClickCancel");
            selectedMission = null;
            selectedButton = null;
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
