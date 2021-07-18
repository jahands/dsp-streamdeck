using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ProductionView
{
    [BepInPlugin("com.github.jahands.dsp_production", Strings.Name, Strings.Version)]
    public class ProductionPublisherMod : BaseUnityPlugin
    {
        private Harmony _harmony;

        public void Awake()
        {
            _harmony = new Harmony(typeof(ProductionPublisherMod).FullName);
        }

        public void Start()
        {
            _harmony.PatchAll(typeof(ProductionPublisherMod));
            ProductionPublisherDebug.Log("Started!");
        }

        public void Update()
        {
        }

        public void Reset()
        {
            _harmony.UnpatchAll();
        }
    }

    public static class ProductionPublisherDebug
    {
        private static readonly bool debug = true;
        internal static bool verbose = false;
        public static ILogger log => Debug.unityLogger;

        internal static void Log(string tagname, object message, Exception exception = null)
        {
            log?.Log($"[{Strings.LogPrefix} - {tagname}]", message + (exception == null
                ? string.Empty
                : "\nSource: " + exception.Source + "\nException:\n" + exception));
        }

        public static void Log(object message, Exception exception = null)
        {
            Log("Info", message, exception);
        }

        public static void LogDebug(object message, Exception exception = null)
        {
            if (!debug)
                return;
            Log("Debug", message, exception);
        }

        public static void LogVerbose(object message, Exception exception = null)
        {
            if (!verbose)
                return;
            Log("Verbose", message, exception);
        }
    }

    public static class Strings
    {
        public const string Name = "ProductionPublisher";
        public const string Version = "0.0.1";
        public const string LogPrefix = "[" + Name + "]";
    }
}