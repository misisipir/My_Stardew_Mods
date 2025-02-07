using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Inventories;
using StardewValley.GameData.Machines;
using StardewValley.GameData.BigCraftables;
using StardewValley.TokenizableStrings;
using HarmonyLib;
using System.Collections.Generic;
using Selph.StardewMods.ExtraMachineConfig;

namespace ExtraMachineConfig;

public class ExtraMachineConfigApi : IExtraMachineConfigApi {
  // Extract the additional fuel data from the output data as a list of fuel IDs to fuel count.
  public IList<(string, int)> GetExtraRequirements(MachineItemOutput outputData) {
    IList<(string, int)> extraRequirements = new List<(string, int)>();
    if (outputData?.CustomData == null) {
      return extraRequirements;
    }
    foreach (var entry in outputData.CustomData) {
      var match = ModEntry.RequirementIdKeyRegex.Match(entry.Key);
      if (!match.Success) {
        match = ModEntry.RequirementIdKeyRegex_Legacy.Match(entry.Key);
      }
      if (match.Success) {
        string countKey = ModEntry.RequirementCountKeyPrefix + "." + match.Groups[1].Value;
        string countKey_Legacy = ModEntry.RequirementCountKeyPrefix_Legacy + "." + match.Groups[1].Value;
        string countString;
        if ((outputData.CustomData.TryGetValue(countKey, out countString) ||
              outputData.CustomData.TryGetValue(countKey_Legacy, out countString)) &&
            Int32.TryParse(countString, out int count)) {
          extraRequirements.Add((entry.Value, count));
        } else {
          extraRequirements.Add((entry.Value, 1));
        }
      }
    }
    return extraRequirements;
  }

  // Same as above, but with item category tags instead of IDs
  public IList<(string, int)> GetExtraTagsRequirements(MachineItemOutput outputData) {
    IList<(string, int)> extraRequirements = new List<(string, int)>();
    if (outputData?.CustomData == null) {
      return extraRequirements;
    }
    foreach (var entry in outputData.CustomData) {
      var match = ModEntry.RequirementTagsKeyRegex.Match(entry.Key);
      if (match.Success) {
        string countKey = ModEntry.RequirementCountKeyPrefix + "." + match.Groups[1].Value;
        string countKey_Legacy = ModEntry.RequirementCountKeyPrefix_Legacy + "." + match.Groups[1].Value;
        string countString;
        if ((outputData.CustomData.TryGetValue(countKey, out countString) ||
              outputData.CustomData.TryGetValue(countKey_Legacy, out countString)) &&
            Int32.TryParse(countString, out int count)) {
          extraRequirements.Add((entry.Value, count));
        } else {
          extraRequirements.Add((entry.Value, 1));
        }
      }
    }
    return extraRequirements;
  }
    public IList<MachineItemOutput> GetExtraOutputs(MachineItemOutput outputData, MachineData? machineData = null)
    {
            IList<MachineItemOutput> extraOutputs = new List<MachineItemOutput>();
        if (!ModEntry.addByproducts)
        {
            return extraOutputs;
        }


        string itemquery;


        if(outputData?.CustomData is not null && outputData.CustomData.TryGetValue("selph.ExtraMachineConfig.ExtraOutputIds", out itemquery) && itemquery != null)
        {
            string[] items = itemquery.Split(',', ' ');

            foreach (var extraOutputId in items)
            {
                if (ModEntry.extraOutputAssetHandler.data.TryGetValue(extraOutputId, out var extraOutputData) && (!extraOutputData.CustomData?.ContainsKey("selph.ExtraMachineConfig.ExtraOutputIds") ?? true))
                {
                    extraOutputs.Add(extraOutputData);
                }
            }
        }


        if (machineData?.CustomFields is not null &&
                machineData.CustomFields.TryGetValue(ModEntry.ExtraOutputIdsKey, out var globalExtraOutputIds))
        {
            foreach (var extraOutputId in globalExtraOutputIds.Split(',', ' '))
            {

                if (ModEntry.extraOutputAssetHandler.data.TryGetValue(extraOutputId, out var extraOutputData) &&
                    (!extraOutputData.CustomData?.ContainsKey(ModEntry.ExtraOutputIdsKey) ?? true))
                {
                    extraOutputs.Add(extraOutputData);
                }
            }
        }


        return extraOutputs;
    }
}
