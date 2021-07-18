using System;
using System.Net;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using WebSockets;

namespace ProductionView
{
    [BepInPlugin("com.github.jahands.dsp_production", Strings.Name, Strings.Version)]
    public class ProductionPublisherMod : BaseUnityPlugin
    {
        private const int TickLimit = 10;
        private static ProductionPublisherMod _instance;
        private static int _count;
        private Harmony _harmony;
        private WebSocketServer _socket;

        public void Awake()
        {
            _instance = this;
            _harmony = new Harmony(typeof(ProductionPublisherMod).FullName);
            _socket = new WebSocketServer();
            _socket.Connected += SocketConnected;
        }

        public void Reset()
        {
            _instance = null;
            _harmony.UnpatchAll();
            _harmony = null;
            _socket.Dispose();
            _socket = null;
        }

        public void Start()
        {
            _harmony.PatchAll(typeof(ProductionPublisherMod));
            _socket.Bind(new IPEndPoint(IPAddress.Any, 1320));
            _socket.StartAccept();
            ProductionPublisherDebug.Log("Started!");
        }

        private void SocketConnected(object sender, WebSocket e)
        {
            // todo Handle incoming sockets
        }

        [HarmonyPatch(typeof(ProductionStatistics), "AfterTick")]
        [HarmonyPostfix]
        public static void StatisticsPatch(ProductionStatistics __instance)
        {
            if (++_count > TickLimit && __instance != null)
            {
                _count = 0;
                _instance.StatisticsUpdateTick(__instance);
            }
        }

        public void StatisticsUpdateTick(ProductionStatistics it)
        {
            // todo Publish ONLY required data to WebSocket
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