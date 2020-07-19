// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.IO;

namespace Naninovel
{
    public class GlobalStateSlotManager : StateSlotManager<GlobalStateMap>
    {
        private string defaultSlotId;

        public GlobalStateSlotManager (StateConfiguration config, string savesFolderPath) 
            : base(savesFolderPath, config.BinarySaveFiles)
        {
            defaultSlotId = config.DefaultGlobalSlotId;
        }

        public override bool AnySaveExists ()
        {
            if (!Directory.Exists(SaveDataPath)) return false;
            return Directory.GetFiles(SaveDataPath, $"{defaultSlotId}.{Extension}", SearchOption.TopDirectoryOnly).Length > 0;
        }
    }
}
