using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MoreToggleableItems;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[Harmony]
public class Plugin : BaseUnityPlugin {
	public const string TOGGLE_TOOLTIP = "Toggle : [RMB]";

	internal static new ManualLogSource Logger;

	private void Awake() {
		Logger = base.Logger;
		Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
		harmony.PatchAll();
		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
	}

	[HarmonyPatch(typeof(AnimatedItem), nameof(AnimatedItem.Start))]
	[HarmonyPrefix]
	private static void SyncAnimatedItemUse(AnimatedItem __instance) {
		if(__instance.itemProperties.name == "RobotToy" || __instance.itemProperties.name == "Dentures") {
			__instance.itemProperties.syncUseFunction = true;
			__instance.itemProperties.syncInteractLRFunction = true;
			if(!__instance.itemProperties.toolTips.Contains(TOGGLE_TOOLTIP)) {
				List<string> tooltips = __instance.itemProperties.toolTips.ToList();
				tooltips.Add(TOGGLE_TOOLTIP);
				__instance.itemProperties.toolTips = tooltips.ToArray();
			}
		}
	}

	[HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.ItemActivate))]
	[HarmonyPrefix]
	private static void OnActivateItem(GrabbableObject __instance, bool used, bool buttonDown = true) {
		if(__instance.itemProperties.name == "RobotToy" || __instance.itemProperties.name == "Dentures") {
			AnimatedItem item = __instance as AnimatedItem;
			if(!item)
				return;
			if(item.chanceToTriggerAlternateMesh > 0) {
				item.gameObject.GetComponent<MeshFilter>().mesh = item.normalMesh;
			}
			if(item.itemAudio != null) {
				if(item.itemAnimator != null) {
					item.itemAnimator.SetBool(item.grabItemBoolString, !item.itemAudio.isPlaying);
				}
				if(item.itemAudio.isPlaying) {
					item.itemAudio.Stop();
				} else {
					item.itemAudio.Play();
					item.itemAudio.clip = item.grabAudio;
					item.itemAudio.loop = item.loopGrabAudio;
				}
			}
		} else if(__instance.itemProperties.name == "Clock" && __instance.TryGetComponent(out ClockNoiseBlocker blocker)) {
			ClockProp clock = __instance as ClockProp;
			if(!clock)
				return;
			if(blocker.blockClockTicking) {
				clock.timeOfLastSecond = Time.realtimeSinceStartup - blocker.timeUntilNextSecond;
				blocker.blockClockTicking = false;
			} else {
				blocker.timeUntilNextSecond = Time.realtimeSinceStartup - clock.timeOfLastSecond;
				blocker.blockClockTicking = true;
			}
		}
	}

	[HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
	[HarmonyPrefix]
	private static void SyncClockPropUse(GrabbableObject __instance) {
		if(__instance.itemProperties.name == "Clock" && (__instance as ClockProp)) {
			__instance.itemProperties.syncUseFunction = true;
			__instance.itemProperties.syncInteractLRFunction = true;
			__instance.itemProperties.saveItemVariable = true;
			if(!__instance.itemProperties.toolTips.Contains(TOGGLE_TOOLTIP)) {
				List<string> tooltips = __instance.itemProperties.toolTips.ToList();
				tooltips.Add(TOGGLE_TOOLTIP);
				__instance.itemProperties.toolTips = tooltips.ToArray();
			}
			if(!__instance.GetComponent<ClockNoiseBlocker>()) {
				ClockNoiseBlocker blocker = __instance.gameObject.AddComponent<ClockNoiseBlocker>();
				blocker.Initialize(__instance, 5);
			}
		}
	}

	[HarmonyPatch(typeof(ClockProp), nameof(ClockProp.Update))]
	[HarmonyPrefix]
	private static void TurnOffClock(ClockProp __instance) {
		if(__instance.itemProperties.name == "Clock" && __instance.TryGetComponent(out ClockNoiseBlocker blocker) && blocker.blockClockTicking) {
			__instance.timeOfLastSecond = Time.realtimeSinceStartup;
		}
	}

	[HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GetItemDataToSave))]
	[HarmonyPostfix]
	private static void ClockSoundSaveData(GrabbableObject __instance, ref int __result) {
		if(__instance.itemProperties.name == "Clock" && (__instance as ClockProp) && __instance.TryGetComponent(out ClockNoiseBlocker blocker)) {
			__result = blocker.blockClockTicking ? (int)(blocker.timeUntilNextSecond * 100f) : -1;
		}
	}

	[HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LoadItemSaveData))]
	[HarmonyPostfix]
	private static void ClockSoundLoadData(GrabbableObject __instance, int saveData) {
		if(__instance.itemProperties.name == "Clock" && (__instance as ClockProp)) {
			ClockNoiseBlocker blocker = __instance.GetComponent<ClockNoiseBlocker>();
			if(blocker == null) {
				blocker = __instance.gameObject.AddComponent<ClockNoiseBlocker>();
				blocker.Initialize(__instance, 5);
			}
			blocker.blockClockTicking = (saveData != -1);
			blocker.timeUntilNextSecond = saveData / 100f;
		}
	}
}
