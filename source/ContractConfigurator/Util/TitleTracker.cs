using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using KSP.UI;
using KSP.UI.Screens;
using Contracts;
using Contracts.Parameters;
using UnityEngine.UI;

namespace ContractConfigurator
{
    /// <summary>
    /// Class for tracking and changing parameter titles in the stock contract app
    /// </summary>
    public class TitleTracker
    {
        static FieldInfo contractsField = typeof(ContractsApp).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(mi => mi.FieldType == typeof(Dictionary<Guid, UICascadingList.CascadingListItem>)).First();
        private Dictionary<Guid, UICascadingList.CascadingListItem> uiListMap = null;
        private List<string> titles = new List<string>();

        /// <summary>
        /// Add a title to the TitleTracker to be tracked.  Call this in GetTitle() before returning a new string.
        /// </summary>
        /// <param name="title">The title to add</param>
        public void Add(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return;
            }
            titles.AddUnique(title);
        }

        /// <summary>
        /// Call this any time the title text has changed - this will make an attempt to update
        /// the contract window title.  We do this because otherwise the window will only ever read
        /// the title once.
        /// </summary>
        /// <param name="newTitle">New title to display</param>
        public void UpdateContractWindow(ContractParameter param, string newTitle)
        {
            if (uiListMap == null)
            {
                uiListMap = (Dictionary<Guid, UICascadingList.CascadingListItem>)contractsField.GetValue(ContractsApp.Instance);
            }

            // Get the cascading list for our contract
            UICascadingList.CascadingListItem list = uiListMap.ContainsKey(param.Root.ContractGuid) ? uiListMap[param.Root.ContractGuid] : null;
            if (list != null)
            {
                foreach (KSP.UI.UIListItem item in list.items)
                {
                    Text text = item.GetComponentsInChildren<Text>(true).FirstOrDefault();
                    if (text != null)
                    {
                        // Check for any string in titleTracker
                        string found = null;
                        foreach (string title in titles)
                        {
                            if (text.text.EndsWith(title + "</color>"))
                            {
                                found = title;
                                break;
                            }
                        }

                        // Clear the titleTracker, and replace the text
                        if (found != null)
                        {
                            titles.Clear();
                            text.text = text.text.Replace(found, newTitle);
                            titles.Add(newTitle);
                        }
                    }
                }
            }

            // Contracts Window + update
            ContractsWindow.SetParameterTitle(param, newTitle);
        }
    }
}
