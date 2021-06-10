﻿namespace NoPerformanceStats
{

    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using MelonLoader;

    using UnityEngine;

    using VRCSDK2.Validation.Performance;

    public static class BuildInfo
    {

        public const string Name = "NoPerformanceStats";

        public const string Author = "ImTiara & Psychloor";

        public const string Company = null;

        public const string Version = "1.1.1";

        public const string DownloadLink = "https://github.com/Psychloor/NoPerformanceStats/";

    }

    public class NoPerformanceStats : MelonMod
    {

        private static MelonPreferences_Entry<bool> disablePerformanceStatsEntry;

        private static CalculatePerformanceStats calculatePerformanceStats;

        private static CalculatePerformanceStatsEnumerator calculatePerformanceStatsEnumerator;

        private static ApplyPerformanceFiltersEnumerator applyPerformanceFiltersEnumerator;

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
            disablePerformanceStatsEntry =
                settingsCategory.CreateEntry("DisablePerformanceStats", true, "Disable Performance Stats");

            MethodInfo calculatePerformanceStatsMethod = typeof(AvatarPerformance).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Single(
                m => m.Name.StartsWith("Method_Public_Static_Void_String_GameObject_AvatarPerformanceStats_")
                     && m.ReturnType == typeof(void)
                     && m.GetParameters().Length == 3);
            calculatePerformanceStats = Patch<CalculatePerformanceStats>(calculatePerformanceStatsMethod, GetDetour(nameof(CalculatePerformanceStatsPatch)));

            MethodInfo calculatePerformanceStatsEnumeratorMethod = typeof(AvatarPerformance).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Single(
                m => m.Name.StartsWith("Method_Public_Static_IEnumerator_String_GameObject_AvatarPerformanceStats") && m.GetParameters().Length == 3);
            calculatePerformanceStatsEnumerator = Patch<CalculatePerformanceStatsEnumerator>(
                calculatePerformanceStatsEnumeratorMethod,
                GetDetour(nameof(CalculatePerformanceStatsEnumeratorPatch)));

            MethodInfo applyPerformanceFiltersMethod = typeof(AvatarPerformance).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).Single(
                m => m.Name.StartsWith("Method_Public_Static_IEnumerator_GameObject_AvatarPerformanceStats_PerformanceRating_MulticastDelegate")
                     && m.GetParameters().Length == 4);
            applyPerformanceFiltersEnumerator = Patch<ApplyPerformanceFiltersEnumerator>(
                applyPerformanceFiltersMethod,
                GetDetour(nameof(ApplyPerformanceFiltersPatch)));
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.P)
                && Input.GetKey(KeyCode.LeftShift)
                && Input.GetKey(KeyCode.LeftControl))
            {
                disablePerformanceStatsEntry.Value = !disablePerformanceStatsEntry.Value;
                disablePerformanceStatsEntry.Save();

                MelonLogger.Msg("Avatar Performance Stats is now " + (!disablePerformanceStatsEntry.Value ? "ENABLED" : "DISABLED"));
            }
        }

        private static IntPtr ApplyPerformanceFiltersPatch(
            IntPtr avatarObjectPtr,
            IntPtr performanceStatsPtr,
            int minimumPerformanceRating,
            IntPtr onBlockPtr,
            IntPtr stackPtr)
        {
            return disablePerformanceStatsEntry.Value
                       ? IntPtr.Zero
                       : applyPerformanceFiltersEnumerator(avatarObjectPtr, performanceStatsPtr, minimumPerformanceRating, onBlockPtr, stackPtr);
        }

        private static IntPtr CalculatePerformanceStatsEnumeratorPatch(
            IntPtr avatarNamePtr,
            IntPtr avatarObjectPtr,
            IntPtr performanceStatsPtr,
            IntPtr stackPtr)
        {
            return disablePerformanceStatsEntry.Value
                       ? IntPtr.Zero
                       : calculatePerformanceStatsEnumerator(avatarNamePtr, avatarObjectPtr, performanceStatsPtr, stackPtr);
        }

        private static void CalculatePerformanceStatsPatch(IntPtr avatarNamePtr, IntPtr avatarObjectPtr, IntPtr performanceStatsPtr, IntPtr stackPtr)
        {
            if (disablePerformanceStatsEntry.Value) return;
            calculatePerformanceStats(avatarNamePtr, avatarObjectPtr, performanceStatsPtr, stackPtr);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CalculatePerformanceStats(IntPtr avatarNamePtr, IntPtr avatarObjectPtr, IntPtr performanceStatsPtr, IntPtr stackPtr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr CalculatePerformanceStatsEnumerator(IntPtr avatarNamePtr, IntPtr avatarObjectPtr, IntPtr performanceStatsPtr, IntPtr stackPtr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ApplyPerformanceFiltersEnumerator(
            IntPtr avatarObjectPtr,
            IntPtr performanceStatsPtr,
            int minimumPerformanceRating,
            IntPtr onBlockPtr,
            IntPtr stackPtr);

    }

}