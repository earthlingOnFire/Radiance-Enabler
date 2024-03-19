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
  public const string PLUGIN_VERSION = "1.0.0";

  private ConfigBuilder config;

  private void Awake() {
    config = new ConfigBuilder(PLUGIN_GUID, PLUGIN_NAME);
    config.BuildAll();
  }

  private void Start() {
    new Harmony(PLUGIN_GUID).PatchAll();
  }
}

public class RadianceConfig : MonoBehaviour {
  [Configgable("", "Radiance Tier")]
  public static float radianceTier = 1f;

  [Configgable("", "Toggle Radiance")]
  public static ConfigToggle radianceToggle = new ConfigToggle(false);
}

[HarmonyPatch]
public static class Patches {
  [HarmonyPostfix]
  [HarmonyPatch(typeof(OptionsManager), "Update")]
  private static void OptionsManager_Update_Postfix() {
    SetRadiance(RadianceConfig.radianceToggle.Value, RadianceConfig.radianceTier);
  }

  private static void SetRadiance(bool radiance, float tier) {
    if (!SceneHelper.CurrentScene.Contains("Level") ||
       (OptionsManager.forceRadiance == radiance && OptionsManager.radianceTier == tier)) {
      return;
    }

    if (radiance) {
      MonoSingleton<AssistController>.Instance.cheatsEnabled = true;
    }

    OptionsManager.forceRadiance = radiance;
    OptionsManager.radianceTier = tier;

    foreach(EnemyIdentifier enemy in Object.FindObjectsOfType<EnemyIdentifier>()) {
      enemy.UpdateBuffs();
    }
  }
}

