using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;
using ContractConfigurator.ExpressionParser;
namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// BehaviourFactory wrapper for CopyCraftFile ContractBehaviour.
    /// </summary>
    public class CopyCraftFileFactory : BehaviourFactory
    {
        protected string url;
        protected EditorFacility craftType;
        protected TriggeredBehaviour.State onState;
        protected List<string> parameter = new List<string>();

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "url", x => url = x, this, ValidateURL);
            valid &= ConfigNodeUtil.ParseValue<EditorFacility>(configNode, "craftType", x => craftType = x, this);
            valid &= ConfigNodeUtil.ParseValue<TriggeredBehaviour.State>(configNode, "onState", x => onState = x, this, TriggeredBehaviour.State.CONTRACT_SUCCESS);
            if (onState == TriggeredBehaviour.State.PARAMETER_COMPLETED || onState == TriggeredBehaviour.State.PARAMETER_FAILED)
            {
                valid &= ConfigNodeUtil.ParseValue<List<string>>(configNode, "parameter", x => parameter = x, this);
            }

            return valid;
        }

        public static bool ValidateURL(string url)
        {
            string[] pathComponents = new string[] { KSPUtil.ApplicationRootPath, "GameData" }.Concat(url.Split("/".ToCharArray())).ToArray();
            string path = string.Join(Path.DirectorySeparatorChar.ToString(), pathComponents);
            if (!File.Exists(path))
            {
                throw new ArgumentException("File could not be found at path '" + path + "'.");
            }
            return true;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new CopyCraftFile(url, craftType, onState, parameter);
        }
    }
}
