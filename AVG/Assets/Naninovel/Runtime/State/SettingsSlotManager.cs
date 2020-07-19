// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.IO;

namespace Naninovel
{
    public class SettingsSlotManager : StateSlotManager<SettingsStateMap>
    {
        private string defaultSlotId;

        public SettingsSlotManager (StateConfiguration config, string savesFolderPath) 
            : base(savesFolderPath, false)
        {
            defaultSlotId = config.DefaultSettingsSlotId;
        }

        public override bool AnySaveExists ()
        {
            if (!Directory.Exists(SaveDataPath)) return false;
            return Directory.GetFiles(SaveDataPath, $"{defaultSlotId}.{Extension}", SearchOption.TopDirectoryOnly).Length > 0;
        }
    }
}
