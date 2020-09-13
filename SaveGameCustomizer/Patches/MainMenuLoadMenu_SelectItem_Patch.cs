using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SaveGameCustomizer.Patches
{
    [HarmonyPatch(typeof(MainMenuLoadMenu), "SelectItem")]
    internal static class MainMenuLoadMenu_SelectItem_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo colorBlackGetterMethod = typeof(Color).GetProperty("black", BindingFlags.Public | BindingFlags.Static).GetGetMethod();
            MethodInfo colorWhiteGetterMethod = typeof(Color).GetProperty("white", BindingFlags.Public | BindingFlags.Static).GetGetMethod();

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode.Equals(OpCodes.Call) && instruction.operand.Equals(colorBlackGetterMethod))
                {
                    yield return new CodeInstruction(OpCodes.Call, colorWhiteGetterMethod);
                    continue;
                }
                yield return instruction;
            }
        }

        private static void Postfix(GameObject ___selectedItem)
        {
            MainPatcher.HandleChangeColour(___selectedItem, true);
        }
    }
}
