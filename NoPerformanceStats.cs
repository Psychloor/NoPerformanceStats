namespace NoPerformanceStats
{

    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using MelonLoader;

    using UnityEngine;

    using VRC.SDKBase.Validation.Performance;
    using VRC.SDKBase.Validation.Performance.Stats;

    using VRCSDK2.Validation.Performance;

    public static class BuildInfo
    {

        public const string Name = "NoPerformanceStats";

        public const string Author = "ImTiara & Psychloor";

        public const string Company = null;

        public const string Version = "1.1.2";

        public const string DownloadLink = "https://github.com/Psychloor/NoPerformanceStats/";

    }

    public sealed class NoPerformanceStats : MelonMod
    {

        private static MelonPreferences_Entry<bool> disablePerformanceStatsEntry;

        private static CalculatePerformanceDelegate calculatePerformanceDelegate;

        private MelonPreferences_Category settingsCategory;

        private static unsafe TDelegate Patch<TDelegate>(MethodBase originalMethod, IntPtr patchDetour)
        {
            IntPtr original = *(IntPtr*)UnhollowerSupport.MethodBaseToIl2CppMethodInfoPointer(originalMethod);
            MelonUtils.NativeHookAttach((IntPtr)(&original), patchDetour);
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(original);
        }

        private static IntPtr GetDetour(string name)
        {
            return typeof(NoPerformanceStats).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.MethodHandle.GetFunctionPointer();
        }

        public override void OnApplicationStart()
        {
            settingsCategory = MelonPreferences.CreateCategory("NoPerformanceStats", "No Performance Stats");
            disablePerformanceStatsEntry = settingsCategory.CreateEntry("DisablePerformanceStats", true, "Disable Performance Stats");

            try
            {
                MethodInfo calculateEnumerator = typeof(PerformanceScannerSet).GetMethod(
                    nameof(PerformanceScannerSet.Method_Public_IEnumerator_GameObject_AvatarPerformanceStats_MulticastDelegateNPublicSealedBoCoUnique_0),
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                    null,
                    new[] { typeof(GameObject), typeof(AvatarPerformanceStats), typeof(AvatarPerformance.MulticastDelegateNPublicSealedBoCoUnique) },
                    null);

                calculatePerformanceDelegate = Patch<CalculatePerformanceDelegate>(calculateEnumerator, GetDetour(nameof(CalculatePerformancePatch)));
            }
            catch (Exception e)
            {
                MelonLogger.Error("Failed to patch CalculatePerformanceStats\n" + e);
            }
        }

        private static IntPtr CalculatePerformancePatch(IntPtr instancePtr, IntPtr gameObjectPtr, IntPtr statsPtr, IntPtr delegatePtr, IntPtr stackPtr)
        {
            return disablePerformanceStatsEntry.Value ? IntPtr.Zero : calculatePerformanceDelegate(instancePtr, gameObjectPtr, statsPtr, delegatePtr, stackPtr);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr CalculatePerformanceDelegate(IntPtr instancePtr, IntPtr gameObjectPtr, IntPtr statsPtr, IntPtr delegatePtr, IntPtr stackPtr);

    }

}