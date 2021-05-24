// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System.Linq;
using UnityEditor;

namespace Naninovel
{
    public static class AddressableHelper
    {
        #if ADDRESSABLES_AVAILABLE
        public static bool Available => true;
        #else
        public static bool Available => false;
        #endif

        #if ADDRESSABLES_AVAILABLE
        private static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings =>
            settingsCache ? settingsCache : settingsCache = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.GetSettings(true);
        private static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settingsCache;
        #endif

        public static bool CheckAssetConflict (string assetGuid, string path, out string conflictAddress)
        {
            conflictAddress = null;
            #if ADDRESSABLES_AVAILABLE
            var entry = settings.FindAssetEntry(assetGuid);
            if (entry is null) return false;
            var address = PathToAddress(path);
            conflictAddress = entry.address;
            return entry.address != address;
            #else
            return false;
            #endif
        }

        public static void RemovePreviousEntries ()
        {
            #if ADDRESSABLES_AVAILABLE
            foreach (var group in settings.groups)
                if (group.Name.StartsWithFast(ResourceProviderConfiguration.AddressableId))
                    foreach (var entry in group.entries.ToArray())
                        group.RemoveAssetEntry(entry);
            #endif
        }

        public static void CreateOrUpdateAddressableEntry (string assetGuid, string path, bool groupByCategory)
        {
            #if ADDRESSABLES_AVAILABLE
            var address = PathToAddress(path);
            var label = ResourceProviderConfiguration.AddressableId;
            var groupName = groupByCategory ? PathToGroup(path) : ResourceProviderConfiguration.AddressableId;

            var group = FindOrCreateGroup(groupName);
            var entry = settings.CreateOrMoveEntry(assetGuid, group);

            entry.SetAddress(address);
            entry.SetLabel(label, true, true);

            EditorUtility.SetDirty(settings);
            #endif
        }

        public static void RebuildPlayerContent ()
        {
            #if ADDRESSABLES_AVAILABLE
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent();
            #endif
        }

        #if ADDRESSABLES_AVAILABLE
        private static UnityEditor.AddressableAssets.Settings.AddressableAssetGroup FindOrCreateGroup (string groupName)
        {
            return settings.FindGroup(groupName) ?? settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
        }

        private static string PathToAddress (string path) => PathUtils.Combine(ResourceProviderConfiguration.AddressableId, path);

        private static string AddressToPath (string address) => address.GetAfterFirst($"{ResourceProviderConfiguration.AddressableId}/");

        private static string PathToGroup (string path)
        {
            var postfix = path.Contains("/") ? path.GetBefore("/") : path;
            return $"{ResourceProviderConfiguration.AddressableId}-{postfix}";
        }
        #endif
    }
}
