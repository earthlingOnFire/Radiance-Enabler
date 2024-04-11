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
  public const string PLUGIN_VERSION = "1.0.3";

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
  [Configgable("", "Enable Radiance")]
  public static ConfigToggle radianceToggle = new ConfigToggle(false);

  [Configgable("", "Visuals Only")]
  public static ConfigToggle visualsOnly = new ConfigToggle(false);
  
  [Configgable("", "Radiance Tier")]
  public static float radianceTier = 1f;

  [Configgable("", "Ignore Radiance Tier")]
  public static ConfigToggle ignoreTier = new ConfigToggle(false);

  [Configgable("", "Health Modifier")]
  public static float healthModifier = 1f;

  [Configgable("", "Speed Modifier")]
  public static float speedModifier = 1f;

  [Configgable("", "Damage Modifier")]
  public static float damageModifier = 1f;
}

[HarmonyPatch]
public static class Patches {
  private static bool currentVisualsOnly;
  private static bool currentIgnoreTier;
  private static float currentSpeedModifier;
  private static float currentHealthModifier;
  private static float currentDamageModifier;

  [HarmonyPostfix]
  [HarmonyPatch(typeof(OptionsManager), "Update")]
  private static void OptionsManager_Update_Postfix() {
    bool radiance = RadianceConfig.radianceToggle.Value;
    float tier = RadianceConfig.radianceTier;
    bool visualsOnly = RadianceConfig.visualsOnly.Value;
    bool ignoreTier = RadianceConfig.ignoreTier.Value;
    float speedModifier = RadianceConfig.speedModifier;
    float healthModifier = RadianceConfig.healthModifier;
    float damageModifier = RadianceConfig.damageModifier;

    if (radiance == OptionsManager.forceRadiance &&
        tier == OptionsManager.radianceTier &&
        visualsOnly == currentVisualsOnly &&
        ignoreTier == currentIgnoreTier &&
        speedModifier == currentSpeedModifier &&
        healthModifier == currentHealthModifier &&
        damageModifier == currentDamageModifier) return;

    SetRadiance(radiance, tier, visualsOnly, ignoreTier, speedModifier, healthModifier, damageModifier);
  }

  private static void SetRadiance(bool radiance, float tier, bool visualsOnly, bool ignoreTier,
                                  float speedModifier, float healthModifier, float damageModifier) {
    if (radiance) {
      MonoSingleton<AssistController>.Instance.cheatsEnabled = true;
    }

    OptionsManager.forceRadiance = radiance;
    if (radiance) {
      OptionsManager.radianceTier = tier;
    }
    currentVisualsOnly = visualsOnly;
    currentIgnoreTier = ignoreTier;
    currentSpeedModifier = speedModifier;
    currentHealthModifier = healthModifier;
    currentDamageModifier = damageModifier;

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
      } else {
        num = Mathf.Max(1f, enemy.radianceTier);
      }

      if (enemy.speedBuff || (OptionsManager.forceRadiance && !currentIgnoreTier)) {
        enemy.totalSpeedModifier *= enemy.speedBuffModifier * ((num > 1f) ? (0.75f + num / 4f) : num);
      }
      if (enemy.healthBuff || (OptionsManager.forceRadiance && !currentIgnoreTier)) {
        enemy.totalHealthModifier *= enemy.healthBuffModifier * ((num > 1f) ? (0.75f + num / 4f) : num);
      }
      if (enemy.damageBuff || (OptionsManager.forceRadiance && !currentIgnoreTier)) {
        enemy.totalDamageModifier *= enemy.damageBuffModifier;
      }

      if (OptionsManager.forceRadiance) {
        enemy.totalSpeedModifier *= currentSpeedModifier;
        enemy.totalHealthModifier *= currentHealthModifier;
        enemy.totalDamageModifier *= currentDamageModifier;
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

