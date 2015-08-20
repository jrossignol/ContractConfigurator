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
        protected CopyCraftFile.Condition condition;
        protected string parameter;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "url", x => url = x, this, ValidateURL);
            valid &= ConfigNodeUtil.ParseValue<EditorFacility>(configNode, "craftType", x => craftType = x, this);
            valid &= ConfigNodeUtil.ParseValue<CopyCraftFile.Condition>(configNode, "condition", x => condition = x, this, CopyCraftFile.Condition.CONTRACT_COMPLETED);
            if (condition == CopyCraftFile.Condition.PARAMETER_COMPLETED || condition == CopyCraftFile.Condition.PARAMETER_FAILED)
            {
                valid &= ConfigNodeUtil.ParseValue<string>(configNode, "parameter", x => parameter = x, this);
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
            return new CopyCraftFile(url, craftType, condition, parameter);
        }
    }
}
