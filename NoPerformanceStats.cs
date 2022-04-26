namespace NoPerformanceStats
{
    
    using System.Collections;
    using System.Reflection;

    using MelonLoader;
    
    using UnityEngine.UI;

    using VRC.SDKBase.Validation.Performance;
    using VRC.SDKBase.Validation.Performance.Stats;

    public static class BuildInfo
    {

        public const string Name = "NoPerformanceStats";

        public const string Author = "ImTiara & Psychloor";

        public const string Company = null;

        public const string Version = "1.0.5";

        public const string DownloadLink = "https://github.com/Psychloor/NoPerformanceStats/";

    }

      public class NoPerformanceStats : MelonMod
    {
        private static HarmonyLib.Harmony _harmonyInstance;

        private static bool allowPerformanceScanner;

        private static Text avatarStatsButtonText;

        public override void OnApplicationStart()
        {
            MelonPreferences.CreateCategory(GetType().Name, "No Performance Stats");
            MelonPreferences.CreateEntry(GetType().Name, "DisablePerformanceStats", true, "Disable Performance Stats");

            _harmonyInstance = HarmonyInstance;
            ApplyPatches();

            MelonCoroutines.Start(UiManagerInitializer());
        }

        private void OnUiManagerInit()
        {
            // UserInterface/MenuContent/Screens/Avatar/AvatarDetails Button/Text
            avatarStatsButtonText = VRCUiManager.prop_VRCUiManager_0.transform.Find("MenuContent/Screens/Avatar/AvatarDetails Button/Text")?.GetComponent<Text>();

            OnPreferencesSaved();
        }

        public override void OnPreferencesSaved()
        {
            allowPerformanceScanner = !MelonPreferences.GetEntryValue<bool>(GetType().Name, "DisablePerformanceStats");

            RefreshPerfStuff();
        }

        private void ApplyPatches()
        {
            MethodInfo performanceScanMethod = typeof(AvatarPerformanceStats).GetMethod(
                nameof(AvatarPerformanceStats.CalculatePerformanceRating),
                BindingFlags.Public | BindingFlags.Instance);
            
            _harmonyInstance.Patch(
                performanceScanMethod,
                typeof(NoPerformanceStats).GetMethod(
                                              nameof(CalculatePerformance),
                                              BindingFlags.NonPublic | BindingFlags.Static)
                                          .ToNewHarmonyMethod());
            
            

            /*try
            {
                foreach (MethodInfo method in typeof(PerformanceScannerSet).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (method.Name.StartsWith(
                            "RunPerformanceScanEnumerator",
                       //     "Method_Public_IEnumerator_GameObject_AvatarPerformanceStats_MulticastDelegateNPublicSealedBoCoUnique",
                            StringComparison.Ordinal) && !method.IsAbstract && !method.IsVirtual)
                    {
                        _harmonyInstance.Patch(
                            method,
                            typeof(NoPerformanceStats).GetMethod(
                                                          nameof(CalculatePerformance),
                                                          BindingFlags.NonPublic | BindingFlags.Static)
                                                      .ToNewHarmonyMethod());
                    }
                }

            }
            catch (Exception e) { MelonLogger.Error("Failed to patch Performance Scanner: " + e); }*/
        }

        private static void RefreshPerfStuff()
        {
            if (avatarStatsButtonText != null)
                avatarStatsButtonText.text =
                    allowPerformanceScanner ? "Avatar Stats" : "<color=#ff6464>Stats Disabled!</color>";
        }

        private static bool CalculatePerformance(ref PerformanceRating __result)
        {
            if (allowPerformanceScanner) return true;

            __result = PerformanceRating.None;
            return false;
        }

        private IEnumerator UiManagerInitializer()
        {
            while (VRCUiManager.prop_VRCUiManager_0 == null) yield return null;
            OnUiManagerInit();
        }
    }

}