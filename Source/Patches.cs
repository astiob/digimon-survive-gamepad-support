using Game.Databases;
using HarmonyLib;
using MonoMod.Utils;
using Steamworks;
using Survivor.Steam;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using static GamePadDevice;

namespace GamepadSupportPlugin
{
	static class Patches
	{
		static readonly Dictionary<GameInput2.KeyType, string> joyConTextureNameMap = new Dictionary<GameInput2.KeyType, string>
		{
			{ GameInput2.KeyType.GamePad_A, "sw_button_19" },
			{ GameInput2.KeyType.GamePad_B, "sw_button_17" },
			{ GameInput2.KeyType.GamePad_X, "sw_button_13" },
			{ GameInput2.KeyType.GamePad_Y, "sw_button_18" },
			{ GameInput2.KeyType.GamePad_L1, "sw_button_15" },
			{ GameInput2.KeyType.GamePad_R1, "sw_button_16" },
			// L2 & R2 don't exist in the default configuration.
			// Use the default ZL & ZR sprites, but it's impossible
			// for these two buttons to both exist on a single JoyCon.
			{ GameInput2.KeyType.GamePad_L2, "sw_button_07" },
			{ GameInput2.KeyType.GamePad_R2, "sw_button_08" },
			// The D-pad doesn't exist on a single JoyCon. Ideally we'd
			// hide any D-pad sprites entirely, but for now keep the
			// Xbox's sprites, which are conspicuously different from
			// the Switch's button diamond sprites that we use for ABXY
			// (although they're similar to the Switch's Plus sprite).
			{ GameInput2.KeyType.GamePad_Up, "xb_button_13" },
			{ GameInput2.KeyType.GamePad_Down, "xb_button_15" },
			{ GameInput2.KeyType.GamePad_Left, "xb_button_16" },
			{ GameInput2.KeyType.GamePad_Right, "xb_button_17" },
			// The R-stick doesn't exist on a single JoyCon.
			// There's nothing we can do about that...
			{ GameInput2.KeyType.GamePad_RStickUp, "sw_button_28" },
			{ GameInput2.KeyType.GamePad_RStickDown, "sw_button_29" },
			{ GameInput2.KeyType.GamePad_RStickLeft, "sw_button_30" },
			{ GameInput2.KeyType.GamePad_RStickRight, "sw_button_31" },
			{ GameInput2.KeyType.GamePad_LStickUp, "sw_button_22" },
			{ GameInput2.KeyType.GamePad_LStickDown, "sw_button_23" },
			{ GameInput2.KeyType.GamePad_LStickLeft, "sw_button_24" },
			{ GameInput2.KeyType.GamePad_LStickRight, "sw_button_25" },
			{ GameInput2.KeyType.GamePad_Start, "sw_button_12" },
			{ GameInput2.KeyType.GamePad_Select, "sw_button_11" },
			{ GameInput2.KeyType.GamePad_L3, "sw_button_09" },
			{ GameInput2.KeyType.GamePad_R3, "sw_button_10" },
			{ GameInput2.KeyType.GamePad_L3DI, "sw_button_09" },
			{ GameInput2.KeyType.GamePad_R3DI, "sw_button_10" },
		};
		static readonly string[][] joyConDirectionInputTextureNames =
		{
			new string[3] { "xb_button_14", "xb_button_18", "xb_button_19" },
			new string[3] { "sw_button_26", "sw_button_27", "sw_button_09" },
			new string[3] { "sw_button_32", "sw_button_33", "sw_button_10" },
		};
		static readonly Dictionary<GameInput2.KeyType, string> gamepadButtonKeyTypeToEmojiStrMapJoyCon = new Dictionary<GameInput2.KeyType, string>
		{
			{ GameInput2.KeyType.GamePad_B, "<sprite index=95>" },
			{ GameInput2.KeyType.GamePad_A, "<sprite index=94>" },
			{ GameInput2.KeyType.GamePad_X, "<sprite index=30>" },
			{ GameInput2.KeyType.GamePad_Y, "<sprite index=96>" },
			// SL & SR sprites don't exist here, so use the standard Switch L & R sprites
			{ GameInput2.KeyType.GamePad_L1, "<sprite index=24>" },
			{ GameInput2.KeyType.GamePad_R1, "<sprite index=25>" },
			{ GameInput2.KeyType.GamePad_L2, "<sprite index=26>" },
			{ GameInput2.KeyType.GamePad_R2, "<sprite index=27>" },
			{ GameInput2.KeyType.GamePad_L3, "<sprite index=28>" },
			{ GameInput2.KeyType.GamePad_R3, "<sprite index=29>" },
			{ GameInput2.KeyType.GamePad_Select, "<sprite index=129>" },
		};
		static readonly Dictionary<string, string> stickAndDpadEmojiForJoyCon = new Dictionary<string, string> {
			// Switch's sticks
			{ "<sprite index=16>", "<sprite index=28>" },
			{ "<sprite index=17>", "<sprite index=29>" },
			{ "<sprite index=40>", "<sprite index=28>" },
			{ "<sprite index=41>", "<sprite index=29>" },
			// Xbox's D-pad because there's nothing better we can do...
			{ "<sprite index=18>", "<sprite index=42>" },
			{ "<sprite index=19>", "<sprite index=43>" },
			{ "<sprite index=30>", "<sprite index=42>" },
			{ "<sprite index=31>", "<sprite index=43>" },
			{ "<sprite index=44>", "<sprite index=46>" },
			{ "<sprite index=45>", "<sprite index=46>" },
			{ "<sprite index=91>", "<sprite index=97>" },
			{ "<sprite index=92>", "<sprite index=98>" },
			{ "<sprite index=93>", "<sprite index=99>" },
			{ "<sprite index=94>", "<sprite index=97>" },
			{ "<sprite index=95>", "<sprite index=98>" },
			{ "<sprite index=96>", "<sprite index=99>" },
		};

		static readonly Dictionary<GameInput2.KeyType, string> mobileTouchTextureNameMap = new Dictionary<GameInput2.KeyType, string>
		{
			{ GameInput2.KeyType.GamePad_A, "xb_button_02" },
			{ GameInput2.KeyType.GamePad_B, "xb_button_01" },
			{ GameInput2.KeyType.GamePad_X, "xb_button_03" },
			{ GameInput2.KeyType.GamePad_Y, "xb_button_04" },
			// The shape isn't right, but it's the best we've got
			{ GameInput2.KeyType.GamePad_L1, "xb_button_05" },
			// The shape isn't right, but it's the best we've got
			{ GameInput2.KeyType.GamePad_R1, "xb_button_06" },
			// The shape isn't right, but it's the best we've got
			{ GameInput2.KeyType.GamePad_L2, "xb_button_07" },
			// The shape isn't right, but it's the best we've got
			{ GameInput2.KeyType.GamePad_R2, "xb_button_08" },
			{ GameInput2.KeyType.GamePad_Up, "ps_button_13" },
			{ GameInput2.KeyType.GamePad_Down, "ps_button_17" },
			{ GameInput2.KeyType.GamePad_Left, "ps_button_18" },
			{ GameInput2.KeyType.GamePad_Right, "ps_button_19" },
			// Use PlayStation's sprites (that is, "L" & "R" instead of "LS" & "RS")
			// because the mobile app's control UI uses "LS" & "RS" for stick *clicks*.
			{ GameInput2.KeyType.GamePad_RStickUp, "ps_button_28" },
			{ GameInput2.KeyType.GamePad_RStickDown, "ps_button_29" },
			{ GameInput2.KeyType.GamePad_RStickLeft, "ps_button_30" },
			{ GameInput2.KeyType.GamePad_RStickRight, "ps_button_31" },
			{ GameInput2.KeyType.GamePad_LStickUp, "ps_button_22" },
			{ GameInput2.KeyType.GamePad_LStickDown, "ps_button_23" },
			{ GameInput2.KeyType.GamePad_LStickLeft, "ps_button_24" },
			{ GameInput2.KeyType.GamePad_LStickRight, "ps_button_25" },
			// There's no suitable sprite, so use the least confusing one: this at least matches the name in the game's default Steam controller settings
			{ GameInput2.KeyType.GamePad_Start, "xb_button_12" },
			// There's no suitable sprite, so use the least confusing one: this at least matches the name in the game's default Steam controller settings
			{ GameInput2.KeyType.GamePad_Select, "xb_button_11" },
			{ GameInput2.KeyType.GamePad_L3, "xb_button_09" },
			{ GameInput2.KeyType.GamePad_R3, "xb_button_10" },
			{ GameInput2.KeyType.GamePad_L3DI, "xb_button_09" },
			{ GameInput2.KeyType.GamePad_R3DI, "xb_button_10" },
		};
		static readonly string[][] mobileTouchDirectionInputTextureNames =
		{
			new string[3] { "ps_button_14", "ps_button_20", "ps_button_21" },
			new string[3] { "ps_button_26", "ps_button_27", "ps_button_09" },
			new string[3] { "ps_button_32", "ps_button_33", "ps_button_10" },
		};
		static readonly Dictionary<string, string> stickAndDpadEmojiForMobileTouch = new Dictionary<string, string> {
			// PS4's sticks
			{ "<sprite index=28>", "<sprite index=16>" },
			{ "<sprite index=29>", "<sprite index=17>" },
			{ "<sprite index=40>", "<sprite index=16>" },
			{ "<sprite index=41>", "<sprite index=17>" },
			// PS4's D-pad
			{ "<sprite index=30>", "<sprite index=18>" },
			{ "<sprite index=31>", "<sprite index=19>" },
			{ "<sprite index=42>", "<sprite index=18>" },
			{ "<sprite index=43>", "<sprite index=19>" },
			{ "<sprite index=45>", "<sprite index=44>" },
			{ "<sprite index=46>", "<sprite index=44>" },
			{ "<sprite index=94>", "<sprite index=91>" },
			{ "<sprite index=95>", "<sprite index=92>" },
			{ "<sprite index=96>", "<sprite index=93>" },
			{ "<sprite index=97>", "<sprite index=91>" },
			{ "<sprite index=98>", "<sprite index=92>" },
			{ "<sprite index=99>", "<sprite index=93>" },
		};

		static readonly Dictionary<GameInput2.KeyType, string> steamDeckTextureNameMap = new Dictionary<GameInput2.KeyType, string>
		{
			{ GameInput2.KeyType.GamePad_A, "sw_button_01" },
			{ GameInput2.KeyType.GamePad_B, "sw_button_02" },
			{ GameInput2.KeyType.GamePad_X, "sw_button_04" },
			{ GameInput2.KeyType.GamePad_Y, "sw_button_03" },
			// The shape isn't quite right, but it's the best we've got
			{ GameInput2.KeyType.GamePad_L1, "ps_button_05" },
			// The shape isn't quite right, but it's the best we've got
			{ GameInput2.KeyType.GamePad_R1, "ps_button_06" },
			{ GameInput2.KeyType.GamePad_L2, "ps_button_07" },
			{ GameInput2.KeyType.GamePad_R2, "ps_button_08" },
			{ GameInput2.KeyType.GamePad_Up, "xb_button_13" },
			{ GameInput2.KeyType.GamePad_Down, "xb_button_15" },
			{ GameInput2.KeyType.GamePad_Left, "xb_button_16" },
			{ GameInput2.KeyType.GamePad_Right, "xb_button_17" },
			{ GameInput2.KeyType.GamePad_RStickUp, "ps_button_28" },
			{ GameInput2.KeyType.GamePad_RStickDown, "ps_button_29" },
			{ GameInput2.KeyType.GamePad_RStickLeft, "ps_button_30" },
			{ GameInput2.KeyType.GamePad_RStickRight, "ps_button_31" },
			{ GameInput2.KeyType.GamePad_LStickUp, "ps_button_22" },
			{ GameInput2.KeyType.GamePad_LStickDown, "ps_button_23" },
			{ GameInput2.KeyType.GamePad_LStickLeft, "ps_button_24" },
			{ GameInput2.KeyType.GamePad_LStickRight, "ps_button_25" },
			// The shape isn't quite right, but it's the best we've got
			{ GameInput2.KeyType.GamePad_Start, "xb_button_12" },
			// The shape isn't quite right, but it's the best we've got
			{ GameInput2.KeyType.GamePad_Select, "xb_button_11" },
			{ GameInput2.KeyType.GamePad_L3, "ps_button_15" },
			{ GameInput2.KeyType.GamePad_R3, "ps_button_16" },
			{ GameInput2.KeyType.GamePad_L3DI, "ps_button_15" },
			{ GameInput2.KeyType.GamePad_R3DI, "ps_button_16" },
		};
		static readonly string[][] steamDeckDirectionInputTextureNames =
		{
			new string[3] { "xb_button_14", "xb_button_18", "xb_button_19" },
			new string[3] { "ps_button_26", "ps_button_27", "ps_button_09" },
			new string[3] { "ps_button_32", "ps_button_33", "ps_button_10" },
		};
		static readonly Dictionary<GameInput2.KeyType, string> gamepadButtonKeyTypeToEmojiStrMapSteamDeck = new Dictionary<GameInput2.KeyType, string>
		{
			{ GameInput2.KeyType.GamePad_B, "<sprite index=21>" },
			{ GameInput2.KeyType.GamePad_A, "<sprite index=20>" },
			{ GameInput2.KeyType.GamePad_X, "<sprite index=23>" },
			{ GameInput2.KeyType.GamePad_Y, "<sprite index=22>" },
			{ GameInput2.KeyType.GamePad_L1, "<sprite index=12>" },
			{ GameInput2.KeyType.GamePad_R1, "<sprite index=13>" },
			{ GameInput2.KeyType.GamePad_L2, "<sprite index=14>" },
			{ GameInput2.KeyType.GamePad_R2, "<sprite index=15>" },
			{ GameInput2.KeyType.GamePad_L3, "<sprite index=126>" },
			{ GameInput2.KeyType.GamePad_R3, "<sprite index=127>" },
			{ GameInput2.KeyType.GamePad_Select, "<sprite index=128>" },
		};
		static readonly Dictionary<string, string> stickAndDpadEmojiForSteamDeck = new Dictionary<string, string> {
			// PS4's sticks
			{ "<sprite index=28>", "<sprite index=16>" },
			{ "<sprite index=29>", "<sprite index=17>" },
			{ "<sprite index=40>", "<sprite index=16>" },
			{ "<sprite index=41>", "<sprite index=17>" },
			// Xbox's D-pad
			{ "<sprite index=18>", "<sprite index=42>" },
			{ "<sprite index=19>", "<sprite index=43>" },
			{ "<sprite index=30>", "<sprite index=42>" },
			{ "<sprite index=31>", "<sprite index=43>" },
			{ "<sprite index=44>", "<sprite index=46>" },
			{ "<sprite index=45>", "<sprite index=46>" },
			{ "<sprite index=91>", "<sprite index=97>" },
			{ "<sprite index=92>", "<sprite index=98>" },
			{ "<sprite index=93>", "<sprite index=99>" },
			{ "<sprite index=94>", "<sprite index=97>" },
			{ "<sprite index=95>", "<sprite index=98>" },
			{ "<sprite index=96>", "<sprite index=99>" },
		};

		static readonly Dictionary<GameInput2.KeyType, string> steamControllerTextureNameMap = new Dictionary<GameInput2.KeyType, string>
		{
			{ GameInput2.KeyType.GamePad_A, "xb_button_02" },
			{ GameInput2.KeyType.GamePad_B, "xb_button_01" },
			{ GameInput2.KeyType.GamePad_X, "xb_button_03" },
			{ GameInput2.KeyType.GamePad_Y, "xb_button_04" },
			// The shape isn't right, but it's the best we've got
			{ GameInput2.KeyType.GamePad_L1, "xb_button_05" },
			// The shape isn't right, but it's the best we've got
			{ GameInput2.KeyType.GamePad_R1, "xb_button_06" },
			// The shape isn't right, but it's the best we've got
			{ GameInput2.KeyType.GamePad_L2, "xb_button_07" },
			// The shape isn't right, but it's the best we've got
			{ GameInput2.KeyType.GamePad_R2, "xb_button_08" },
			{ GameInput2.KeyType.GamePad_Up, "xb_button_13" },
			{ GameInput2.KeyType.GamePad_Down, "xb_button_15" },
			{ GameInput2.KeyType.GamePad_Left, "xb_button_16" },
			{ GameInput2.KeyType.GamePad_Right, "xb_button_17" },
			{ GameInput2.KeyType.GamePad_RStickUp, "ps_button_28" },
			{ GameInput2.KeyType.GamePad_RStickDown, "ps_button_29" },
			{ GameInput2.KeyType.GamePad_RStickLeft, "ps_button_30" },
			{ GameInput2.KeyType.GamePad_RStickRight, "ps_button_31" },
			{ GameInput2.KeyType.GamePad_LStickUp, "xb_button_20" },
			{ GameInput2.KeyType.GamePad_LStickDown, "xb_button_21" },
			{ GameInput2.KeyType.GamePad_LStickLeft, "xb_button_22" },
			{ GameInput2.KeyType.GamePad_LStickRight, "xb_button_23" },
			// There's no suitable sprite, so use the least confusing one: this at least matches the name in the game's default Steam controller settings
			{ GameInput2.KeyType.GamePad_Start, "xb_button_12" },
			// There's no suitable sprite, so use the least confusing one: this at least matches the name in the game's default Steam controller settings
			{ GameInput2.KeyType.GamePad_Select, "xb_button_11" },
			{ GameInput2.KeyType.GamePad_L3, "xb_button_09" },
			{ GameInput2.KeyType.GamePad_R3, "ps_button_10" },
			{ GameInput2.KeyType.GamePad_L3DI, "xb_button_09" },
			{ GameInput2.KeyType.GamePad_R3DI, "ps_button_10" },
			// Left trackpad
			//{ GameInput2.KeyType.NpadButton_StickLUp, "ps_button_22" },
			//{ GameInput2.KeyType.NpadButton_StickLDown, "ps_button_23" },
			//{ GameInput2.KeyType.NpadButton_StickLLeft, "ps_button_24" },
			//{ GameInput2.KeyType.NpadButton_StickLRight, "ps_button_25" },
			//{ GameInput2.KeyType.NpadButton_StickL, "ps_button_15" },
		};
		static readonly string[][] steamControllerDirectionInputTextureNames =
		{
			new string[3] { "xb_button_14", "xb_button_18", "xb_button_19" },
			new string[3] { "xb_button_24", "xb_button_25", "xb_button_09" },
			new string[3] { "ps_button_32", "ps_button_33", "ps_button_10" },
		};
		static readonly Dictionary<GameInput2.KeyType, string> gamepadButtonKeyTypeToEmojiStrMapSteamController = new Dictionary<GameInput2.KeyType, string>
		{
			{ GameInput2.KeyType.GamePad_B, "<sprite index=32>" },
			{ GameInput2.KeyType.GamePad_A, "<sprite index=33>" },
			{ GameInput2.KeyType.GamePad_X, "<sprite index=34>" },
			{ GameInput2.KeyType.GamePad_Y, "<sprite index=35>" },
			{ GameInput2.KeyType.GamePad_L1, "<sprite index=36>" },
			{ GameInput2.KeyType.GamePad_R1, "<sprite index=37>" },
			{ GameInput2.KeyType.GamePad_L2, "<sprite index=38>" },
			{ GameInput2.KeyType.GamePad_R2, "<sprite index=39>" },
			{ GameInput2.KeyType.GamePad_L3, "<sprite index=40>" },
			{ GameInput2.KeyType.GamePad_R3, "<sprite index=17>" },
			{ GameInput2.KeyType.GamePad_Select, "<sprite index=128>" },
		};
		static readonly Dictionary<string, string> stickAndDpadEmojiForSteamController = new Dictionary<string, string> {
			// PS4's right stick
			{ "<sprite index=29>", "<sprite index=17>" },
			{ "<sprite index=41>", "<sprite index=17>" },
		};

		// Random int values to avoid clashing with the base game and other mods
		const GamePadDeviceType JoyConGamePadDeviceType = (GamePadDeviceType)1083105190;
		const GamePadDeviceType MobileTouchGamePadDeviceType = (GamePadDeviceType)2067307885;
		const GamePadDeviceType SteamDeckGamePadDeviceType = (GamePadDeviceType)(-125593974);

		static string ReplaceAll(this string text, Dictionary<string, string> substitutions)
		{
			var regex = new Regex(substitutions.Keys.Select(s => Regex.Escape(s)).Join(null, "|"));
			return regex.Replace(text, m => substitutions[m.Value]);
		}

		static string AdaptStickAndDpadEmojiToGamePad(string text, GamePadDeviceType gamePadDeviceType)
		{
			switch (gamePadDeviceType)
			{
				case GamePadDeviceType.STEAM:
					return text.ReplaceAll(stickAndDpadEmojiForSteamController);
				case JoyConGamePadDeviceType:
					return text.ReplaceAll(stickAndDpadEmojiForJoyCon);
				case MobileTouchGamePadDeviceType:
					return text.ReplaceAll(stickAndDpadEmojiForMobileTouch);
				case SteamDeckGamePadDeviceType:
					return text.ReplaceAll(stickAndDpadEmojiForSteamDeck);
				default:
					return text;
			}
		}

		static string AdaptDefaultRebindableEmojiToGamePad(string text, GamePadDeviceType gamePadDeviceType)
		{
			switch (gamePadDeviceType)
			{
				case JoyConGamePadDeviceType:
				case MobileTouchGamePadDeviceType:
				case SteamDeckGamePadDeviceType:
					return new Regex("<sprite index=[^>]*>").Replace(text, m =>
					{
						var keyType = SurvivorDefine.GetGamepadButtonKeyTypeFromEmojiStr(m.Value);
						if (keyType == (GameInput2.KeyType)(-1))
							return m.Value;
						else
							return SurvivorDefine.GetEmojiStrFromGamepadButtonKeyType(gamePadDeviceType, keyType);
					});

				default:
					return text;
			}
		}

		static int EnsureSteamControllerTextureNameMapIndex(
			ref Dictionary<GameInput2.KeyType, string>[] ___gamePadTextureNameMap,
			ref string[][][] ___directionInputTetureNames)
		{
			int steamControllerTextureNameMapIndex = Array.IndexOf(___gamePadTextureNameMap, steamControllerTextureNameMap);
			if (steamControllerTextureNameMapIndex == -1)
			{
				steamControllerTextureNameMapIndex = Math.Max(___gamePadTextureNameMap.Length, ___directionInputTetureNames.Length);

				Array.Resize(ref ___gamePadTextureNameMap, steamControllerTextureNameMapIndex + 1);
				___gamePadTextureNameMap[steamControllerTextureNameMapIndex] = steamControllerTextureNameMap;

				Array.Resize(ref ___directionInputTetureNames, steamControllerTextureNameMapIndex + 1);
				___directionInputTetureNames[steamControllerTextureNameMapIndex] = steamControllerDirectionInputTextureNames;
			}
			return steamControllerTextureNameMapIndex;
		}

		static int EnsureJoyConTextureNameMapIndex(
			ref Dictionary<GameInput2.KeyType, string>[] ___gamePadTextureNameMap,
			ref string[][][] ___directionInputTetureNames)
		{
			int joyConTextureNameMapIndex = Array.IndexOf(___gamePadTextureNameMap, joyConTextureNameMap);
			if (joyConTextureNameMapIndex == -1)
			{
				joyConTextureNameMapIndex = Math.Max(___gamePadTextureNameMap.Length, ___directionInputTetureNames.Length);

				Array.Resize(ref ___gamePadTextureNameMap, joyConTextureNameMapIndex + 1);
				___gamePadTextureNameMap[joyConTextureNameMapIndex] = joyConTextureNameMap;

				Array.Resize(ref ___directionInputTetureNames, joyConTextureNameMapIndex + 1);
				___directionInputTetureNames[joyConTextureNameMapIndex] = joyConDirectionInputTextureNames;
			}
			return joyConTextureNameMapIndex;
		}

		static int EnsureMobileTouchTextureNameMapIndex(
			ref Dictionary<GameInput2.KeyType, string>[] ___gamePadTextureNameMap,
			ref string[][][] ___directionInputTetureNames)
		{
			int mobileTouchTextureNameMapIndex = Array.IndexOf(___gamePadTextureNameMap, mobileTouchTextureNameMap);
			if (mobileTouchTextureNameMapIndex == -1)
			{
				mobileTouchTextureNameMapIndex = Math.Max(___gamePadTextureNameMap.Length, ___directionInputTetureNames.Length);

				Array.Resize(ref ___gamePadTextureNameMap, mobileTouchTextureNameMapIndex + 1);
				___gamePadTextureNameMap[mobileTouchTextureNameMapIndex] = mobileTouchTextureNameMap;

				Array.Resize(ref ___directionInputTetureNames, mobileTouchTextureNameMapIndex + 1);
				___directionInputTetureNames[mobileTouchTextureNameMapIndex] = mobileTouchDirectionInputTextureNames;
			}
			return mobileTouchTextureNameMapIndex;
		}

		static int EnsureSteamDeckTextureNameMapIndex(
			ref Dictionary<GameInput2.KeyType, string>[] ___gamePadTextureNameMap,
			ref string[][][] ___directionInputTetureNames)
		{
			int steamDeckTextureNameMapIndex = Array.IndexOf(___gamePadTextureNameMap, steamDeckTextureNameMap);
			if (steamDeckTextureNameMapIndex == -1)
			{
				steamDeckTextureNameMapIndex = Math.Max(___gamePadTextureNameMap.Length, ___directionInputTetureNames.Length);

				Array.Resize(ref ___gamePadTextureNameMap, steamDeckTextureNameMapIndex + 1);
				___gamePadTextureNameMap[steamDeckTextureNameMapIndex] = steamDeckTextureNameMap;

				Array.Resize(ref ___directionInputTetureNames, steamDeckTextureNameMapIndex + 1);
				___directionInputTetureNames[steamDeckTextureNameMapIndex] = steamDeckDirectionInputTextureNames;
			}
			return steamDeckTextureNameMapIndex;
		}

		[HarmonyPatch(typeof(SteamGamePad), MethodType.Constructor)]
		[HarmonyPrefix]
		static void SteamGamePad_preconstruct(Dictionary<Steamworks.EInputActionOrigin, GameInput2.KeyType[]> ___eInputActionOriginToKeyInputMap)
		{
			// Single Switch JoyCon
			___eInputActionOriginToKeyInputMap[Steamworks.EInputActionOrigin.k_EInputActionOrigin_Switch_Reserved12] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_A };
			___eInputActionOriginToKeyInputMap[Steamworks.EInputActionOrigin.k_EInputActionOrigin_Switch_Reserved13] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_B };
			___eInputActionOriginToKeyInputMap[Steamworks.EInputActionOrigin.k_EInputActionOrigin_Switch_Reserved11] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_X };
			___eInputActionOriginToKeyInputMap[Steamworks.EInputActionOrigin.k_EInputActionOrigin_Switch_Reserved14] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Y };

			// Steam Deck
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_A] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_A };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_B] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_B };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_X] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_X };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_Y] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Y };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_L1] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_L1 };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_R1] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_R1 };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_Menu] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Start };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_View] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Select };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_LeftPad_Swipe] = new GameInput2.KeyType[4]
			//{
			//		GameInput2.KeyType.GamePad_LStickUp,
			//		GameInput2.KeyType.GamePad_LStickDown,
			//		GameInput2.KeyType.GamePad_LStickLeft,
			//		GameInput2.KeyType.GamePad_LStickRight
			//};
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_LeftPad_Click] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_L3 };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_LeftPad_DPadNorth] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Up };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_LeftPad_DPadSouth] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Down };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_LeftPad_DPadWest] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Left };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_LeftPad_DPadEast] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Right };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_RightPad_Swipe] = new GameInput2.KeyType[4]
			//{
			//		GameInput2.KeyType.GamePad_RStickUp,
			//		GameInput2.KeyType.GamePad_RStickDown,
			//		GameInput2.KeyType.GamePad_RStickLeft,
			//		GameInput2.KeyType.GamePad_RStickRight
			//};
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_RightPad_Click] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_R3 };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_RightPad_DPadNorth] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Y };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_RightPad_DPadSouth] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_A };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_RightPad_DPadWest] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_X };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_RightPad_DPadEast] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_B };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_L2_SoftPull] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_L2 };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_L2] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_L2 };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_R2_SoftPull] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_R2 };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_R2] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_R2 };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_LeftStick_Move] = new GameInput2.KeyType[4]
			{
					GameInput2.KeyType.GamePad_LStickUp,
					GameInput2.KeyType.GamePad_LStickDown,
					GameInput2.KeyType.GamePad_LStickLeft,
					GameInput2.KeyType.GamePad_LStickRight
			};
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_L3] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_L3 };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_LeftStick_DPadNorth] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Up };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_LeftStick_DPadSouth] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Down };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_LeftStick_DPadWest] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Left };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_LeftStick_DPadEast] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Right };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_RightStick_Move] = new GameInput2.KeyType[4]
			{
					GameInput2.KeyType.GamePad_RStickUp,
					GameInput2.KeyType.GamePad_RStickDown,
					GameInput2.KeyType.GamePad_RStickLeft,
					GameInput2.KeyType.GamePad_RStickRight
			};
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_R3] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_R3 };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_RightStick_DPadNorth] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Up };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_RightStick_DPadSouth] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Down };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_RightStick_DPadWest] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Left };
			//___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_RightStick_DPadEast] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Right };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_DPad_North] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Up };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_DPad_South] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Down };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_DPad_West] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Left };
			___eInputActionOriginToKeyInputMap[(Steamworks.EInputActionOrigin)EInputActionOrigin.k_EInputActionOrigin_SteamDeck_DPad_East] = new GameInput2.KeyType[1] { GameInput2.KeyType.GamePad_Right };
		}

		[HarmonyPatch(typeof(InputTextureDefine), "GetTextureNameMapIndex")]
		[HarmonyPostfix]
		static void GetTextureNameMapIndex_postfix(
			ref int __result,
			ref Dictionary<GameInput2.KeyType, string>[] ___gamePadTextureNameMap,
			ref string[][][] ___directionInputTetureNames,
			Dictionary<GameInput2.KeyType, string> ___steamSwitchTextureNameMap)
		{
#if DEBUG
			var resultBeforePatch = __result;
#endif

			switch (GameInput2.GetGamePadType())
			{
				case GamePadDeviceType.SWITCH:
					if (___steamSwitchTextureNameMap[GameInput2.KeyType.GamePad_L3] == "sw_button_15")
					{
						___steamSwitchTextureNameMap[GameInput2.KeyType.GamePad_L3] = "sw_button_09";
						___steamSwitchTextureNameMap[GameInput2.KeyType.GamePad_R3] = "sw_button_10";
						___steamSwitchTextureNameMap[GameInput2.KeyType.GamePad_L3DI] = "sw_button_09";
						___steamSwitchTextureNameMap[GameInput2.KeyType.GamePad_R3DI] = "sw_button_10";
					}
					break;
				case GamePadDeviceType.STEAM:
					__result = EnsureSteamControllerTextureNameMapIndex(ref ___gamePadTextureNameMap, ref ___directionInputTetureNames);
					break;
				case JoyConGamePadDeviceType:
					__result = EnsureJoyConTextureNameMapIndex(ref ___gamePadTextureNameMap, ref ___directionInputTetureNames);
					break;
				case MobileTouchGamePadDeviceType:
					__result = EnsureMobileTouchTextureNameMapIndex(ref ___gamePadTextureNameMap, ref ___directionInputTetureNames);
					break;
				case SteamDeckGamePadDeviceType:
					__result = EnsureSteamDeckTextureNameMapIndex(ref ___gamePadTextureNameMap, ref ___directionInputTetureNames);
					break;
			}

#if DEBUG
			var myLogSource = BepInEx.Logging.Logger.CreateLogSource("SteamInput");
			myLogSource.LogInfo($"Changing GetTextureNameMapIndex result from {resultBeforePatch} to {__result}");
			BepInEx.Logging.Logger.Sources.Remove(myLogSource);
#endif
		}

#if !SILENT
		class GamePadConfigurationSnapshot
		{
			public int connectedControllers;
			public ESteamInputType[] inputTypes;
			public uint[] remotePlaySessionIds;
			public EInputActionOrigin[,,] actionOrigins;

			public override bool Equals(object obj)
			{
				return Equals(obj as GamePadConfigurationSnapshot);
			}

			public bool Equals(GamePadConfigurationSnapshot other)
			{
				return other != null
					&& connectedControllers == other.connectedControllers
					&& inputTypes.SequenceEqual(other.inputTypes)
					&& remotePlaySessionIds.SequenceEqual(other.remotePlaySessionIds)
					&& actionOrigins.Cast<EInputActionOrigin>().SequenceEqual(other.actionOrigins.Cast<EInputActionOrigin>());
			}

			public override int GetHashCode()
			{
				var hashCode = new HashCode();
				hashCode.Add(connectedControllers);
				foreach (var inputType in inputTypes)
					hashCode.Add(inputType);
				foreach (var remotePlaySessionId in remotePlaySessionIds)
					hashCode.Add(remotePlaySessionId);
				foreach (var actionOrigin in actionOrigins)
					hashCode.Add(actionOrigin);
				return hashCode.ToHashCode();
			}
		}

		static GamePadConfigurationSnapshot lastGamePadConfiguration = null;
#endif

		enum GamePadDevicePriority
		{
			EXPLICITLY_SUPPORTED,
			GENERIC,
			BUILTIN_HARDWARE,
			SINGLE_JOY_CON,
			NONE,
		}

		[HarmonyPatch(typeof(SteamGamePad), "CheckGamePadType")]
		[HarmonyPrefix]
		static bool CheckGamePadType(
			SteamGamePad __instance,
			ref GamePadDeviceType ___gamePadType,
			ref InputHandle_t ___inputHandle_t,
			InputHandle_t[] ___controllerHandles,
			bool ___isInitalized,
			string[] ___ActionHandleNames,
			Steamworks.EInputActionOrigin[] ___m_eInputActionOrigins,
			Dictionary<Steamworks.EInputActionOrigin, GameInput2.KeyType[]> ___eInputActionOriginToKeyInputMap)
		{
			var IsAnalogActionHandleType = AccessTools.Method(typeof(SteamGamePad), "IsAnalogActionHandleType").CreateDelegate<Func<ActionHandleType, bool>>();
			var actionSetHandle = SteamInput.GetActionSetHandle("MenuControls");
			int connectedControllers = SteamInput.GetConnectedControllers(___controllerHandles);

#if !SILENT
			var gamePadConfiguration = new GamePadConfigurationSnapshot
			{
				connectedControllers = connectedControllers,
				inputTypes = new ESteamInputType[connectedControllers],
				remotePlaySessionIds = new uint[connectedControllers],
				actionOrigins = new EInputActionOrigin[connectedControllers, ___ActionHandleNames.Length, Constants.STEAM_INPUT_MAX_ORIGINS],
			};
#endif

			bool foundRemote = false;
			GamePadDevicePriority foundPriority = GamePadDevicePriority.NONE;
			InputHandle_t foundInputHandle = default(InputHandle_t);
			___gamePadType = GamePadDeviceType.NONE;
			for (int i = 0; i < connectedControllers; i++)
			{
				GamePadDeviceType gamePadDeviceType;
				GamePadDevicePriority priority;

				var inputHandle = ___controllerHandles[i];
				var inputType = (ESteamInputType)SteamInput.GetInputTypeForHandle(inputHandle);
				var remotePlaySessionId = SteamInput.GetRemotePlaySessionID(inputHandle);
				bool isRemote = remotePlaySessionId != 0;
#if !SILENT
				gamePadConfiguration.inputTypes[i] = inputType;
				gamePadConfiguration.remotePlaySessionIds[i] = remotePlaySessionId;
#endif
				switch (inputType)
				{
					case ESteamInputType.k_ESteamInputType_SteamController:
						gamePadDeviceType = GamePadDeviceType.STEAM;
						priority = GamePadDevicePriority.EXPLICITLY_SUPPORTED;
						break;
					case ESteamInputType.k_ESteamInputType_XBox360Controller:
					case ESteamInputType.k_ESteamInputType_XBoxOneController:
						gamePadDeviceType = GamePadDeviceType.XBOX;
						priority = GamePadDevicePriority.EXPLICITLY_SUPPORTED;
						break;
					case ESteamInputType.k_ESteamInputType_PS4Controller:
					case ESteamInputType.k_ESteamInputType_PS3Controller:
					case ESteamInputType.k_ESteamInputType_PS5Controller:
						gamePadDeviceType = GamePadDeviceType.PS4;
						priority = GamePadDevicePriority.EXPLICITLY_SUPPORTED;
						break;
					//case ESteamInputType.k_ESteamInputType_AppleMFiController:
					//case ESteamInputType.k_ESteamInputType_AndroidController:
					//	gamePadDeviceType = GamePadDeviceType.XBOX;
					//	priority = LocalGamePadDevicePriority.MOBILE_PHYSICAL;
					//	break;
					case ESteamInputType.k_ESteamInputType_SwitchJoyConPair:
					case ESteamInputType.k_ESteamInputType_SwitchProController:
						gamePadDeviceType = GamePadDeviceType.SWITCH;
						priority = GamePadDevicePriority.EXPLICITLY_SUPPORTED;
						break;
					case ESteamInputType.k_ESteamInputType_SwitchJoyConSingle:
						gamePadDeviceType = JoyConGamePadDeviceType;
						priority = GamePadDevicePriority.SINGLE_JOY_CON;
						break;
					case ESteamInputType.k_ESteamInputType_MobileTouch:
						gamePadDeviceType = MobileTouchGamePadDeviceType;
						priority = GamePadDevicePriority.BUILTIN_HARDWARE;
						break;
					case ESteamInputType.k_ESteamInputType_SteamDeckController:
						gamePadDeviceType = SteamDeckGamePadDeviceType;
						priority = GamePadDevicePriority.BUILTIN_HARDWARE;
						break;
					default:
						gamePadDeviceType = GamePadDeviceType.XBOX;
						priority = GamePadDevicePriority.GENERIC;
						break;
				}

				// Override the reported type if specific action origins are
				// detected. GetInputTypeForHandle never returns values newer
				// than the Steamworks SDK library build that is shipped with
				// the game, but action origins may use newer values.
				// Specifically, this affects the Steam Deck. Add PlayStation
				// origins in case it helps third-party controllers.
				//
				// Conceptually, action origins should be *the* thing that
				// defines on-screen prompts, as they supposedly denote the
				// exact hardware controls. Unfortunately, Steam does reuse
				// some origins across controller types, e. g. PS4 origins for
				// PS3 controllers and Xbox360 origins for MobileTouch, plus
				// we want to show special prompts for a lone Joy-Con which
				// genuinely shares its hardware with paired Joy-Con; so we
				// still need to check both the origins and the type and be
				// careful about which origins can override the type here.
				for (int j = 0; j < ___ActionHandleNames.Length; j++)
				{
					int n;
					if (IsAnalogActionHandleType((ActionHandleType)j))
					{
						var actionHandle = SteamInput.GetAnalogActionHandle(___ActionHandleNames[j]);
						n = SteamInput.GetAnalogActionOrigins(inputHandle, actionSetHandle, actionHandle, ___m_eInputActionOrigins);
					}
					else
					{
						var actionHandle = SteamInput.GetDigitalActionHandle(___ActionHandleNames[j]);
						n = SteamInput.GetDigitalActionOrigins(inputHandle, actionSetHandle, actionHandle, ___m_eInputActionOrigins);
					}
#if !SILENT
					for (int k = 0; k < n; k++)
						gamePadConfiguration.actionOrigins[i, j, k] = (EInputActionOrigin)___m_eInputActionOrigins[k];
#endif

					for (int k = 0; k < n; k++)
					{
						var origin = (EInputActionOrigin)___m_eInputActionOrigins[k];
						if (___eInputActionOriginToKeyInputMap.ContainsKey((Steamworks.EInputActionOrigin)origin))
						{
							if (origin >= EInputActionOrigin.k_EInputActionOrigin_SteamDeck_A && origin <= EInputActionOrigin.k_EInputActionOrigin_SteamDeck_Reserved20)
							{
								gamePadDeviceType = SteamDeckGamePadDeviceType;
								priority = GamePadDevicePriority.BUILTIN_HARDWARE;
#if SILENT
								goto actionHandlesInspected;
#endif
							}
							else if (origin >= EInputActionOrigin.k_EInputActionOrigin_PS4_X && origin <= EInputActionOrigin.k_EInputActionOrigin_PS4_Reserved10
								|| origin >= EInputActionOrigin.k_EInputActionOrigin_PS5_X && origin <= EInputActionOrigin.k_EInputActionOrigin_PS5_Reserved20)
							{
								gamePadDeviceType = GamePadDeviceType.PS4;
								priority = GamePadDevicePriority.EXPLICITLY_SUPPORTED;
#if SILENT
								goto actionHandlesInspected;
#endif
							}
							break;
						}
					}
				}

#if SILENT
			actionHandlesInspected:
				;
#endif

				if (isRemote && !foundRemote || isRemote == foundRemote && priority < foundPriority)
				{
					___gamePadType = gamePadDeviceType;
					foundInputHandle = inputHandle;
					foundRemote = isRemote;
					foundPriority = priority;
#if SILENT
					if (isRemote && priority == GamePadDevicePriority.EXPLICITLY_SUPPORTED)
						break;
#endif
				}
			}
			if (!___isInitalized || !___inputHandle_t.Equals(foundInputHandle))
			{
				___inputHandle_t = foundInputHandle;
				AccessTools.Method(typeof(SteamGamePad), "InitSteamInputInitActionHandle").Invoke(__instance, null);
			}

#if !SILENT
			if (!gamePadConfiguration.Equals(lastGamePadConfiguration))
			{
				lastGamePadConfiguration = gamePadConfiguration;

				var logger = GamepadSupportPlugin.Logger;

#if DEBUG
				logger.LogInfo($"Action set handle \"MenuControls\": {actionSetHandle}");
				for (int i = 0; i < ___ActionHandleNames.Length; i++)
				{
					if (IsAnalogActionHandleType((ActionHandleType)i))
						logger.LogInfo($"Analog action handle #{i} \"{___ActionHandleNames[i]}\": {SteamInput.GetAnalogActionHandle(___ActionHandleNames[i])}");
					else
						logger.LogInfo($"Digital action handle #{i} \"{___ActionHandleNames[i]}\": {SteamInput.GetDigitalActionHandle(___ActionHandleNames[i])}");
				}
#endif

				logger.LogInfo($"{connectedControllers} controller{(connectedControllers == 1 ? " is" : "s are")} connected.");
				for (int i = 0; i < connectedControllers; i++)
				{
					var remotePlaySessionId = gamePadConfiguration.remotePlaySessionIds[i];
					logger.LogInfo($"Controller #{i} ({___controllerHandles[i]}) of type {gamePadConfiguration.inputTypes[i]} ({(remotePlaySessionId == 0 ? "local" : $"remote session {remotePlaySessionId}")}):");
					for (int j = 0; j < ___ActionHandleNames.Length; j++)
					{
						var n = Constants.STEAM_INPUT_MAX_ORIGINS;
						while (n > 0 && gamePadConfiguration.actionOrigins[i, j, n - 1] == EInputActionOrigin.k_EInputActionOrigin_None)
							--n;
						var builder = new StringBuilder($"  action \"{___ActionHandleNames[j]}\" has {n} origin");
						if (n != 1)
							builder.Append('s');
						for (int k = 0; k < n; k++)
						{
							var origin = gamePadConfiguration.actionOrigins[i, j, k];
							builder.Append(k == 0 ? ": " : ", ");
							if (origin < EInputActionOrigin.k_EInputActionOrigin_Count)
								builder.Append(origin);
							else
							{
								builder.Append((int)origin);
								builder.Append(" ~= ");
								builder.Append((EInputActionOrigin)SteamInput.TranslateActionOrigin(Steamworks.ESteamInputType.k_ESteamInputType_Unknown, (Steamworks.EInputActionOrigin)origin));
							}
						}
						logger.LogInfo(builder.ToString());
					}
				}
			}
#endif

			return false;
		}

		static Steamworks.ESteamInputType BestSteamInputType
		{
			get
			{
				switch (GameInput2.GetGamePadType())
				{
					case GamePadDeviceType.PS4:
						return Steamworks.ESteamInputType.k_ESteamInputType_PS4Controller;
					case GamePadDeviceType.XBOX:
						return Steamworks.ESteamInputType.k_ESteamInputType_XBoxOneController;
					case GamePadDeviceType.SWITCH:
						return Steamworks.ESteamInputType.k_ESteamInputType_SwitchProController;
					case GamePadDeviceType.STEAM:
						return Steamworks.ESteamInputType.k_ESteamInputType_SteamController;
					case JoyConGamePadDeviceType:
						return Steamworks.ESteamInputType.k_ESteamInputType_SwitchJoyConSingle;
					case MobileTouchGamePadDeviceType:
						return Steamworks.ESteamInputType.k_ESteamInputType_MobileTouch;
					case SteamDeckGamePadDeviceType:
						return (Steamworks.ESteamInputType)ESteamInputType.k_ESteamInputType_SteamDeckController;
					default:
						return Steamworks.ESteamInputType.k_ESteamInputType_Unknown;
				}
			}
		}

		static bool TryMapInputActionOrigin(
			Dictionary<Steamworks.EInputActionOrigin, GameInput2.KeyType[]> eInputActionOriginToKeyInputMap,
			Steamworks.EInputActionOrigin[] eInputActionOrigins,
			int n,
			out GameInput2.KeyType[] keyTypes)
		{
			GameInput2.KeyType[] value = null;
			if (eInputActionOrigins.Take(n).Any(origin => eInputActionOriginToKeyInputMap.TryGetValue(origin, out value)) ||
				eInputActionOrigins.Take(n).Any(origin => eInputActionOriginToKeyInputMap.TryGetValue(
					SteamInput.TranslateActionOrigin(BestSteamInputType, origin), out value)) ||
				eInputActionOrigins.Take(n).Any(origin => eInputActionOriginToKeyInputMap.TryGetValue(
					SteamInput.TranslateActionOrigin(Steamworks.ESteamInputType.k_ESteamInputType_Unknown, origin), out value)))
			{
				keyTypes = value;
				return true;
			}

			keyTypes = null;
			return false;
		}

		internal delegate bool MyDigitalActionHandle_InitKeyType_delegate(
			InputHandle_t inputHandle_t,
			InputActionSetHandle_t actionSetHandle,
			Steamworks.EInputActionOrigin[] eInputActionOrigins,
			ref GameInput2.KeyType[] ___keyTypes,
			InputDigitalActionHandle_t ___m_actionHandle_t);

		internal static bool MyDigitalActionHandle_InitKeyType_prefix(
			InputHandle_t inputHandle_t,
			InputActionSetHandle_t actionSetHandle,
			Steamworks.EInputActionOrigin[] eInputActionOrigins,
			ref GameInput2.KeyType[] ___keyTypes,
			InputDigitalActionHandle_t ___m_actionHandle_t)
		{
			if (___keyTypes == null)
				___keyTypes = new GameInput2.KeyType[1];
			// Patch 1: make sure to set this to -1 if it isn't set
			// to a valid value. The official code leaves it with the
			// value of 0 if an origin exists but is unrecognized.
			___keyTypes[0] = (GameInput2.KeyType)(-1);

			// Patch 2: check *all* origins to see if
			// we recognize any, not just the first one.
			int n = SteamInput.GetDigitalActionOrigins(inputHandle_t, actionSetHandle, ___m_actionHandle_t, eInputActionOrigins);
			if (n != 0)
			{
				var eInputActionOriginToKeyInputMap = (Dictionary<Steamworks.EInputActionOrigin, GameInput2.KeyType[]>)AccessTools.Field(typeof(SteamGamePad), "eInputActionOriginToKeyInputMap").GetValue(null);
				// Patch 3: handle newer origins
				if (TryMapInputActionOrigin(eInputActionOriginToKeyInputMap, eInputActionOrigins, n, out var value))
					___keyTypes[0] = value[0];
			}

			return false;
		}

		internal delegate bool MyAnalogActionHandle_InitKeyType_delegate(
			InputHandle_t inputHandle_t,
			InputActionSetHandle_t actionSetHandle,
			Steamworks.EInputActionOrigin[] eInputActionOrigins,
			ref GameInput2.KeyType[] ___keyTypes,
			ActionHandleType ___m_actionHandleType,
			InputAnalogActionHandle_t ___m_actionHandle_t);

		internal static bool MyAnalogActionHandle_InitKeyType_prefix(
			InputHandle_t inputHandle_t,
			InputActionSetHandle_t actionSetHandle,
			Steamworks.EInputActionOrigin[] eInputActionOrigins,
			ref GameInput2.KeyType[] ___keyTypes,
			ActionHandleType ___m_actionHandleType,
			InputAnalogActionHandle_t ___m_actionHandle_t)
		{
			if (___keyTypes == null)
			{
				if (___m_actionHandleType == ActionHandleType.LStick || ___m_actionHandleType == ActionHandleType.RStick)
					___keyTypes = new GameInput2.KeyType[4];
				else
					___keyTypes = new GameInput2.KeyType[1];
			}
			for (int i = 0; i < ___keyTypes.Length; i++)
				___keyTypes[i] = (GameInput2.KeyType)(-1);

			// Patch 1: check *all* origins to see if
			// we recognize any, not just the first one.
			int n = SteamInput.GetAnalogActionOrigins(inputHandle_t, actionSetHandle, ___m_actionHandle_t, eInputActionOrigins);
			if (n != 0)
			{
				var eInputActionOriginToKeyInputMap = (Dictionary<Steamworks.EInputActionOrigin, GameInput2.KeyType[]>)AccessTools.Field(typeof(SteamGamePad), "eInputActionOriginToKeyInputMap").GetValue(null);
				// Patch 2: handle newer origins
				if (TryMapInputActionOrigin(eInputActionOriginToKeyInputMap, eInputActionOrigins, n, out var value))
				{
					for (int j = 0; j < ___keyTypes.Length; j++)
						___keyTypes[j] = value[j];
				}
			}

			return false;
		}

		[HarmonyPatch(typeof(SteamGamePad), "InitKeyTypes")]
		[HarmonyPostfix]
		static void InitKeyTypes_postfix(
			Dictionary<GameInput2.InputType, GameInput2.KeyType> ___defaultInputTypeToKeyTypeMap,
			object[] ___myActionHandleBases)
		{
			var typeofMyActionHandleBase = AccessTools.Inner(typeof(SteamGamePad), "MyActionHandleBase");
			var GetKeyType = AccessTools.Method(typeofMyActionHandleBase, "GetKeyType").GetFastDelegate();
			var KeyTypes = AccessTools.FieldRefAccess<GameInput2.KeyType[]>(typeofMyActionHandleBase, "keyTypes");
			for (GameInput2.InputType input = 0; input < GameInput2.InputType.Max; input++)
			{
				int index = 0;
				switch (input)
				{
					case GameInput2.InputType.LStickUp:
						index = 0;
						break;
					case GameInput2.InputType.LStickDown:
						index = 1;
						break;
					case GameInput2.InputType.LStickRight:
						index = 3;
						break;
					case GameInput2.InputType.LStickLeft:
						index = 2;
						break;
					case GameInput2.InputType.RStickUp:
						index = 0;
						break;
					case GameInput2.InputType.RStickDown:
						index = 1;
						break;
					case GameInput2.InputType.RStickRight:
						index = 3;
						break;
					case GameInput2.InputType.RStickLeft:
						index = 2;
						break;
				}
				var myActionHandle = ___myActionHandleBases[(int)DefaultInputTypeToActionHandleTypeMap[input]];
				var keyType = (GameInput2.KeyType)GetKeyType(myActionHandle, index);
				if (keyType == (GameInput2.KeyType)(-1))
				{
					var keyTypes = KeyTypes(myActionHandle);
					if (index < keyTypes.Length)
						keyTypes[index] = ___defaultInputTypeToKeyTypeMap[input];
				}
			}
		}

		// Choose by START button label
		[SuppressMessage("Method Declaration", "Harmony002:Patching properties by patching get_ or set_ is not recommended", Justification = "This is not a property.")]
		[HarmonyPatch(typeof(AdvEngine.LuaScript), "get_game_pad_type")]
		[HarmonyPostfix]
		static void LuaScript_get_game_pad_type_postfix(ref int __result)
		{
			switch ((GamePadDeviceType)__result)
			{
				case JoyConGamePadDeviceType:
					__result = (int)GamePadDeviceType.SWITCH;
					break;
				case MobileTouchGamePadDeviceType:
					__result = (int)GamePadDeviceType.XBOX;
					break;
				case SteamDeckGamePadDeviceType:
					__result = (int)GamePadDeviceType.XBOX;
					break;
			}
		}

		[HarmonyPatch(typeof(SurvivorDefine), "GetStartButtonText")]
		[HarmonyPrefix]
		static void GetStartButtonText_prefix(ref GamePadDeviceType gamePadDeviceType)
		{
			switch (gamePadDeviceType)
			{
				case JoyConGamePadDeviceType:
					gamePadDeviceType = GamePadDeviceType.SWITCH;
					break;
				case MobileTouchGamePadDeviceType:
					gamePadDeviceType = GamePadDeviceType.XBOX;
					break;
				case SteamDeckGamePadDeviceType:
					gamePadDeviceType = GamePadDeviceType.XBOX;
					break;
			}
		}

		internal delegate void GetGamepadTutorialText_delegate(ref string text);

		internal static void GetGamepadTutorialText_prefix(ref string text)
		{
			text = AdaptStickAndDpadEmojiToGamePad(text, GameInput2.GetGamePadType());
		}

		[HarmonyPatch(typeof(SurvivorDefine), "GetGamepadButtonKeyTypeFromEmojiStr")]
		[HarmonyPrefix]
		static void GetGamepadButtonKeyTypeFromEmojiStr_prefix(Dictionary<string, GameInput2.KeyType> ___emojiStrToGamepadButtonKeyTypeStrMap)
		{
			if (___emojiStrToGamepadButtonKeyTypeStrMap.ContainsKey("<sprite index=28>"))
			{
				// Throughout the game, Xbox & Switch stick sprites are used
				// both for stick movement and for L3/R3. However, L3/R3 are
				// unused in the default configuration, so tutorial text only
				// uses them to refer to stick movement. Nevertheless, the stock
				// TutorialUI/SurvivorDefine code remaps them via user settings
				// for L3/R3. For actual Xbox & Switch controllers, this is benign
				// because the settings UI doesn't let the user remap L3/R3
				// (even though the underlying code and save files support this).
				// But some of our custom controller types have different sprites
				// for stick movement and L3/R3 and yet use Xbox/Switch tutorial
				// texts (to get the right START button label). To ensure they get
				// the right stick movement sprites, remove this mistaken remapping.
				___emojiStrToGamepadButtonKeyTypeStrMap.Remove("<sprite index=28>");
				___emojiStrToGamepadButtonKeyTypeStrMap.Remove("<sprite index=29>");
				___emojiStrToGamepadButtonKeyTypeStrMap.Remove("<sprite index=40>");
				___emojiStrToGamepadButtonKeyTypeStrMap.Remove("<sprite index=41>");
			}
		}

		[HarmonyPatch(typeof(SurvivorDefine), "GetEmojiStrFromGamepadButtonKeyType")]
		[HarmonyPrefix]
		static void GetEmojiStrFromGamepadButtonKeyType_prefix(Dictionary<GameInput2.KeyType, string> ___gamepadButtonKeyTypeToEmojiStrMapSwitch)
		{
			if (___gamepadButtonKeyTypeToEmojiStrMapSwitch[GameInput2.KeyType.GamePad_B] == "<sprite index=20>")
			{
				___gamepadButtonKeyTypeToEmojiStrMapSwitch[GameInput2.KeyType.GamePad_A] = "<sprite index=20>";
				___gamepadButtonKeyTypeToEmojiStrMapSwitch[GameInput2.KeyType.GamePad_B] = "<sprite index=21>";
				___gamepadButtonKeyTypeToEmojiStrMapSwitch[GameInput2.KeyType.GamePad_Y] = "<sprite index=22>";
				___gamepadButtonKeyTypeToEmojiStrMapSwitch[GameInput2.KeyType.GamePad_X] = "<sprite index=23>";
			}
		}

		[HarmonyPatch(typeof(SurvivorDefine), "GetEmojiStrFromGamepadButtonKeyType")]
		[HarmonyPostfix]
		static void GetEmojiStrFromGamepadButtonKeyType_postfix(
			ref string __result,
			GamePadDeviceType gamePadDeviceType,
			GameInput2.KeyType gamepadKeyType)
		{
			switch (gamePadDeviceType)
			{
				case GamePadDeviceType.STEAM:
					__result = gamepadButtonKeyTypeToEmojiStrMapSteamController.GetValueSafe(gamepadKeyType) ?? "";
					break;
				case JoyConGamePadDeviceType:
					__result = gamepadButtonKeyTypeToEmojiStrMapJoyCon.GetValueSafe(gamepadKeyType) ?? "";
					break;
				case SteamDeckGamePadDeviceType:
					__result = gamepadButtonKeyTypeToEmojiStrMapSteamDeck.GetValueSafe(gamepadKeyType) ?? "";
					break;
			}
		}

		[HarmonyPatch(typeof(HelpMaster), MethodType.Constructor)]
		[HarmonyPostfix]
		static void HelpMaster_construct(Dictionary<GamePadDeviceType, string> ___PadDeviceTypePFNameDic)
		{
			___PadDeviceTypePFNameDic[JoyConGamePadDeviceType] = "Switch";
			___PadDeviceTypePFNameDic[MobileTouchGamePadDeviceType] = "Xbox";
			___PadDeviceTypePFNameDic[SteamDeckGamePadDeviceType] = "PS4";
		}

		[HarmonyPatch(typeof(DBHelpText), "GetString")]
		[HarmonyPostfix]
		static void DBHelpText_GetString_postfix(ref string __result)
		{
			var gamePadType = GameInput2.GetGamePadType();
			__result = AdaptDefaultRebindableEmojiToGamePad(AdaptStickAndDpadEmojiToGamePad(__result, gamePadType), gamePadType);
		}

		delegate GamePadDeviceType FixGamePadDeviceTypeForSprite(GamePadDeviceType gamePadType, ref string spriteName);

		[HarmonyPatch(typeof(HelpImageManager), "GetSprite")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> GetSprite_transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;
				if (instruction.Calls(AccessTools.Method(typeof(GameInput2), "GetGamePadType")))
				{
					yield return new CodeInstruction(OpCodes.Ldarga_S, 0);
					yield return Transpilers.EmitDelegate<FixGamePadDeviceTypeForSprite>((GamePadDeviceType gamePadType, ref string spriteName) =>
					{
						switch (gamePadType)
						{
							case JoyConGamePadDeviceType:
								return GamePadDeviceType.SWITCH;
							case MobileTouchGamePadDeviceType:
								return GamePadDeviceType.XBOX;
							case SteamDeckGamePadDeviceType:
								switch (spriteName)
								{
									case "map_04_ps":
									case "map_05_ps":
										spriteName = spriteName.Replace("_ps", "_sw");
										return GamePadDeviceType.SWITCH;
									default:
										return GamePadDeviceType.PS4;
								}
							default:
								return gamePadType;
						}
					});
				}
			}
		}

		internal static void GetTextSpineObjSkinSkinName_prefix(Dictionary<GameInput2.KeyType, string> ___steamPS4SkinNameMap)
		{
			if (___steamPS4SkinNameMap[GameInput2.KeyType.GamePad_A] == "ps_A")
			{
				___steamPS4SkinNameMap[GameInput2.KeyType.GamePad_A] = "ps_B";
				___steamPS4SkinNameMap[GameInput2.KeyType.GamePad_B] = "ps_A";
			}
		}

		internal static string GetTextSpineObjSkinSkinName_postfix(string __result, Dictionary<GameInput2.KeyType, string> ___steamSwitchSkinNameMap)
		{
			switch (GameInput2.GetGamePadType())
			{
				case JoyConGamePadDeviceType:
					var key = GameInput2.GetGamePadButtonKeyType(GameInput2.InputType.Decide);
					return ___steamSwitchSkinNameMap.GetValueSafe(key) ?? "";

					// the rest are like Xbox
			}
			return __result;
		}

		static readonly bool[] buttonPushed = new bool[(int)ActionHandleType.Max];

		[HarmonyPatch(typeof(SteamSettingGamepadConfigWindow), "OnChangeToKeyInputButtonAction")]
		[HarmonyPostfix]
		static void OnChangeToKeyInputButtonAction_postfix()
		{
			var gamePadDevice = (SteamGamePad)AccessTools.Field(typeof(GameInput2), "gamePadDevice").GetValue(GameInput2.GetInstance());
			var myActionHandleBases = (object[])AccessTools.Field(typeof(SteamGamePad), "myActionHandleBases").GetValue(gamePadDevice);
			var inputHandle = AccessTools.Field(typeof(SteamGamePad), "inputHandle_t").GetValue(gamePadDevice);
			var GetButton = AccessTools.Method(AccessTools.Inner(typeof(SteamGamePad), "MyActionHandleBase"), "GetButton").GetFastDelegate();
			for (ActionHandleType i = 0; i < ActionHandleType.Max; i++)
				if (IsConfiguableActionHandleTypeFlg(i))
					buttonPushed[(int)i] = (bool)GetButton(myActionHandleBases[(int)i], inputHandle, 0);
		}

		[HarmonyPatch(typeof(SteamSettingGamepadConfigWindow), "UpdateInKeyInputButtonAction")]
		[HarmonyPostfix]
		static void UpdateInKeyInputButtonAction_postfix(SteamSettingGamepadConfigWindow __instance, ref object __result)
		{
			var typeofGamepadConfigWindowState = AccessTools.Inner(typeof(SteamSettingGamepadConfigWindow), "GamepadConfigWindowState");
			//var myLogSource = BepInEx.Logging.Logger.CreateLogSource("SteamInput");
			//myLogSource.LogInfo($"New state: {__result}. GetAnyButtonDown: {GameInput2.GetAnyButtonDown()}. Decide pushed: {AccessTools.Method(typeof(GameInput2), "GetSteamGamePadPush").Invoke(GameInput2.GetInstance(), new object[] { GameInput2.InputType.Decide })}");
			if (__result.Equals(AccessTools.Field(typeofGamepadConfigWindowState, "KeyInputButtonAction").GetValue(null)))
			{
				var gamePadDevice = (SteamGamePad)AccessTools.Field(typeof(GameInput2), "gamePadDevice").GetValue(GameInput2.GetInstance());
				var myActionHandleBases = (object[])AccessTools.Field(typeof(SteamGamePad), "myActionHandleBases").GetValue(gamePadDevice);
				var inputHandle = AccessTools.Field(typeof(SteamGamePad), "inputHandle_t").GetValue(gamePadDevice);
				var GetButton = AccessTools.Method(AccessTools.Inner(typeof(SteamGamePad), "MyActionHandleBase"), "GetButton").GetFastDelegate();
				for (ActionHandleType i = 0; i < ActionHandleType.Max; i++)
				{
					if (IsConfiguableActionHandleTypeFlg(i))
					{
						var pushed = (bool)GetButton(myActionHandleBases[(int)i], inputHandle, 0);
						if (pushed != buttonPushed[(int)i])
						{
							buttonPushed[(int)i] = pushed;
							if (pushed)
							{
								AccessTools.Method(typeof(SteamSettingGamepadConfigWindow), "SwapActionHandleType").Invoke(__instance, new object[] { i });
								__result = AccessTools.Field(typeofGamepadConfigWindowState, "SelectItem").GetValue(null);
								//myLogSource.LogInfo($"Found pressed action handle {i}");
								break;
							}
						}
					}
				}
			}
			//BepInEx.Logging.Logger.Sources.Remove(myLogSource);
		}

		[HarmonyPatch(typeof(GameInput2.KeyInput), "keyDown")]
		[HarmonyPostfix]
		static void KeyInput_keyDown_postfix()
		{
			Cursor.visible = false;
		}

		[HarmonyPatch(typeof(GameInput2), "UpdateMouse")]
		[HarmonyPostfix]
		static void UpdateMouse_postfix()
		{
			if (GameInput2.isMouseMove() || GameInput2.isTouchStart() || GameInput2.isTouchNow() || GameInput2.isTouchTap() || GameInput2.isMouseDownL() || GameInput2.isMouseNowL() || GameInput2.isMouseClickL() || GameInput2.isMouseDownR() || GameInput2.isMouseNowR() || GameInput2.isMouseClickR())
				Cursor.visible = true;
		}
	}
}
