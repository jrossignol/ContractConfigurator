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
    public class DataStoreCastException : InvalidCastException
    {
        public Type FromType { get; private set; }
        public Type ToType { get; private set; }

        public DataStoreCastException(Type fromType, Type toType)
            : this(fromType, toType, null)
        {
        }

        public DataStoreCastException(Type fromType, Type toType, Exception inner)
            : base("Cannot cast from " + fromType + " to " + toType + ".", inner)
        {
            FromType = fromType;
            ToType = toType;
        }
    }

    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames,
        GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER)]
    class PersistentDataStore : ScenarioModule
    {
        static public PersistentDataStore Instance { get; private set; }
        private Dictionary<string, System.Object> data = new Dictionary<string, System.Object>();
        private Dictionary<string, ConfigNode> configNodes = new Dictionary<string, ConfigNode>();

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
         * Call this to store an entire config node into the persistant data store.
         */
        public void Store(ConfigNode node)
        {
            configNodes[node.name] = node;
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
            try
            {
                return (T)data[key];
            }
            catch (InvalidCastException)
            {
                throw new DataStoreCastException(data[key].GetType(), typeof(T));
            }
        }

        /*
         * Call this to retrieve a previously stored config node from the persistant data store.
         */
        public ConfigNode Retrieve(string key)
        {
            if (!configNodes.ContainsKey(key))
            {
                return new ConfigNode();
            }
            return configNodes[key];
        }

        public override void OnLoad(ConfigNode node)
        {
 	        base.OnLoad(node);

            ConfigNode dataNode = node.GetNode("DATA");
            if (dataNode != null)
            {
                // Handle individual values
                foreach (ConfigNode.Value pair in dataNode.values)
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

                // Handle config nodes
                foreach (ConfigNode childNode in dataNode.GetNodes())
                {
                    configNodes[childNode.name] = childNode;
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
 	        base.OnSave(node);

            ConfigNode dataNode = new ConfigNode("DATA");
            node.AddNode(dataNode);

            // Handle individual values
            foreach (KeyValuePair<string, System.Object> p in data)
            {
                dataNode.AddValue(p.Key, p.Value.GetType() + ":" + p.Value);
            }

            // Handle config nodes
            foreach (ConfigNode childNode in configNodes.Values)
            {
                dataNode.AddNode(childNode);
            }
        }
    }
}
