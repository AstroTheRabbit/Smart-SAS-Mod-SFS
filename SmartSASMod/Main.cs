using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ModLoader;
using ModLoader.Helpers;
using SFS.IO;
using UnityEngine;

namespace SmartSASMod
{
    public class Main : Mod
    {
        public override string ModNameID => "smartsas";
        public override string DisplayName => "Smart SAS";
        public override string Author => "Astro The Rabbit";
        public override string MinimumGameVersionNecessary => "1.5.10.2";
        public override string ModVersion => "v1.6";
        public override string Description => "Adds a variety of control options for the stability assist system (SAS).";

        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.1.5" } };
        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>() { { "https://github.com/AstroTheRabbit/Smart-SAS-Mod-SFS/releases/latest/download/SmartSASMod.dll", new FolderPath(ModFolder).ExtendToFile("SmartSASMod.dll") } };

        public static Mod mod;
        public static FolderPath modFolder;
        public static Traverse ANAISTraverse = null;

        public override void Early_Load()
        {
            mod = this;
            modFolder = new FolderPath(ModFolder);
            new Harmony("smartsasmod").PatchAll();
        }

        public override void Load()
        {
            SettingsManager.Load();
            SceneHelper.OnWorldSceneLoaded += GUI.SpawnGUI;
            if (SettingsManager.settings.useANAISTargeting)
            {
                try
                {
                    Assembly ANAISAssembly = Loader.main.GetLoadedMods().First((Mod mod) => mod.ModNameID == "ANAIS").GetType().Assembly;
                    Type velocityArrowPatch = ANAISAssembly.GetTypes().First((Type type) => type.Name == "VelocityArrowDrawer_OnLocationChange_Patch");
                    ANAISTraverse = Traverse.Create(velocityArrowPatch);
                } 
                catch
                {
                    Debug.Log("Smart SAS: ANAIS is not installed/active.");
                }
            }
        }
    }
}