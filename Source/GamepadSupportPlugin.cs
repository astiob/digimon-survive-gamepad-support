using AdvEngine;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace GamepadSupportPlugin
{
	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	public class GamepadSupportPlugin : BaseUnityPlugin
	{
		internal static new ManualLogSource Logger;

		public void Awake()
		{
			Logger = base.Logger;
			Logger.LogInfo($"Plugin {pluginGuid} loaded");

			var harmony = new Harmony(pluginGuid);
			var GetGamePadType_common_call_transpiler =
				new HarmonyMethod(new Func<IEnumerable<CodeInstruction>, IEnumerable<CodeInstruction>>(Patches.GetGamePadType_common_call_transpiler).Method);
			harmony.PatchAll(typeof(Patches));
			harmony.Patch(AccessTools.Method(AccessTools.Inner(typeof(AdvExploreCursorDisp), "CursorObj"), "GetTextSpineObjSkinSkinName"),
				prefix: new HarmonyMethod(new Action<Dictionary<GameInput2.KeyType, string>>(Patches.GetTextSpineObjSkinSkinName_prefix).Method),
				postfix: new HarmonyMethod(new Func<string, Dictionary<GameInput2.KeyType, string>, string>(Patches.GetTextSpineObjSkinSkinName_postfix).Method),
				transpiler: GetGamePadType_common_call_transpiler);
			harmony.Patch(AccessTools.Method(AccessTools.Inner(typeof(SteamGamePad), "MyDigitalActionHandle"), "InitKeyType"),
				prefix: new HarmonyMethod(new Patches.MyDigitalActionHandle_InitKeyType_delegate(Patches.MyDigitalActionHandle_InitKeyType_prefix).Method));
			harmony.Patch(AccessTools.Method(AccessTools.Inner(typeof(SteamGamePad), "MyAnalogActionHandle"), "InitKeyType"),
				prefix: new HarmonyMethod(new Patches.MyAnalogActionHandle_InitKeyType_delegate(Patches.MyAnalogActionHandle_InitKeyType_prefix).Method));
			harmony.Patch(AccessTools.Method(AccessTools.Inner(typeof(TutorialUI), "TextArea"), "GetGamepadTutorialText"),
				prefix: new HarmonyMethod(new Patches.GetGamepadTutorialText_delegate(Patches.GetGamepadTutorialText_prefix).Method));
		}

		public const string pluginGuid = "mods.digimonsurvive.gamepad";
		public const string pluginName = "GamepadSupport";
		public const string pluginVersion = "1.3.0.0";
	}
}
