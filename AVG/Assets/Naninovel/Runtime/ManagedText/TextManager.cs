// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System.Collections.Generic;
using System.Linq;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ITextManager"/>
    [InitializeAtRuntime]
    public class TextManager : ITextManager
    {
        public virtual ManagedTextConfiguration Configuration { get; }

        private readonly IResourceProviderManager providersManager;
        private readonly ILocalizationManager localizationManager;
        private readonly HashSet<ManagedTextRecord> records = new HashSet<ManagedTextRecord>();
        private LocalizableResourceLoader<TextAsset> documentLoader;

        public TextManager (ManagedTextConfiguration config, IResourceProviderManager providersManager, ILocalizationManager localizationManager)
        {
            Configuration = config;
            this.providersManager = providersManager;
            this.localizationManager = localizationManager;
        }

        public virtual UniTask InitializeServiceAsync ()
        {
            documentLoader = Configuration.Loader.CreateLocalizableFor<TextAsset>(providersManager, localizationManager);
            localizationManager.AddChangeLocaleTask(ApplyManagedTextAsync);
            return UniTask.CompletedTask;
        }

        public virtual void ResetService () { }

        public virtual void DestroyService ()
        {
            localizationManager?.RemoveChangeLocaleTask(ApplyManagedTextAsync);
            documentLoader?.ReleaseAll(this);
        }

        public virtual string GetRecordValue (string key, string category = ManagedTextRecord.DefaultCategoryName)
        {
            foreach (var record in records)
                if (record.Category.EqualsFast(category) && record.Key.EqualsFast(key))
                    return record.Value;
            return null;
        }

        public virtual IReadOnlyCollection<ManagedTextRecord> GetAllRecords (params string[] categoryFilter)
        {
            if (categoryFilter is null || categoryFilter.Length == 0)
                return records.ToList();

            var result = new List<ManagedTextRecord>();
            foreach (var record in records)
                if (categoryFilter.Contains(record.Category))
                    result.Add(record);
            return result;
        }

        public virtual async UniTask ApplyManagedTextAsync ()
        {
            records.Clear();
            var documentResources = await documentLoader.LoadAndHoldAllAsync(this);
            foreach (var documentResource in documentResources)
            {
                if (!documentResource.Valid)
                {
                    Debug.LogWarning($"Failed to load `{documentResource.Path}` managed text document.");
                    continue;
                }
                var managedTextSet = ManagedTextUtils.ParseDocument(documentResource.Object.text, documentLoader.GetLocalPath(documentResource));

                foreach (var text in managedTextSet)
                    records.Add(new ManagedTextRecord(text.Key, text.Value, text.Category));

                ManagedTextUtils.ApplyRecords(managedTextSet);
            }
        }
    }
}
