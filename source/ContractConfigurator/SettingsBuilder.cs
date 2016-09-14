using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using KSP;

namespace ContractConfigurator
{
    public class SettingsBuilder : GameParameters.CustomParameterNode
    {
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER; } }
        public override bool HasPresets { get { return false; } }
        public override string Section { get { return "Contract Configurator"; } }
        public override int SectionOrder { get { return 1; } }
        public override string Title { get { return "Contract Groups"; } }

        public static void EmitSettings()
        {
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName("ContractConfiguratorDynamic"), AssemblyBuilderAccess.ReflectionOnly);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("ContractConfiguratorDynamicModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("ContractConfigurator.ContractConfiguratorParameters",
                TypeAttributes.Public | TypeAttributes.Class, typeof(SettingsBuilder));

            // Attribute constructor
            ConstructorInfo paramUICons = typeof(GameParameters.CustomParameterUI).GetConstructor(new Type[] { typeof(string) });

            // Define a field for each Group
            foreach (ContractGroup contractGroup in ContractGroup.AllGroups.Where(g => g != null && g.parent == null).OrderBy(g => g == null ? "ZZZ" : g.name))
            {
                FieldBuilder groupField = typeBuilder.DefineField(contractGroup.name, typeof(bool), FieldAttributes.Public);

                CustomAttributeBuilder attBuilder = new CustomAttributeBuilder(paramUICons, new object[] { contractGroup.displayName });
                groupField.SetCustomAttribute(attBuilder);
            }

            typeBuilder.CreateType();
        }
    }
}
