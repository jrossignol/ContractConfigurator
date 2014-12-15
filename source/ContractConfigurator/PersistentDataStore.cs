using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;

namespace ContractConfigurator
{
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames,
        GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER)]
    class PersistentDataStore : ScenarioModule
    {
        static public PersistentDataStore Instance { get; private set; }
        private Dictionary<string, System.Object> data = new Dictionary<string, System.Object>();

        public PersistentDataStore()
        {
            Instance = this;
        }

        /*
         * Call this to store a key/value pair into the persistant data store.  Only basic
         * value types and strings are supported.
         */
        public void Store<T>(string key, T value) where T : struct
        {
            Type type = value.GetType();
            if (type != typeof(bool) &&
                type != typeof(float) &&
                type != typeof(double) &&
                type != typeof(sbyte) &&
                type != typeof(byte) &&
                type != typeof(char) &&
                type != typeof(short) &&
                type != typeof(ushort) &&
                type != typeof(int) &&
                type != typeof(uint) &&
                type != typeof(long) &&
                type != typeof(ulong) &&
                type != typeof(string))
            {
                throw new ArgumentException("ContractConfigurator: Supplied value must be of a simple value type.", "value");
            }
            data[key] = value;
        }

        /*
         * Call this to retrieve a previously stored value from the persistant data store.
         */
        public T Retrieve<T>(string key) where T : struct
        {
            if (!data.ContainsKey(key))
            {
                return new T();
            }
            return (T)data[key];
        }

        public override void OnLoad(ConfigNode node)
        {
 	        base.OnLoad(node);

            ConfigNode child = node.GetNode("DATA");
            if (child != null)
            {
                foreach (ConfigNode.Value pair in child.values)
                {
                    string typeName = pair.value.Remove(pair.value.IndexOf(":"));
                    string value = pair.value.Substring(typeName.Length + 1, pair.value.Length - typeName.Length - 1);
                    Type type = Type.GetType(typeName);
                    if (type == typeof(string))
                    {
                        data[pair.name] = pair.value;
                    }
                    else
                    {
                        data[pair.name] = type.InvokeMember("Parse", System.Reflection.BindingFlags.InvokeMethod, null, null, new string[] { value });
                    }
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
 	        base.OnSave(node);

            ConfigNode child = new ConfigNode("DATA");
            node.AddNode(child);

            foreach (KeyValuePair<string, System.Object> p in data)
            {
                child.AddValue(p.Key, p.Value.GetType() + ":" + p.Value);
            }
        }
    }
}
