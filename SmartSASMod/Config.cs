using System;
using SFS.IO;
using SFS.Parsers.Json;
using UnityEngine;

namespace SmartSASMod
{
    [Serializable]
    public class SettingsData
    {
        public float windowScale = 1f;
        public Vector2Int windowPosition = new Vector2Int( -850, -500 );
    }

    public static class Config
    {
        static readonly FilePath Path = Main.modFolder.ExtendToFile("Config.txt");

        public static void Load()
        {
            if (!JsonWrapper.TryLoadJson(Path, out data) && Path.FileExists())
            {
                Debug.Log("Config couldn't be loaded correctly, reverting to defaults.");
            }
            data = data ?? new SettingsData();
            Save();
        }

        public static SettingsData data;

        public static void Save()
        {
            if (data == null)
                Load();
            Path.WriteText(JsonWrapper.ToJson(data, true));
        }
    }
}
