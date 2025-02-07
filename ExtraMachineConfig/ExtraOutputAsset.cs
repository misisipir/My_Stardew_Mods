using StardewValley.GameData.Machines;
using Selph.StardewMods.Common;
using System.Collections.Generic;
using ExtraMachineConfig;

namespace Selph.StardewMods.ExtraMachineConfig;

public sealed class ExtraOutputAssetHandler : DictAssetHandler<MachineItemOutput>
{
    public ExtraOutputAssetHandler() : base("selph.ExtraMachineConfig/ExtraOutputs", ModEntry.Mmonitor) { }
}

public class IngredientConfig
{
    public string? Id;
    // Match the ingredient being consumed
    public string? ItemId;
    public string? ContextTags;

    public string? InputPreserveId;

    public int? OutputPreserveId;

    public int? OutputColor;
    public float? OutputPriceMultiplier;
}

public class ExtraCraftingConfig
{
    public List<IngredientConfig>? IngredientConfigs;
    public string? ObjectDisplayName;
    public string? ObjectInternalName;
}

public sealed class ExtraCraftingConfigAssetHandler : DictAssetHandler<ExtraCraftingConfig>
{
    public ExtraCraftingConfigAssetHandler() : base("selph.ExtraMachineConfig/ExtraCraftingConfig", ModEntry.Mmonitor) { }
}