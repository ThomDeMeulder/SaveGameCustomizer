using HarmonyLib;
using SaveGameCustomizer.Config;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SaveGameCustomizer.Patches
{
    [HarmonyPatch(typeof(UserStoragePC), "LoadFilesAsyncImpl")]
    internal static class UserStoragePC_LoadFilesAsyncImpl_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int matches = 0;
            bool previousStlocS = false;
            bool previousLdnull = false;
            bool addInstructions = false;

            MethodInfo saveFilePathMethod = typeof(UserStoragePC).GetMethod("GetSaveFilePath", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo ensureSaveGameConfigMethod = typeof(SaveGameConfig).GetMethod(nameof(SaveGameConfig.CreateSaveGameConfigOnAbsence), BindingFlags.Static | BindingFlags.NonPublic);

            foreach (CodeInstruction instruction in instructions)
            {
                if (addInstructions)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
                    yield return new CodeInstruction(OpCodes.Call, saveFilePathMethod);
                    yield return new CodeInstruction(OpCodes.Call, ensureSaveGameConfigMethod);
                    yield return instruction;

                    addInstructions = false;
                    continue;
                }

                if (instruction.opcode.Equals(OpCodes.Stloc_S))
                {
                    if (previousStlocS && previousLdnull)
                    {
                        // Reset all states
                        previousStlocS = false;
                        previousLdnull = false;

                        if (matches == 1)
                        {
                            addInstructions = true;
                        }

                        matches++;
                        yield return instruction;
                        continue;
                    }
                    previousStlocS = true;
                }
                else if (instruction.opcode.Equals(OpCodes.Ldnull))
                {
                    if (previousStlocS)
                    {
                        previousLdnull = true;
                    }
                    else
                    {
                        previousStlocS = false;
                    }
                }
                yield return instruction;
            }
        }
    }
}
