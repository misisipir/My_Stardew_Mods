using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
using StardewValley.Objects;
using Selph.StardewMods.ExtraMachineConfig;
using StardewValley.Delegates;
using StardewValley.Internal;

namespace ExtraMachineConfig; 

using SObject = StardewValley.Object;

internal sealed class ModEntry : Mod {
  internal new static IModHelper Helper { get;
    set;
  }

  internal static IMonitor Mmonitor { get; set; }
  internal static IExtraMachineConfigApi ModApi;

  // Keys for the CustomData map
  internal static Regex RequirementIdKeyRegex =
    new Regex(@"selph.ExtraMachineConfig\.RequirementId\.(\d+)");
  internal static Regex RequirementTagsKeyRegex =
    new Regex(@"selph.ExtraMachineConfig\.RequirementTags\.(\d+)");
  internal static string RequirementCountKeyPrefix = "selph.ExtraMachineConfig.RequirementCount";
  internal static string RequirementInvalidMsgKey = "selph.ExtraMachineConfig.RequirementInvalidMsg";
  internal static string InheritPreserveIdKey = "selph.ExtraMachineConfig.InheritPreserveId";
  internal static string CopyColorKey = "selph.ExtraMachineConfig.CopyColor";

  internal static string ExtraContextTagsKey = "selph.ExtraMachineConfig.ExtraContextTags";

  // Legacy versions, no mod IDs because I'm stupid
  internal static Regex RequirementIdKeyRegex_Legacy =
    new Regex(@"ExtraMachineConfig\.RequirementId\.(\d+)");
  internal static string RequirementCountKeyPrefix_Legacy = "ExtraMachineConfig.RequirementCount";
  internal static string RequirementInvalidMsgKey_Legacy = "ExtraMachineConfig.RequirementInvalidMsg";
  internal static string InheritPreserveIdKey_Legacy = "ExtraMachineConfig.InheritPreserveId";
  internal static string CopyColorKey_Legacy = "ExtraMachineConfig.CopyColor";
    internal static string HolderId = $"selph.ExtraMachineConfig.Holder";
    internal static string HolderQualifiedId = $"(O){HolderId}";
    internal static ExtraOutputAssetHandler extraOutputAssetHandler;
    internal static string ExtraOutputIdsKey = "selph.ExtraMachineConfig.ExtraOutputIds";



    // This is a dirty, dirty hack to prevent an infinite loop where the machine byproducts get
    // their own global machine byproducts attached, leading to cascading calls of
    // GetOutputItems on top of GetOutputItems.
    // (Gods why did I code it like this?)
    public static bool addByproducts = true;

    public override void Entry(IModHelper helper) {
    Helper = helper;
    Mmonitor = this.Monitor;
    ModApi = new ExtraMachineConfigApi();
    var harmony = new Harmony(this.ModManifest.UniqueID);
    extraOutputAssetHandler = new ExtraOutputAssetHandler();


    extraOutputAssetHandler.RegisterEvents(Helper);


        harmony.Patch(
        original: AccessTools.Method(
          typeof(StardewValley.MachineDataUtility),
          nameof(StardewValley.MachineDataUtility.GetOutputData),
          new Type[] { typeof(List<MachineItemOutput>), typeof(bool), typeof(Item),
          typeof(Farmer), typeof(GameLocation) }),
        prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.MachineDataUtility_GetOutputData_prefix)));


        harmony.Patch(
        original: AccessTools.Method(typeof(StardewValley.MachineDataUtility),
          nameof(StardewValley.MachineDataUtility.GetOutputItem)),
        postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.MachineDataUtility_GetOutputItem_postfix)));

    harmony.Patch(
        original: AccessTools.Method(typeof(Item),
          nameof(Item.GetContextTags)),
        postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Item_GetContextTags_postfix)));
        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer),
              nameof(Farmer.OnItemReceived)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Farmer_OnItemReceived_postfix)));

        harmony.Patch(
     original: AccessTools.Method(typeof(Chest),
     nameof(Chest.addItem)),
     postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Chest_addItem_postfix)));

    SmokedItemHarmonyPatcher.ApplyPatches(harmony);
    AnimalDataPatcher.ApplyPatches(harmony);
    Helper.Events.GameLoop.DayStarted += AnimalDataPatcher.OnDayStartedJunimoHut;



        try
        {
            if (Helper.ModRegistry.IsLoaded("Pathoschild.Automate"))
            {
                this.Monitor.Log("This mod patches Automate. If you notice issues with Automate, make sure it happens without this mod before reporting it to the Automate page.", LogLevel.Debug);
                AutomatePatcher.ApplyPatches(harmony);
            }
        }
        catch (Exception e)
        {
            Monitor.Log("Failed patching Automate. Detail: " + e.Message, LogLevel.Error);
        }

    }

    public override object GetApi() {
    return ModApi;
  }

  // This patch:
  // * Checks for additional fuel requirements specified in the output rule's custom data, and
  // removes rules that cannot be satisfied
  private static void MachineDataUtility_GetOutputData_prefix(ref List<MachineItemOutput> outputs,
      bool useFirstValidOutput, Item inputItem, Farmer who,
      GameLocation location) {
    if (outputs == null || outputs.Count < 0) {
      return;
    }
    string invalidMessage = null;
    IInventory inventory = SObject.autoLoadFrom ?? who.Items;
    List<MachineItemOutput> newOutputs = new List<MachineItemOutput>();
    
    foreach (MachineItemOutput output in outputs) {
      if (output.CustomData == null) {
        newOutputs.Add(output);
        continue;
      }
      bool valid = true;
      var extraRequirements = ModApi.GetExtraRequirements(output);
      foreach (var entry in extraRequirements) {
        if (Game1.player.getItemCountInList(inventory, entry.Item1) < entry.Item2) {
          valid = false;
        }
      }
      var extraTagsRequirements = ModApi.GetExtraTagsRequirements(output);
      foreach (var entry in extraTagsRequirements) {
        if (Utils.getItemCountInListByTags(inventory, entry.Item1) < entry.Item2) {
          valid = false;
        }
      }
      if (valid) {
        newOutputs.Add(output);
      } else {
        if (output.CustomData.TryGetValue(RequirementInvalidMsgKey, out var msg)) {
          invalidMessage ??= msg;
        }
        if (output.CustomData.TryGetValue(RequirementInvalidMsgKey_Legacy, out var msgLegacy)) {
          invalidMessage ??= msgLegacy;
        }
      }
    }
    outputs = newOutputs;
    if (outputs.Count == 0 && invalidMessage != null && who.IsLocalPlayer &&
        SObject.autoLoadFrom == null) {
      Game1.showRedMessage(invalidMessage);
    }
  }

  // This patch:
  // * Checks for additional fuel requirements specified in the output rule's custom data, and
  // removes them from inventory
  // * Checks if preserve ID is set to inherit the input item's preserve ID, and applies it
  // * Checks if a colored item should be created and apply the changes
  private static void MachineDataUtility_GetOutputItem_postfix(ref Item __result, SObject machine,
      MachineItemOutput outputData, Item inputItem,
      Farmer who, bool probe,
      ref int? overrideMinutesUntilReady) {
    if (__result == null || outputData == null || inputItem == null) {
      return;
    }
 
    IInventory inventory = SObject.autoLoadFrom ?? who.Items;
    // Inherit preserve ID
    if ((outputData.PreserveId == "INHERIT" ||
          (outputData.CustomData != null &&
           (outputData.CustomData.ContainsKey(InheritPreserveIdKey) ||
            outputData.CustomData.ContainsKey(InheritPreserveIdKey_Legacy)))) &&
        inputItem is SObject inputObject &&
        inputObject.preservedParentSheetIndex.Value != "-1" &&
        __result is SObject resultObject) {
      resultObject.preservedParentSheetIndex.Value = inputObject.preservedParentSheetIndex.Value;
    }



        var extraOutputs = ModApi.GetExtraOutputs(outputData, machine.GetMachineData());
        if (extraOutputs.Count > 0 && __result != null)
        {
            Chest chest = new Chest(false);
            (__result as SObject).heldObject.Value = chest;
            GameStateQueryContext context = new GameStateQueryContext(machine.Location, who, (__result as SObject), inputItem, Game1.random);
            ItemQueryContext itemContext = new ItemQueryContext(machine.Location, who, Game1.random, "machine '" + machine.QualifiedItemId + "' > output rules - extra output items from with ExtraMachineConfig");
            foreach (var extraOutputData in extraOutputs)
            {
                addByproducts = false;
                var item = MachineDataUtility.GetOutputItem(machine, extraOutputData, inputItem, who, false, out var _);
                addByproducts = true;

                chest.addItem(item);
                // Game1.createItemDebris(item, machine.TileLocation * 64f, -1, machine.Location);
            }
        }


        if (outputData.CustomData == null)
        {
            return;
        }

        // Remove extra fuel
    var extraRequirements = ModApi.GetExtraRequirements(outputData);
    foreach (var entry in extraRequirements) {
      Utils.RemoveItemFromInventoryById(inventory, entry.Item1, entry.Item2);
    }
    var extraTagsRequirements = ModApi.GetExtraTagsRequirements(outputData);
    foreach (var entry in extraTagsRequirements) {
      Utils.RemoveItemFromInventoryByTags(inventory, entry.Item1, entry.Item2);
    }
    // Color the item
    if ((outputData.CustomData.ContainsKey(CopyColorKey) ||
          outputData.CustomData.ContainsKey(CopyColorKey_Legacy)) &&
        __result is SObject) {
      StardewValley.Objects.ColoredObject newColoredObject;
      if (__result is StardewValley.Objects.ColoredObject coloredObject) {
        newColoredObject = coloredObject;
      } else {
        newColoredObject = new StardewValley.Objects.ColoredObject(
            __result.ItemId,
            __result.Stack,
            Color.White
            );
        Helper.Reflection.GetMethod(newColoredObject, "GetOneCopyFrom").Invoke(__result);
        newColoredObject.Stack = __result.Stack;
      }
       var color = TailoringMenu.GetDyeColor(inputItem);
      if (color != null) {
        newColoredObject.color.Value = (Color)color;
        __result = newColoredObject;
      }
    }



       


    }

    private static void Item_GetContextTags_postfix(Item __instance, ref HashSet<string> __result) {
    if (__instance.modData.TryGetValue(ExtraContextTagsKey, out string contextTags)) {
      __result.UnionWith(contextTags.Split(","));
    }
  }


    public static void Chest_addItem_postfix(Chest __instance, Item __result, Item item)
    {
        if (item.QualifiedItemId == HolderQualifiedId)
        {
            __instance.Items.Remove(item);
        }
        if (__result != null || item is not SObject { heldObject.Value: Chest chest } obj) return;
        foreach (var extraItem in chest.Items)
        {
            Mmonitor.Log("item info : " + extraItem.Name);
            var leftoverItem = __instance.addItem(extraItem);
            if (leftoverItem != null)
            {
                Game1.createItemDebris(leftoverItem, __instance.TileLocation * 64f, -1, __instance.Location);
            }
        }
        obj.heldObject.Value = null;
    }


    private static void Farmer_OnItemReceived_postfix(Farmer __instance, Item item, int countAdded, Item mergedIntoStack, bool hideHudNotification = false)
    {
        Mmonitor.Log("FARMOER"+item.QualifiedItemId);
        if (item.QualifiedItemId == HolderQualifiedId)
        {
            __instance.removeItemFromInventory(item);
        }
        if (item is SObject obj && obj.heldObject.Value is Chest chest)
        {
            __instance.addItemsByMenuIfNecessary(new List<Item>(chest.Items));
            obj.heldObject.Value = null;
        }
    }



}
