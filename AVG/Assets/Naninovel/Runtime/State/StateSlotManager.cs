// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    public abstract class StateSlotManager<TData> : IOSaveSlotManager<TData> where TData : StateMap, new()
    {
        protected override string SaveDataPath => $"{GameDataPath}/{saveFolderName}";
        protected override string Extension => binary ? "nson" : "json";
        protected override bool Binary => binary;

        private string saveFolderName;
        private bool binary;

        public StateSlotManager (string saveFolderName, bool binary)
        {
            this.saveFolderName = saveFolderName;
            this.binary = binary;
        }
    }
}
