using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using ModLoader;
using ModLoader.Helpers;
using SFS.IO;
using UnityEngine;

namespace SmartSASMod
{
    [UsedImplicitly]
    public class Entrypoint : Mod // ! , IUpdatable
    {
        public override string ModNameID => "smartsas";
        public override string DisplayName => "Smart SAS";
        public override string Author => "Astro The Rabbit";
        public override string MinimumGameVersionNecessary => "1.6.0.14";
        public override string ModVersion => "1.10";
        public override string Description => "Adds a variety of control options for the stability assist system (SAS).";

        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string>
        {
            { "UITools", "1.1.5" }
        };
        
        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>()
        {
            {
                "https://github.com/AstroTheRabbit/Smart-SAS-Mod-SFS/releases/latest/download/SmartSASMod.dll",
                new FolderPath(ModFolder).ExtendToFile("SmartSASMod.dll")
            }
        };

        public static Entrypoint Main { get; private set; }
        public static Traverse ANAISTraverse;

        public override void Early_Load()
        {
            Main = this;
            new Harmony(ModNameID).PatchAll();
        }

        public override void Load()
        {
            Settings.Init();
            KeybindsManager.Init();
            
            SceneHelper.OnWorldSceneLoaded += GUI.SpawnGUI;
            if (Settings.settings.useANAISTargeting)
            {
                try
                {
                    Assembly assembly = Loader.main.GetLoadedMods().First(mod => mod.ModNameID == "ANAIS").GetType().Assembly;
                    Type velocityArrowPatch = assembly.GetTypes().First(type => type.Name == "VelocityArrowDrawer_OnLocationChange_Patch");
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