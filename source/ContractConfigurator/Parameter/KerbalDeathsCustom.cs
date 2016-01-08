using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Custom implementation of the KerbalDeaths parameter
    /// </summary>
    public class KerbalDeathsCustom : KerbalDeaths
    {
        string title;

        public KerbalDeathsCustom()
            : base()
        {
        }

        public KerbalDeathsCustom(int countMax, string title)
            : base(countMax)
        {
            this.title = title;
        }

        protected override string GetTitle()
        {
            return string.IsNullOrEmpty(title) ? base.GetTitle() : title;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            if (!string.IsNullOrEmpty(title))
            {
                node.AddValue("title", title);
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            title = ConfigNodeUtil.ParseValue<string>(node, "title", "");
        }
    }
}
