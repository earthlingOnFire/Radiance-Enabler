using BepInEx;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using Configgy;

namespace RadianceEnabler;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInDependency("Hydraxous.ULTRAKILL.Configgy", BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BaseUnityPlugin {	
  public const string PLUGIN_GUID = "com.earthlingOnFire.RadianceEnabler";
  public const string PLUGIN_NAME = "Radiance Enabler";
  public const string PLUGIN_VERSION = "1.0.1";

  private ConfigBuilder config;

  private void Awake() {
    config = new ConfigBuilder(PLUGIN_GUID, PLUGIN_NAME);
    config.BuildAll();
  }

  private void Start() {
    new Harmony(PLUGIN_GUID).PatchAll();
  }
}

public static class RadianceConfig {
  [Configgable("", "Force Radiance")]
  public static ConfigToggle radianceToggle = new ConfigToggle(false);

  [Configgable("", "Visuals Only")]
  public static ConfigToggle visualsOnly = new ConfigToggle(false);
  
  [Configgable("", "Radiance Tier")]
  public static float radianceTier = 1f;
}

[HarmonyPatch]
public static class Patches {
  private static bool currentVisualsOnly;

  [HarmonyPostfix]
  [HarmonyPatch(typeof(OptionsManager), "Update")]
  private static void OptionsManager_Update_Postfix() {
    bool radiance = RadianceConfig.radianceToggle.Value;
    float tier = RadianceConfig.radianceTier;
    bool visualsOnly = RadianceConfig.visualsOnly.Value;

    if (radiance == OptionsManager.forceRadiance &&
        tier == OptionsManager.radianceTier &&
        visualsOnly == currentVisualsOnly) return;

    SetRadiance(radiance, tier, visualsOnly);
  }

  private static void SetRadiance(bool radiance, float tier, bool visualsOnly) {
    if (radiance) {
      MonoSingleton<AssistController>.Instance.cheatsEnabled = true;
    }

    OptionsManager.forceRadiance = radiance;
    if (radiance) {
      OptionsManager.radianceTier = tier;
    }
    currentVisualsOnly = visualsOnly;

    foreach(EnemyIdentifier enemy in Object.FindObjectsOfType<EnemyIdentifier>()) {
      enemy.UpdateBuffs();
    }
  }

  [HarmonyPrefix]
  [HarmonyPatch(typeof(EnemyIdentifier), "UpdateModifiers")]
  private static bool EnemyIdentifier_UpdateModifiers_Prefix(EnemyIdentifier __instance) {
    EnemyIdentifier enemy = __instance;

		enemy.totalSpeedModifier = 1f;
		enemy.totalHealthModifier = 1f;
		enemy.totalDamageModifier = 1f;

    if (!currentVisualsOnly || !OptionsManager.forceRadiance) {
      float num;
      if (OptionsManager.forceRadiance) {
        num = Mathf.Max(OptionsManager.radianceTier, enemy.radianceTier);
      }
      else {
        num = enemy.radianceTier;
      }

      if (enemy.speedBuff || OptionsManager.forceRadiance) {
        enemy.totalSpeedModifier *= enemy.speedBuffModifier * ((num > 1f) ? (0.75f + num / 4f) : num);
      }
      if (enemy.healthBuff || OptionsManager.forceRadiance) {
        enemy.totalHealthModifier *= enemy.healthBuffModifier * ((num > 1f) ? (0.75f + num / 4f) : num);
      }
      if (enemy.damageBuff || OptionsManager.forceRadiance) {
        enemy.totalDamageModifier *= enemy.damageBuffModifier;
      }
    }

    if (enemy.puppet) {
      enemy.totalHealthModifier /= 2f;
      enemy.totalSpeedModifier *= Mathf.Lerp(
          0f, Mathf.Max(0f, enemy.puppetSpawnTimer - 0.75f) * 3f, enemy.puppetSpawnTimer
      );
    }

    return false;
  }
}

