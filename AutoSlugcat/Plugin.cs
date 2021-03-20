using System;
using System.IO;
using BepInEx;

namespace AutoSlugcat
{
    [BepInPlugin("casheww.autoslugcat", "AutoSlugcat", "0.1.1")]
    public class Plugin : BaseUnityPlugin
    {
        void OnEnable()
        {
            File.WriteAllText(logPath, "");
            Log($"AutoSlugcat started! {DateTime.Now}\n");

            Manager = new PathManager(pathPath, debugRooms: false);
            PlayerHooks.Apply();
        }

        void OnDisable()
        {
           PlayerHooks.UnApply();
        }

        public static void Log(object message, bool unityLog = false)
        {
            using (StreamWriter sw = File.AppendText(logPath))
            {
                sw.WriteLine(message.ToString());
            }
            
            if (unityLog)
            {
                UnityEngine.Debug.Log($"AutoSlugcat : {message}");
            }
        }

        public static PathManager Manager { get; private set; }

        const string logPath = "./Mods/AutoSlugcatStuff/log.txt";
        const string pathPath = "./Mods/AutoSlugcatStuff/pathing.txt";
    }
}
