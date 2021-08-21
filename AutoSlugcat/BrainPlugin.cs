using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace SlugBrain
{
    [BepInPlugin("casheww.slugbrain", "SlugBrain", "0.1.0")]
    class BrainPlugin : BaseUnityPlugin
    {
        void OnEnable()
        {
            _Logger = Logger;

            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            File.WriteAllText(logPath, "");
            Log($"SlugBrain started! casheww was ere \t{DateTime.Now}\n");

            InputSpoofer = new InputSpoofer();
            TextManager = new TextManager.DebugTextManager();

            Hooks.Enable();
        }

        void OnDisable()
        {
            Hooks.Disable();
        }

        void Update()
        {
            TextManager.Update();

            if (Input.GetKeyDown(KeyCode.PageDown)) debugAI = !debugAI;
            if (Input.GetKeyDown(KeyCode.End)) debugTerrainAndSlopes = !debugTerrainAndSlopes;
        }

        public static void Log(object message, bool bepLog = true, bool error = false)
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

        public static TextManager.DebugTextManager TextManager { get; private set; }

        const string logDir = "./Mods/SlugBrain";
        const string logPath = logDir + "/log.txt";

        public static bool debugAI = true;
        public static bool debugTerrainAndSlopes = false;

    }
}
