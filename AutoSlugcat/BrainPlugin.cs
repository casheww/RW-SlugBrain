using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace SlugBrain
{
    [BepInPlugin("casheww.slugbrain", "SlugBrain", "0.1.1")]
    class BrainPlugin : BaseUnityPlugin
    {
        private void OnEnable()
        {
            Instance = this;
                
            _Logger = Logger;
            if (!Directory.Exists(_logDir)) Directory.CreateDirectory(_logDir);
            File.WriteAllText(_logPath, "");
            Log($"SlugBrain started! casheww was ere \t{DateTime.Now}\n");

            InputSpoofer = new InputSpoofer();
            TextManager = new DebuggingHelpers.DebugTextManager();
            NodeManager = new DebuggingHelpers.DebugNodeManager();
            
            Hooks.Enable();
        }

        private void OnDisable()
        {
            Instance = default;
            Hooks.Disable();
        }

        private void Update()
        {
            TextManager.Update();
            NodeManager.Update();

            if (Input.GetKeyDown(KeyCode.PageDown)) debugAI = !debugAI;
            if (Input.GetKeyDown(KeyCode.End)) debugTerrainAndSlopes = !debugTerrainAndSlopes;
        }

        public static void Log(object message, bool bepLog = true, bool warning = false, bool error = false)
        {
            using (StreamWriter sw = File.AppendText(_logPath))
            {
                sw.WriteLine(message ?? "null");
            }

            if (bepLog)
            {
                LogLevel level = error ? LogLevel.Error : (warning ? LogLevel.Warning : LogLevel.Message);
                _Logger.Log(level, message ?? "null");
            }
        }


        public static BrainPlugin Instance { get; private set; }
        public static ManualLogSource _Logger { get; private set; }
        public static InputSpoofer InputSpoofer { get; private set; }

        public static DebuggingHelpers.DebugTextManager TextManager { get; private set; }
        public static DebuggingHelpers.DebugNodeManager NodeManager { get; private set; }

        private const string _logDir = "./Mods/SlugBrain";
        private const string _logPath = _logDir + "/log.txt";

        public static bool debugAI = true;
        public static bool debugTerrainAndSlopes = false;

    }
}
