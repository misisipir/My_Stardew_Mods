using System;
using StardewValley;
using StardewValley.Objects;
using HarmonyLib;
using StardewModdingAPI;
using ExtraMachineConfig;

namespace Selph.StardewMods.ExtraMachineConfig;

using SObject = StardewValley.Object;

public class AutomatePatcher
{
    public static void ApplyPatches(Harmony harmony)
    {
        var dataBasedMachineType = AccessTools.TypeByName("Pathoschild.Stardew.Automate.Framework.Machines.DataBasedObjectMachine");

        harmony.Patch(
            original: AccessTools.Method(dataBasedMachineType, "GetOutput"),
            postfix: new HarmonyMethod(typeof(AutomatePatcher),
              nameof(AutomatePatcher.DataBasedMachine_GetOutput_Postfix)));

    }

    static void DataBasedMachine_GetOutput_Postfix(object __instance, ref object __result)
    {
        try
        {
            var machine = ModEntry.Helper.Reflection.GetProperty<SObject>(__instance, "Machine").GetValue();
            if (machine.heldObject.Value.heldObject.Value is Chest chest &&
                chest.Items.Count > 0)
            {
                foreach (var item in chest.Items)
                {
                    if (item is not null)
                    {
                        __result = ModEntry.Helper.Reflection.GetMethod(__instance, "GetTracked")
                          .Invoke<object>(item, (Item _) => {
                              chest.Items.Remove(item);
                              if (chest.Items.Count == 0)
                              {
                                  machine.heldObject.Value.heldObject.Value = null;
                                  if (machine.heldObject.Value.QualifiedItemId == ModEntry.HolderQualifiedId)
                                  {
                                      machine.heldObject.Value = null;
                                      machine.readyForHarvest.Value = false;
                                      machine.showNextIndex.Value = false;
                                      machine.ResetParentSheetIndex();
                                  }
                              }
                          });
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            ModEntry.Mmonitor.Log(e.Message, LogLevel.Error);
        }
    }
}