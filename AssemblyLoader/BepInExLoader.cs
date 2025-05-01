using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace AssemblyLoader
{

    [BepInPlugin("BepInEx.IL2CPP.AssemblyLoader", "AssemblyLoader", "1.0.0")]
    public class BepInExLoader : BasePlugin
    {

        //fix lastest version?
        private AssemblyLoader assemblyLoader;

        public override void Load()
        {
            Log.LogInfo($"Plugin AssemblyLoader is loaded!");

            Bootstrap.Log = Log;

            //fix lastest version?
            assemblyLoader = new AssemblyLoader(Log);

            //AddComponent<Bootstrap>();
        }
    }
}