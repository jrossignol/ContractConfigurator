using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ContractConfigurator.Util;

namespace ContractConfigurator.RemoteTech
{
    public static class Util
    {
        public static Assembly RemoteTechAssembly;

        /// <summary>
        /// Verifies that the RemoteTech version the player has is compatible.
        /// </summary>
        /// <returns>Whether the check passed.</returns>
        public static bool VerifyRemoteTechVersion()
        {
            string minVersion = "1.6.2";
            if (RemoteTechAssembly == null)
            {
                RemoteTechAssembly = Version.VerifyAssemblyVersion("RemoteTech", minVersion);
            }
            return RemoteTechAssembly != null;
        }
    }
}
