using System;
using System.IO;
using BepInEx;

namespace SlugBrain
{
    [BepInPlugin("casheww.slugbrain", "SlugBrain", "0.1.0")]
    class BrainPlugin : BaseUnityPlugin
    {
        void OnEnable()
        {
            _Logger = Logger;

            File.WriteAllText(logPath, "");
            InputSpoofer = new InputSpoofer();
            Log($"AutoSlugcat started! {DateTime.Now}\n");

            Hooks.Enable();
        }

        void OnDisable()
        {
            Hooks.Disable();
        }

        public static void Log(object message, bool bepLog = false, bool error = false)
        {
            using (StreamWriter sw = File.AppendText(logPath))
            {
                sw.WriteLine(message.ToString());
            }
            
            if (bepLog)
            {
                _Logger.Log(error ? BepInEx.Logging.LogLevel.Error : BepInEx.Logging.LogLevel.Message, message);
            }
        }


        public static BepInEx.Logging.ManualLogSource _Logger { get; private set; }
        public static InputSpoofer InputSpoofer { get; private set; }

        const string logPath = "./Mods/AutoSlugcatStuff/log.txt";
        const string pathPath = "./Mods/AutoSlugcatStuff/pathing.txt";
    }
}
