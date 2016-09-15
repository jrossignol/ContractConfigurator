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
    public class CommonTemplate : GameParameters.CustomParameterNode
    {
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER; } }
        public override bool HasPresets { get { return false; } }
        public override string Section { get { return "Contract Configurator"; } }
        public override int SectionOrder { get { return 1; } }
        public override string Title { get { return "TODO - remove"; } }

        public bool IsEnabled(string name)
        {
            FieldInfo fi = GetType().GetField(name);
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

        [GameParameters.CustomParameterUI("Display Offered Orbits")]
        public bool DisplayOfferedOrbits = ContractDefs.DisplayOfferedOrbits;

        [GameParameters.CustomParameterUI("Display Active Orbits")]
        public bool DisplayActiveOrbits = true;

        [GameParameters.CustomParameterUI("Display Offered Waypoints")]
        public bool DisplayOfferedWaypoints = ContractDefs.DisplayOfferedWaypoints;

        [GameParameters.CustomParameterUI("Display Active Waypoints")]
        public bool DisplayActiveWaypoints = true;

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

    public class ContractGroupParametersTemplate : CommonTemplate
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

    public class StockContractParametersTemplate : CommonTemplate
    {
        public override int SectionOrder { get { return 2; } }
        public override string Title { get { return "Stock Contracts"; } }

        public StockContractParametersTemplate()
        {
            SeedStockContractDetails();
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

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            foreach (Type subclass in ContractConfigurator.GetAllTypes<Contract>().Where(t => t != null && !t.Name.StartsWith("ConfiguredContract")))
            {
                FieldInfo fi = GetType().GetField(subclass.Name);
                if (fi != null)
                {
                    bool val = (bool)fi.GetValue(this);
                    ContractDisabler.SetContractState(subclass, val);
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
                FieldBuilder groupField = groupParamBuilder.DefineField(contractGroup.name, typeof(bool), FieldAttributes.Public);

                CustomAttributeBuilder attBuilder = new CustomAttributeBuilder(paramUICons, new object[] { contractGroup.displayName });
                groupField.SetCustomAttribute(attBuilder);
            }

            // Create the stock contracts settings page
            TypeBuilder stockParamBuilder = moduleBuilder.DefineType("ContractConfigurator.StockContractParametersTemplate",
                TypeAttributes.Public | TypeAttributes.Class, typeof(StockContractParametersTemplate));

            // Define a field for each contract type
            foreach (Type subclass in contractTypes)
            {
                FieldBuilder groupField = stockParamBuilder.DefineField(subclass.Name, typeof(bool), FieldAttributes.Public);

                CustomAttributeBuilder attBuilder = new CustomAttributeBuilder(paramUICons, new object[] { new MissionControlUI.GroupContainer(subclass).DisplayName() });
                groupField.SetCustomAttribute(attBuilder);
            }

            // Finalize the types
            GroupParametersType = groupParamBuilder.CreateType();
            StockParametersType = stockParamBuilder.CreateType();

            // Hack the assembly into the assembly loader so it gets picked up
            AssemblyLoader.loadedAssemblies.Add(
                new AssemblyLoader.LoadedAssembly(assemblyBuilder, "not_a_real_path.dll", "not_a_real_url.dll", new ConfigNode()));
        }
    }
}
