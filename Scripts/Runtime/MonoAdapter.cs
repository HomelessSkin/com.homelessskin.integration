using System;
using System.Collections.Concurrent;
using System.Web;

using Core;

using Input;

using TMPro;

using UI;

using Unity.Entities;

using UnityEngine;

using WebSocketSharp;

namespace Integration
{
    public class MonoAdapter : MonoBehaviour
    {
        [SerializeField] string Platform;
        [SerializeField] Adapter _Adapter;
        #region ADAPTER
        [Serializable]
        class Adapter : Storage
        {
            protected static string ExtractToken(string text)
            {
                if (string.IsNullOrEmpty(text))
                    return "";

                return HttpUtility.ParseQueryString(new Uri(text).Fragment.TrimStart('#'))["access_token"];
            }

#if UNITY_EDITOR
            [Space]
            [SerializeField] bool PrintRawSocket = false;
#endif

            [Space]
            [SerializeField] Processor Processor;

            [Space]
            [SerializeField] Indicator Indicator;
            [SerializeField] TMP_InputField TokenInput;
            [SerializeField] TMP_InputField ChannelInput;

            protected EntityManager EntityManager;

            public Platform _Platform => Platform;
            protected Platform Platform;

            protected ConcurrentQueue<SocketMessage> SocketMessages = new ConcurrentQueue<SocketMessage>();

            public void StartAuth() => Processor.StartAuth();
            public void Load()
            {
                EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

                var platform = Load(Processor.name);
                if (string.IsNullOrEmpty(platform))
                    return;

                Platform = JsonUtility.FromJson<Platform>(platform);
            }
            public void Reset()
            {
                var token = ExtractToken(TokenInput.text);
                if (string.IsNullOrEmpty(TokenInput.text))
                {
                    Log.Warning(this, "Token Input is invalid!");

                    if (!string.IsNullOrEmpty(Platform.Token))
                    {
                        token = Platform.Token;

                        Log.Warning(this, "Using saved Token!");
                    }
                    else
                    {
                        Log.Error(this, "Please enter valid Token or Redirect URL!");

                        return;
                    }
                }

                var channel = ChannelInput.text;
                if (string.IsNullOrEmpty(channel))
                {
                    Log.Warning(this, "Channel Input is empty!");

                    if (!string.IsNullOrEmpty(Platform.Channel))
                    {
                        channel = Platform.Channel;

                        Log.Warning(this, "Using saved Channel Name!");
                    }
                    else
                    {
                        Log.Error(this, "Please enter Channel Name!");

                        return;
                    }
                }

                if (Platform == null)
                    Platform = new Platform();

                Platform.Name = Processor.name;
                Platform.Token = token;
                Platform.Channel = channel;

                Save(Platform);
                Connect();
            }
            public async void Connect()
            {
                Log.Info(this, $"Platform {Processor.name} is trying to connect...");

                if (!VerifyPlatform())
                    return;

                Platform.ChannelID = await Processor.Connect(Platform);
                if (!string.IsNullOrEmpty(Platform.ChannelID))
                    InitializeSocket(Processor.GetSocketURL());
            }
            public void Disconnect()
            {
                if (Platform != null &&
                     Platform.Socket != null &&
                     Platform.Socket.IsAlive)
                    Platform.Socket.Close();
            }
            public void Invoke()
            {
                while (SocketMessages.TryDequeue(out var message))
                {
                    Indicator?.Refresh();
                    Processor.DetermineType(ref message, ref Platform);

                    switch (message.type)
                    {
                        case "session_keepalive":
                        Processor.OnPing(Platform);
                        break;
                        case "session_welcome":
                        Processor.SubscribeToEvents(Platform);
                        break;
                        case "notification":
                        Processor.Invoke(message, EntityManager);
                        break;
                    }
                }
            }
            public void RequestDeleteMessage(OuterInput input) => Processor.RequestDeleteMessage(input, Platform);
            public void RequestTimeout(OuterInput input) => Processor.RequestTimeout(input, Platform);
            public void RequestBan(OuterInput input) => Processor.RequestBan(input, Platform);

            void InitializeSocket(string url)
            {
                Log.Info(this, $"{Platform.Name} Socket Initialization.");

                if (Platform.Socket != null)
                    Platform.Socket.Close();

                Platform.Socket = new WebSocket(url);
                Platform.Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                Platform.Socket.SslConfiguration.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                Platform.Socket.OnOpen += OnOpen;
                Platform.Socket.OnMessage += OnMessage;
                Platform.Socket.OnClose += OnClose;
                Platform.Socket.OnError += OnError;

                Platform.Socket.Connect();
            }
            void OnOpen(object sender, EventArgs e) => Processor.OnOpen(Platform);
            void OnMessage(object sender, MessageEventArgs e)
            {
#if UNITY_EDITOR
                if (PrintRawSocket)
                    Debug.Log(e.Data);
#endif

                SocketMessages.Enqueue(Processor.FromJson(e.Data));
            }
            void OnClose(object sender, CloseEventArgs e) => Log.Warning(this, $"{e.Reason} {e.Code}");
            void OnError(object sender, ErrorEventArgs e) => Log.Error(this, $"{e.Message}");

            bool VerifyPlatform()
            {
                if (Platform == null)
                {
                    Log.Warning(this, $"Platform {Processor.name} doesn't exist!");

                    return false;
                }

                if (string.IsNullOrEmpty(Platform.Token))
                {
                    Log.Warning(this, $"Platform {Processor.name} doesn't have a User Token!");

                    return false;
                }

                if (string.IsNullOrEmpty(Platform.Channel))
                {
                    Log.Warning(this, $"Platform {Processor.name} doesn't have a Channel!");

                    return false;
                }

                return true;
            }
        }
        #endregion

        void Start()
        {
            _Adapter.Load();
            _Adapter.Connect();
        }
        void Update()
        {
            _Adapter.Invoke();
        }

        public void StartAuth() => _Adapter.StartAuth();
        public void Submit(int type) => _Adapter.Reset();
        public void Connect(int type) => _Adapter.Connect();
        public void DeleteMessageButton(OuterInput input)
        {
            if (input.Platform == Platform)
                _Adapter.RequestDeleteMessage(input);
        }
        public void TimeoutButton(OuterInput input)
        {
            if (input.Platform == Platform)
                _Adapter.RequestTimeout(input);
        }
        public void BanButton(OuterInput input)
        {
            if (input.Platform == Platform)
                _Adapter.RequestBan(input);
        }

        public Platform GetPlatformData() => _Adapter._Platform;
    }

    #region PLATFORM
    [Serializable]
    public class Platform : IStorage.Data
    {
        [NonSerialized] public string ChannelID;
        [NonSerialized] public string SessionID;
        [NonSerialized] public WebSocket Socket;

        public string Token;
        public string Channel;
    }
    #endregion
}