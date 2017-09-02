using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using KSP;
using Contracts;
using FinePrint;
using ContractConfigurator.Util;

namespace ContractConfigurator
{
    public abstract class CommonTemplate : GameParameters.CustomParameterNode
    {
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER; } }
        public override bool HasPresets { get { return false; } }
        public override string Section { get { return "Contract Configurator"; } }
        public override string DisplaySection { get { return Section; } }

        public bool IsEnabled(string name)
        {
            FieldInfo fi = GetType().GetField(SettingsBuilder.SanitizeName(name));
            if (fi != null)
            {
                return (bool)fi.GetValue(this);
            }
            return false;
        }
    }

    public class ContractConfiguratorParameters : CommonTemplate
    {
        public override int SectionOrder { get { return 0; } }
        public override string Title { get { return "Settings"; } }

        public bool DisplayOfferedOrbits = ContractDefs.DisplayOfferedOrbits;
        public bool DisplayActiveOrbits = true;
        public bool DisplayOfferedWaypoints = ContractDefs.DisplayOfferedWaypoints;
        public bool DisplayActiveWaypoints = true;

        [GameParameters.CustomFloatParameterUI("Active Contract Multiplier", displayFormat = "F2", minValue = 0.1f, maxValue = 2.0f, stepCount = 20,
            toolTip = "Multiplier applied to the active contract limits.")]
        public float ActiveContractMultiplier = 1.0f;

        public enum MissionControlButton
        {
            All,
            Available,
            Active,
            Archive
        }

        public MissionControlButton lastMCButton = MissionControlButton.All;

        public override void OnSave(ConfigNode node)
        {
            node.AddValue("lastMCButton", lastMCButton);
        }

        public override void OnLoad(ConfigNode node)
        {
            lastMCButton = ConfigNodeUtil.ParseValue<MissionControlButton>(node, "lastMCButton", MissionControlButton.All);
        }
    }

    public abstract class ContractGroupParametersTemplate : CommonTemplate
    {
        public override int SectionOrder { get { return 1; } }
        public override string Title { get { return "Contract Groups"; } }

        public ContractGroupParametersTemplate()
        {
            // Default everything to on
            foreach (FieldInfo fi in GetType().GetFields())
            {
                if (fi.FieldType == typeof(bool))
                {
                    fi.SetValue(this, true);
                }
            }
        }
    }

    public abstract class StockContractParametersTemplate : CommonTemplate
    {
        public override int SectionOrder { get { return 2; } }
        public override string Title { get { return "Stock Contracts"; } }

        private List<FieldInfo> contractFields = new List<FieldInfo>();

        public StockContractParametersTemplate()
        {
            foreach (FieldInfo fi in GetType().GetFields())
            {
                if (fi.FieldType == typeof(bool))
                {
                    contractFields.Add(fi);
                    fi.SetValue(this, true);
                }
            }

            DisableContracts();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            foreach (Type subclass in ContractConfigurator.GetAllTypes<Contract>().Where(t => t != null && !t.Name.StartsWith("ConfiguredContract")))
            {
                FieldInfo fi = GetType().GetField(SettingsBuilder.SanitizeName(subclass.Name));
                if (fi != null)
                {
                    bool val = (bool)fi.GetValue(this);
                    ContractDisabler.SetContractState(subclass, val);
                }
            }
        }


        /// <summary>
        /// Disables standard contract types as requested by contract packs.
        /// </summary>
        /// <returns>True if the disabling is done.</returns>
        public bool DisableContracts()
        {
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("CONTRACT_CONFIGURATOR");

            int disabledCounter = 0;
            SeedStockContractDetails();

            // Start disabling via legacy method
            Dictionary<string, Type> contractsToDisable = new Dictionary<string, Type>();
            foreach (ConfigNode node in nodes)
            {
                foreach (string contractType in node.GetValues("disabledContractType"))
                {
                    SetContractToDisabled(contractType);
                    disabledCounter++;
                }
            }

            // Disable via new method
            foreach (ContractGroup contractGroup in ContractGroup.AllGroups.Where(g => g != null && g.parent == null))
            {
                foreach (string contractType in contractGroup.disabledContractType)
                {
                    SetContractToDisabled(contractType);
                    disabledCounter++;
                }
            }

            return true;
        }

        private void SeedStockContractDetails()
        {
            // Default everything to on
            foreach (FieldInfo fi in GetType().GetFields())
            {
                if (fi.FieldType == typeof(bool))
                {
                    fi.SetValue(this, true);
                }
            }
        }

        private void SetContractToDisabled(string contractType)
        {
            foreach (FieldInfo fi in contractFields)
            {
                if (fi.Name == SettingsBuilder.SanitizeName(contractType))
                {
                    fi.SetValue(this, false);
                }
            }
        }
    }

    public class SettingsBuilder
    {
        public static Type GroupParametersType = null;
        public static Type StockParametersType = null;

        public static void EmitSettings()
        {
            // Do this before we start creating assemblies
            List<Type> contractTypes = ContractConfigurator.GetAllTypes<Contract>().Where(t => t != null && !t.Name.StartsWith("ConfiguredContract")).ToList();

            // Create the assembly
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("ContractConfiguratorDynamic"), AssemblyBuilderAccess.ReflectionOnly);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("ContractConfiguratorDynamicModule");

            // Attribute constructor
            ConstructorInfo paramUICons = typeof(GameParameters.CustomParameterUI).GetConstructor(new Type[] { typeof(string) });

            // Create the contract group settings page
            TypeBuilder groupParamBuilder = moduleBuilder.DefineType("ContractConfigurator.ContractGroupParameters",
                TypeAttributes.Public | TypeAttributes.Class, typeof(ContractGroupParametersTemplate));

            // Define a field for each Group
            foreach (ContractGroup contractGroup in ContractGroup.AllGroups.Where(g => g != null && g.parent == null).OrderBy(g => g.displayName))
            {
                FieldBuilder groupField = groupParamBuilder.DefineField(SettingsBuilder.SanitizeName(contractGroup.name), typeof(bool), FieldAttributes.Public);

                CustomAttributeBuilder attBuilder = new CustomAttributeBuilder(paramUICons, new object[] { contractGroup.displayName });
                groupField.SetCustomAttribute(attBuilder);
            }

            // Create the stock contracts settings page
            TypeBuilder stockParamBuilder = moduleBuilder.DefineType("ContractConfigurator.StockContractParametersTemplate",
                TypeAttributes.Public | TypeAttributes.Class, typeof(StockContractParametersTemplate));

            // Define a field for each contract type
            foreach (MissionControlUI.GroupContainer container in contractTypes.Select(t => new MissionControlUI.GroupContainer(t)).OrderBy(mcui => mcui.DisplayName()))
            {
                FieldBuilder groupField = stockParamBuilder.DefineField(SettingsBuilder.SanitizeName(container.stockContractType.Name), typeof(bool), FieldAttributes.Public);

                CustomAttributeBuilder attBuilder = new CustomAttributeBuilder(paramUICons, new object[] { container.DisplayName() });
                groupField.SetCustomAttribute(attBuilder);
            }

            // Finalize the types
            GroupParametersType = groupParamBuilder.CreateType();
            StockParametersType = stockParamBuilder.CreateType();

            // Add the types into the custom parameter list so they get picked up
            GameParameters.ParameterTypes.Add(GroupParametersType);
            GameParameters.ParameterTypes.Add(StockParametersType);
        }

        private static Dictionary<string, string> sanitization = new Dictionary<string, string>();
        public static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            string sanitized;
            if (!sanitization.TryGetValue(name, out sanitized))
            {
                sanitized = new String(name.Where(Char.IsLetterOrDigit).ToArray());
                sanitization[name] = sanitized;
            }
            return sanitized;
        }

    }
}
