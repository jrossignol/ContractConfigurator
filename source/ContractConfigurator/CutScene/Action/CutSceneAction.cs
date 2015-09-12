using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ContractConfigurator.CutScene
{
    /// <summary>
    /// A generic cut scene action.
    /// </summary>
    public abstract class CutSceneAction
    {
        public bool async = false;
        public CutSceneExecutor executor;
        public CutSceneDefinition cutSceneDefinition;

        /// <summary>
        /// Invoke the action required.  This should return fairly quickly, and any logic that
        /// needs to happen over time can be done in a coroutine (added to the executor object) or
        /// the Update() or FixedUpdate() methods.
        /// </summary>
        public abstract void InvokeAction();

        /// <summary>
        /// Checks whether this action is complete and the next action can be moved on to.  If
        /// isAsync is true, then this is never called.
        /// </summary>
        /// <returns>Whether this action is considered complete.</returns>
        public abstract bool ReadyForNextAction();

        /// <summary>
        /// Called from a Unity FixedUpdate function.
        /// </summary>
        public virtual void FixedUpdate()
        {
        }

        /// <summary>
        /// Called from a Unity Update function.
        /// </summary>
        public virtual void Update()
        {
        }

        /// <summary>
        /// Called from the Unity OnDestroy function.
        /// </summary>
        public virtual void OnDestroy()
        {
        }

        public virtual void OnSave(ConfigNode configNode)
        {
            if (async)
            {
                configNode.AddValue("async", async);
            }
        }

        public virtual void OnLoad(ConfigNode configNode)
        {
            async = configNode.HasValue("async") && ConfigNodeUtil.ParseValue<bool>(configNode, "async");
        }

    }
}
