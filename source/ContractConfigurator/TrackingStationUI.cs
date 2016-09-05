using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using KSP.UI;
using KSP.UI.Screens;
using KSP.UI.TooltipTypes;

namespace ContractConfigurator.Util
{
    /// <summary>
    /// Special MonoBehaviour to replace portions of the stock mission control UI.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class TrackingStationUI : MonoBehaviour
    {
        public TrackingStationUI Instance;

        static Texture2D uiAtlas;
        static UnityEngine.Sprite activeWaypointEnabledSprite;
        static UnityEngine.Sprite activeWaypointDisabledSprite;
        static UnityEngine.Sprite offeredWaypointEnabledSprite;
        static UnityEngine.Sprite offeredWaypointDisabledSprite;
        static UnityEngine.Sprite activeOrbitEnabledSprite;
        static UnityEngine.Sprite activeOrbitDisabledSprite;
        static UnityEngine.Sprite offeredOrbitEnabledSprite;
        static UnityEngine.Sprite offeredOrbitDisabledSprite;

        GameObject activeWaypointButton;
        GameObject offeredWaypointButton;
        GameObject activeOrbitButton;
        GameObject offeredOrbitButton;

        public void Awake()
        {
            Instance = this;

            // Set up persistent stuff
            if (uiAtlas == null)
            {
                uiAtlas = GameDatabase.Instance.GetTexture("ContractConfigurator/ui/TrackingStation", false);
                activeWaypointEnabledSprite = UnityEngine.Sprite.Create(uiAtlas, new Rect(0, 88, 40, 40), new Vector2(0, 0));
                activeWaypointDisabledSprite = UnityEngine.Sprite.Create(uiAtlas, new Rect(0, 48, 40, 40), new Vector2(0, 0));
                offeredWaypointEnabledSprite = UnityEngine.Sprite.Create(uiAtlas, new Rect(40, 88, 40, 40), new Vector2(0, 0));
                offeredWaypointDisabledSprite = UnityEngine.Sprite.Create(uiAtlas, new Rect(40, 48, 40, 40), new Vector2(0, 0));
                activeOrbitEnabledSprite = UnityEngine.Sprite.Create(uiAtlas, new Rect(80, 88, 40, 40), new Vector2(0, 0));
                activeOrbitDisabledSprite = UnityEngine.Sprite.Create(uiAtlas, new Rect(80, 48, 40, 40), new Vector2(0, 0));
                offeredOrbitEnabledSprite = UnityEngine.Sprite.Create(uiAtlas, new Rect(0, 8, 40, 40), new Vector2(0, 0));
                offeredOrbitDisabledSprite = UnityEngine.Sprite.Create(uiAtlas, new Rect(40, 8, 40, 40), new Vector2(0, 0));
            }
        }

        public void Start()
        {
            LoggingUtil.LogVerbose(this, "Start");

            // Don't run if not in career
            if (!HighLogic.CurrentGame.scenarios.Any(sm => sm.moduleName == "ContractConfiguratorSettings"))
            {
                LoggingUtil.LogDebug(this, "Destroying TrackingStationUI - not in career mode.");
                Destroy(this);
                return;
            }

            // Get the last button in the list to use as a copy
            MapViewFiltering.FilterButton lastButton = MapViewFiltering.Instance.FilterButtons.Last();

            // Create our copies 
            activeWaypointButton = UnityEngine.Object.Instantiate(lastButton.button.gameObject);
            activeWaypointButton.name = "Button Active Waypoints";
            offeredWaypointButton = UnityEngine.Object.Instantiate(lastButton.button.gameObject);
            offeredWaypointButton.name = "Button Offered Waypoints";
            activeOrbitButton = UnityEngine.Object.Instantiate(lastButton.button.gameObject);
            activeOrbitButton.name = "Button Active Orbits";
            offeredOrbitButton = UnityEngine.Object.Instantiate(lastButton.button.gameObject);
            offeredOrbitButton.name = "Button Offered Orbits";

            // Fix z coordinates
            activeWaypointButton.GetComponent<RectTransform>().SetLocalPositionZ(750);
            offeredWaypointButton.GetComponent<RectTransform>().SetLocalPositionZ(750);
            activeOrbitButton.GetComponent<RectTransform>().SetLocalPositionZ(750);
            offeredOrbitButton.GetComponent<RectTransform>().SetLocalPositionZ(750);

            // Set up the tool-tips
            activeWaypointButton.GetComponent<TooltipController_Text>().textString = "Toggle Active Waypoints";
            offeredWaypointButton.GetComponent<TooltipController_Text>().textString = "Toggle Offered Waypoints";
            activeOrbitButton.GetComponent<TooltipController_Text>().textString = "Toggle Active Orbits";
            offeredOrbitButton.GetComponent<TooltipController_Text>().textString = "Toggle Offered Orbits";

            // Setup the button
            TrackingStationObjectButton activeWaypointTSButton = activeWaypointButton.GetComponent<TrackingStationObjectButton>();
            TrackingStationObjectButton offeredWaypointTSButton = offeredWaypointButton.GetComponent<TrackingStationObjectButton>();
            TrackingStationObjectButton activeOrbitTSButton = activeOrbitButton.GetComponent<TrackingStationObjectButton>();
            TrackingStationObjectButton offeredOrbitTSButton = offeredOrbitButton.GetComponent<TrackingStationObjectButton>();

            // Setup handlers
            activeWaypointTSButton.OnClickLeft.AddListener(new UnityAction(ActiveWaypointButtonClick));
            activeWaypointTSButton.OnClickRight.AddListener(new UnityAction(ActiveWaypointButtonClick));
            offeredWaypointTSButton.OnClickLeft.AddListener(new UnityAction(OfferedWaypointButtonClick));
            offeredWaypointTSButton.OnClickRight.AddListener(new UnityAction(OfferedWaypointButtonClick));
            activeOrbitTSButton.OnClickLeft.AddListener(new UnityAction(ActiveOrbitButtonClick));
            activeOrbitTSButton.OnClickRight.AddListener(new UnityAction(ActiveOrbitButtonClick));
            offeredOrbitTSButton.OnClickLeft.AddListener(new UnityAction(OfferedOrbitButtonClick));
            offeredOrbitTSButton.OnClickRight.AddListener(new UnityAction(OfferedOrbitButtonClick));

            // Setup sprites
            activeWaypointTSButton.spriteTrue = activeWaypointEnabledSprite;
            activeWaypointTSButton.spriteFalse = activeWaypointDisabledSprite;
            offeredWaypointTSButton.spriteTrue = offeredWaypointEnabledSprite;
            offeredWaypointTSButton.spriteFalse = offeredWaypointDisabledSprite;
            activeOrbitTSButton.spriteTrue = activeOrbitEnabledSprite;
            activeOrbitTSButton.spriteFalse = activeOrbitDisabledSprite;
            offeredOrbitTSButton.spriteTrue = offeredOrbitEnabledSprite;
            offeredOrbitTSButton.spriteFalse = offeredOrbitDisabledSprite;

            // Set defaults
            activeWaypointButton.GetComponent<TrackingStationObjectButton>().SetState(ContractConfiguratorSettings.Instance.DisplayActiveWaypoints);
            offeredWaypointButton.GetComponent<TrackingStationObjectButton>().SetState(ContractConfiguratorSettings.Instance.DisplayOfferedWaypoints);
            activeOrbitButton.GetComponent<TrackingStationObjectButton>().SetState(ContractConfiguratorSettings.Instance.DisplayActiveOrbits);
            offeredOrbitButton.GetComponent<TrackingStationObjectButton>().SetState(ContractConfiguratorSettings.Instance.DisplayOfferedOrbits);

            // Disable counts
            activeWaypointButton.GetChild("Count").SetActive(false);
            offeredWaypointButton.GetChild("Count").SetActive(false);
            activeOrbitButton.GetChild("Count").SetActive(false);
            offeredOrbitButton.GetChild("Count").SetActive(false);

            // Re-parent
            GameObject trackingFilters = lastButton.button.transform.parent.gameObject;
            RectTransform trackingFiltersRect = trackingFilters.GetComponent<RectTransform>();
            trackingFiltersRect.sizeDelta = new Vector2(trackingFiltersRect.sizeDelta.x + 44 * 2, trackingFiltersRect.sizeDelta.y);
            activeWaypointButton.transform.SetParent(trackingFilters.transform);
            offeredWaypointButton.transform.SetParent(trackingFilters.transform);
            activeOrbitButton.transform.SetParent(trackingFilters.transform);
            offeredOrbitButton.transform.SetParent(trackingFilters.transform);
            activeWaypointButton.SetActive(true);
            offeredWaypointButton.SetActive(true);
            activeOrbitButton.SetActive(true);
            offeredOrbitButton.SetActive(true);

            LoggingUtil.LogVerbose(this, "Finished setup");
        }

        protected void ActiveWaypointButtonClick()
        {
            LoggingUtil.LogVerbose(this, "ActiveWaypointButtonClick");

            // Flip the toggle
            ContractConfiguratorSettings.Instance.DisplayActiveWaypoints = !ContractConfiguratorSettings.Instance.DisplayActiveWaypoints;

            // Fire the filter modified event
            GameEvents.OnMapViewFiltersModified.Fire(MapViewFiltering.VesselTypeFilter.None);
        }

        protected void OfferedWaypointButtonClick()
        {
            LoggingUtil.LogVerbose(this, "OfferedWaypointButtonClick");

            // Flip the toggle
            ContractConfiguratorSettings.Instance.DisplayOfferedWaypoints = !ContractConfiguratorSettings.Instance.DisplayOfferedWaypoints;

            // Fire the filter modified event
            GameEvents.OnMapViewFiltersModified.Fire(MapViewFiltering.VesselTypeFilter.None);
        }

        protected void ActiveOrbitButtonClick()
        {
            LoggingUtil.LogVerbose(this, "ActiveOrbitButtonClick");

            // Flip the toggle
            ContractConfiguratorSettings.Instance.DisplayActiveOrbits = !ContractConfiguratorSettings.Instance.DisplayActiveOrbits;

            // Fire the filter modified event
            GameEvents.OnMapViewFiltersModified.Fire(MapViewFiltering.VesselTypeFilter.None);
        }

        protected void OfferedOrbitButtonClick()
        {
            LoggingUtil.LogVerbose(this, "OfferedOrbitButtonClick");

            // Flip the toggle
            ContractConfiguratorSettings.Instance.DisplayOfferedOrbits = !ContractConfiguratorSettings.Instance.DisplayOfferedOrbits;

            // Fire the filter modified event
            GameEvents.OnMapViewFiltersModified.Fire(MapViewFiltering.VesselTypeFilter.None);
        }
    }
}
