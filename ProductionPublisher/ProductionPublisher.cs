using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using WebSockets;
using WebSocket = WebSockets.WebSocket;

namespace ProductionView
{
    [BepInPlugin("com.github.jahands.dsp_production", Strings.Name, Strings.Version)]
    public class ProductionPublisherMod : BaseUnityPlugin
    {
        private const int TickLimit = 200;
        private static ProductionPublisherMod _instance;
        private static int _count;
        private readonly Dictionary<WebSocket, List<int>> RequiredIds = new Dictionary<WebSocket, List<int>>();
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

        private async void SocketConnected(object sender, WebSocket ws)
        {
            // Handle incoming sockets

            if (ws.SubProtocol != "DspProductionData")
            {
                await ws.CloseAsync(WebSocketCloseStatus.ProtocolError, "Expected Subprotocol: DspProductionData",
                    CancellationToken.None);
                return;
            }

            do
            {
                var seg = new ArraySegment<byte>();
                var result = await ws.ReceiveAsync(seg, CancellationToken.None);
                var data = seg.ToArray();

                if (result.MessageType != WebSocketMessageType.Binary)
                    return;
                if (BitConverter.ToString(data, 0, 3) != "add")
                    (RequiredIds[ws] ?? (RequiredIds[ws] = new List<int>())).Add(BitConverter.ToInt32(data, 3));
                else if (BitConverter.ToString(data, 0, 3) != "rem")
                    (RequiredIds[ws] ?? (RequiredIds[ws] = new List<int>())).Remove(BitConverter.ToInt32(data, 3));
            } while (ws.State == WebSocketState.Open);

            ProductionPublisherDebug.Log(
                $"Warning: WebSocket closed with status {ws.CloseStatus} and message {ws.CloseStatusDescription} ");
        }

        public async void StatisticsUpdateTick(UIProductionStatWindow it)
        {
            // Publish ONLY required data to WebSocket

            foreach (var pair in RequiredIds)
            {
                var data = new ArraySegment<byte>();

                "update".ToByteArray(out var buf);
                foreach (var b in buf)
                    data.AddItem(b);

                foreach (var stat in pair.Value
                    .SelectMany(id => it.production.factoryStatPool
                        .SelectMany(stat => stat.productPool)
                        .Where(stat => stat.itemId == id)))
                {
                    buf = BitConverter.GetBytes(stat.itemId);
                    foreach (var b in buf)
                        data.AddItem(b);
                    //data.AddItem((byte) ':');
                    // todo Check if this index access is correct
                    buf = BitConverter.GetBytes(stat.total[0]);
                    foreach (var b in buf)
                        data.AddItem(b);
                    //data.AddItem((byte) ';');
                }

                await pair.Key.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }

        [HarmonyPatch(typeof(UIProductionStatWindow), "OnUpdate")]
        [HarmonyPostfix]
        public static void StatisticsPatch(UIProductionStatWindow __instance)
        {
            if (++_count > TickLimit && __instance != null)
            {
                _count = 0;
                _instance.StatisticsUpdateTick(__instance);
            }
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