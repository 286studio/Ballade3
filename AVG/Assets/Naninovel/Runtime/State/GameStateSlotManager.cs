// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.IO;

namespace Naninovel
{
    public class GameStateSlotManager : StateSlotManager<GameStateMap>
    {
        private readonly string savePattern, quickSavePattern;

        public GameStateSlotManager (StateConfiguration config, string savesFolderPath) 
            : base(savesFolderPath, config.BinarySaveFiles)
        {
            savePattern = string.Format(config.SaveSlotMask, "*") + $".{Extension}"; 
            quickSavePattern = string.Format(config.QuickSaveSlotMask, "*") + $".{Extension}";
        }

        public override bool AnySaveExists ()
        {
            if (!Directory.Exists(SaveDataPath)) return false;
            var saveExists = Directory.GetFiles(SaveDataPath, savePattern, SearchOption.TopDirectoryOnly).Length > 0;
            var qSaveExists = Directory.GetFiles(SaveDataPath, quickSavePattern, SearchOption.TopDirectoryOnly).Length > 0;
            return saveExists || qSaveExists;
        }
    }
}
