using System;
using System.Collections;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.Common;

public abstract class AssetHandler<AssetType> where AssetType : class, new()
{
    protected string dataPath;
    protected IMonitor monitor;

    protected AssetType? privateData = null!;
    public AssetType data
    {
        get
        {
            if (privateData == null)
            {
                privateData = Game1.content.Load<AssetType>(this.dataPath);
                if (data is ICollection i)
                {
                    monitor.Log($"Loaded asset {dataPath} with {i.Count} entries.");
                }
                else
                {
                    monitor.Log($"Loaded asset {dataPath}.");
                }
            }
            return privateData!;
        }
    }

    public AssetHandler(string dataPath, IMonitor monitor)
    {
        this.dataPath = dataPath;
        this.monitor = monitor;
    }

    public void RegisterEvents(IModHelper helper)
    {
        helper.Events.Content.AssetRequested += this.OnAssetRequested;
        helper.Events.Content.AssetsInvalidated += this.OnAssetsInvalidated;
    }

    public virtual void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(this.dataPath))
        {
            e.LoadFrom(() => new AssetType(), AssetLoadPriority.Exclusive);
        }
    }

    public void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        foreach (var name in e.NamesWithoutLocale)
        {
            if (name.IsEquivalentTo(this.dataPath))
            {
                monitor.Log($"Asset {dataPath} invalidated, reloading.");
                this.privateData = null;
            }
        }
    }
}

public abstract class DictAssetHandler<AssetType> : AssetHandler<Dictionary<string, AssetType>> where AssetType : new()
{
    private Func<IEnumerable<string>>? getKeysToFillDelegate;

    public DictAssetHandler(string dataPath, IMonitor monitor, Func<IEnumerable<string>>? getKeysToFillDelegate = null) : base(dataPath, monitor)
    {
        this.getKeysToFillDelegate = getKeysToFillDelegate;
    }

    public override void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(this.dataPath))
        {
            var dict = new Dictionary<string, AssetType>();
            IEnumerable<string>? keysToFill = getKeysToFillDelegate?.Invoke() ?? null;
            if (keysToFill is not null)
            {
                foreach (string key in keysToFill)
                {
                    dict[key] = new AssetType();
                }
            }
            e.LoadFrom(() => dict, AssetLoadPriority.Exclusive);
        }
    }
}