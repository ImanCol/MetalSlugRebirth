using BepInEx;
using BepInEx.Logging;
using System.Reflection;

namespace AssemblyLoader
{
    public class AssemblyLoader
    {
        private const string DirectoryName = "MSLoader";
        private const string DllName = "MSLoader.dll";

        private const string MainMethod = "Main";
        private const string UnloadMethod = "Unload";

        private readonly string assemblyPath;
        private readonly ManualLogSource log;

        private FileSystemWatcher fileSystemWatcher;
        private Assembly previousAssembly;
        private string previousAssemblyHash;

        public AssemblyLoader(ManualLogSource log)
        {
            this.log = log;

            string assemblyDirectory = Path.Combine(Paths.GameRootPath, DirectoryName);
            assemblyPath = Path.Combine(assemblyDirectory, DllName);

            fileSystemWatcher = new FileSystemWatcher(assemblyDirectory);
            fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            fileSystemWatcher.Filter = DllName;
            fileSystemWatcher.Changed += OnAssemblyFileUpdated;
            fileSystemWatcher.Created += OnAssemblyFileUpdated;
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            fileSystemWatcher.EnableRaisingEvents = false;
            fileSystemWatcher.Dispose();
            fileSystemWatcher = null;

            log.LogInfo("FileSystemWatcher disposed.");
            InvokeMethod(previousAssembly, UnloadMethod);
            previousAssembly = null;
            previousAssemblyHash = null;
            Task.Yield();
        }

        private void OnAssemblyFileUpdated(object sender, FileSystemEventArgs args)
        {
            if (args.FullPath != assemblyPath)
            {
                Task.Yield();
                return;
            }

            log.LogInfo($"File {Path.GetFileName(args.FullPath)} {args.ChangeType}. Reloading...");
            Thread.Sleep(3000);
            byte[] bytes = File.ReadAllBytes(assemblyPath);
            string newHash = Utils.Md5FromBytes(bytes);
            log.LogInfo($"Hash: {newHash}");
            if (newHash == previousAssemblyHash)
            {
                log.LogInfo("File not changed.");
                Task.Yield();
                return;
            }

            previousAssemblyHash = newHash;
            log.LogWarning("assembly hash changed, unloading previous assembly.");
            InvokeMethod(previousAssembly, UnloadMethod);
            Task.Yield();
            previousAssembly = Assembly.Load(bytes);
            log.LogInfo("Invoking assembly main method.");
            InvokeMethod(previousAssembly, MainMethod, log);
            Task.Yield();
        }

        private void InvokeMethod(Assembly assembly, string methodName, params object[] parameters)
        {
            if (assembly == null)
            {
                Task.Yield();
                return;
            }

            int paramLength = parameters?.Length ?? 0;
            try
            {
                foreach (Type type in assembly.GetTypes())
                {
                    MethodInfo method = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                        .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == paramLength);

                    if (method == null)
                    {
                        continue;
                    }

                    var methodParams = method.GetParameters();
                    bool paramsMatch = true;
                    for (int i = 0; i < methodParams.Length; i++)
                    {
                        if (!methodParams[i].ParameterType.IsAssignableFrom(parameters[i].GetType()))
                        {
                            paramsMatch = false;
                            break;
                        }
                    }

                    if (!paramsMatch)
                    {
                        continue;
                    }

                    log.LogInfo($"Running method {method.Name}");
                    method.Invoke(null, parameters);
                    break;
                }
                log.LogInfo($"Invoking method {methodName} with {paramLength} parameters.");
            }
            catch (Exception ex)
            {
                log.LogError($"Error invoking method {methodName}: {ex}");
            }
            Task.Yield();
        }
    }
}
