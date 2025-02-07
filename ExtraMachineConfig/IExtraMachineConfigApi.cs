using StardewValley.GameData.Machines;
using System.Collections.Generic;

namespace ExtraMachineConfig;

public interface IExtraMachineConfigApi {
  IList<(string, int)> GetExtraRequirements(MachineItemOutput outputData);
  IList<(string, int)> GetExtraTagsRequirements(MachineItemOutput outputData);

    IList<MachineItemOutput> GetExtraOutputs(MachineItemOutput outputData, MachineData? machineData = null);
}
