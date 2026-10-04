using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;

namespace BreakGeodesInBulkChinese
{
    public class ModEntry : Mod
    {
        private Harmony? _harmony;

        private static readonly Dictionary<string, string> Translations = new()
        {
            ["Geode Breaking Options"] = "晶洞破解选项",
            ["Geode Break Mode"] = "晶洞破解模式",
            ["Choose how geodes are broken when inventory is full."] = "当背包已满时，选择如何破解晶洞。",
            ["All (If Inventory Fits)"] = "全部（若背包放得下）",
            ["All (Extra Falls On Ground)"] = "全部（多余物品掉落在地上）",
            ["Animation Speed Multiplier"] = "动画速度倍率",
            ["Adjust how fast Clint breaks geodes. Lower = faster. Example: 0.5 = 2x faster."] = "调整克林特破解晶洞的速度。数值越小越快。例如：0.5 = 2倍速。",
            ["Mobile Compatibility Mode"] = "移动端兼容模式",
            ["Enable this if you're playing on Android/mobile to prevent game crashes."] = "如果在 Android/移动端游玩，请启用此项以防止游戏崩溃。",
            ["Enable Debug Mode"] = "启用调试模式",
            ["Enable verbose logging to help with debugging. Turn off for normal gameplay."] = "启用详细日志以帮助调试。正常游玩时请关闭。",
            ["OverlayOffsetX"] = "叠加层 X 偏移",
            ["OverlayOffsetY"] = "叠加层 Y 偏移",
            ["OverlayScale"] = "叠加层缩放"
        };

        public override void Entry(IModHelper helper)
        {
            _harmony = new Harmony(ModManifest.UniqueID);

            Assembly? targetMod = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name?.Contains("BreakGeodesInBulk", StringComparison.OrdinalIgnoreCase) == true);

            if (targetMod == null)
            {
                Monitor.Log("未找到 BreakGeodesInBulk 程序集，跳过汉化。", LogLevel.Warn);
                return;
            }

            MethodInfo transpiler = typeof(ModEntry).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic)!;
            var harmonyTranspiler = new HarmonyMethod(transpiler);
            int patchedCount = 0;

            foreach (Type type in targetMod.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (!ShouldPatch(method)) continue;
                    try
                    {
                        _harmony.Patch(method, transpiler: harmonyTranspiler);
                        patchedCount++;
                    }
                    catch { }
                }
            }
            Monitor.Log($"BreakGeodesInBulk 汉化补丁已加载，共修补 {patchedCount} 个方法。", LogLevel.Info);
        }

        private static bool ShouldPatch(MethodBase method)
        {
            string name = method.Name;
            return name.Contains("generateGMCM") ||
                   name.Contains("BuildConfigMenu") ||
                   name.Contains("RegisterConfig") ||
                   name.Contains("OnGameLaunched");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string original && Translations.TryGetValue(original, out string? translated))
                {
                    instruction.operand = translated;
                }
                yield return instruction;
            }
        }
    }
}
