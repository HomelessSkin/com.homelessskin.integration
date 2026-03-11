using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Web;

using Core;

using TMPro;

using UI;

using Unity.Entities;

using UnityEngine;

using WebSocketSharp;

namespace Integration
{
    public class MonoAdapter : MonoBehaviour
    {
        [SerializeField] string _PlatformName;
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

            public Processor Processor => _Processor;
            [Space]
            [SerializeField] Processor _Processor;

            [Space]
            [SerializeField] Indicator Indicator;
            [SerializeField] TMP_InputField TokenInput;
            [SerializeField] TMP_InputField ChannelInput;

            protected EntityManager EntityManager;

            public Platform Platform => _Platform;
            protected Platform _Platform;

            protected ConcurrentQueue<SocketMessage> SocketMessages = new ConcurrentQueue<SocketMessage>();

            public void StartAuth() => _Processor.StartAuth();
            public void Load()
            {
                EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

                var platform = Load(_Processor.name);
                if (string.IsNullOrEmpty(platform))
                    return;

                _Platform = JsonUtility.FromJson<Platform>(platform);
            }
            public void Reset()
            {
                var token = ExtractToken(TokenInput.text);
                if (string.IsNullOrEmpty(TokenInput.text))
                {
                    Log.Warning(this, "Token Input is invalid!");

                    if (!string.IsNullOrEmpty(_Platform.Token))
                    {
                        token = _Platform.Token;

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

                    if (!string.IsNullOrEmpty(_Platform.Channel))
                    {
                        channel = _Platform.Channel;

                        Log.Warning(this, "Using saved Channel Name!");
                    }
                    else
                    {
                        Log.Error(this, "Please enter Channel Name!");

                        return;
                    }
                }

                if (_Platform == null)
                    _Platform = new Platform();

                _Platform.Name = _Processor.name;
                _Platform.Token = token;
                _Platform.Channel = channel;

                Save(_Platform);
                Connect();
            }
            public async void Connect()
            {
                Log.Info(this, $"Platform {_Processor.name} is trying to connect...");

                if (!VerifyPlatform())
                    return;

                _Platform.ChannelID = await _Processor.Connect(_Platform);
                if (!string.IsNullOrEmpty(_Platform.ChannelID))
                    InitializeSocket(_Processor.GetSocketURL());
            }
            public void Disconnect()
            {
                if (_Platform != null &&
                     _Platform.Socket != null &&
                     _Platform.Socket.IsAlive)
                    _Platform.Socket.Close();
            }
            public void Invoke()
            {
                while (SocketMessages.TryDequeue(out var message))
                {
                    Indicator?.Refresh();
                    _Processor.DetermineType(ref message, ref _Platform);

                    switch (message.type)
                    {
                        case "session_keepalive":
                        _Processor.OnPing(_Platform);
                        break;
                        case "session_welcome":
                        _Processor.SubscribeToEvents(_Platform);
                        break;
                        case "notification":
                        _Processor.Invoke(message, EntityManager);
                        break;
                    }
                }
            }

            public async Task<T> Get<T>(string uri) where T : class => await _Processor.Get<T>(uri, _Platform.Token);
            public async Task<bool> Post(string uri, object obj) => await _Processor.Post(uri, _Platform.Token, obj);
            public async Task<bool> Delete(string uri) => await _Processor.Delete(uri, _Platform.Token);
            public async Task<bool> Patch<T>(string uri, T obj) where T : class => await _Processor.Patch<T>(uri, _Platform.Token, obj);

            void InitializeSocket(string url)
            {
                Log.Info(this, $"{_Platform.Name} Socket Initialization.");

                if (_Platform.Socket != null)
                    _Platform.Socket.Close();

                _Platform.Socket = new WebSocket(url);
                _Platform.Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                _Platform.Socket.SslConfiguration.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                _Platform.Socket.OnOpen += OnOpen;
                _Platform.Socket.OnMessage += OnMessage;
                _Platform.Socket.OnClose += OnClose;
                _Platform.Socket.OnError += OnError;

                _Platform.Socket.Connect();
            }
            void OnOpen(object sender, EventArgs e) => _Processor.OnOpen(_Platform);
            void OnMessage(object sender, MessageEventArgs e)
            {
#if UNITY_EDITOR
                if (PrintRawSocket)
                    Debug.Log(e.Data);
#endif

                SocketMessages.Enqueue(_Processor.MessageFromJson(e.Data));
            }
            void OnClose(object sender, CloseEventArgs e) => Log.Warning(this, $"{e.Reason} {e.Code}");
            void OnError(object sender, ErrorEventArgs e) => Log.Error(this, $"{e.Message}");

            bool VerifyPlatform()
            {
                if (_Platform == null)
                {
                    Log.Warning(this, $"Platform {_Processor.name} doesn't exist!");

                    return false;
                }

                if (string.IsNullOrEmpty(_Platform.Token))
                {
                    Log.Warning(this, $"Platform {_Processor.name} doesn't have a User Token!");

                    return false;
                }

                if (string.IsNullOrEmpty(_Platform.Channel))
                {
                    Log.Warning(this, $"Platform {_Processor.name} doesn't have a Channel!");

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
        void OnDestroy()
        {
            _Adapter?.Disconnect();
        }

        public void StartAuth() => _Adapter.StartAuth();
        public void Submit() => _Adapter.Reset();
        public void Connect() => _Adapter.Connect();

        public Platform GetPlatform() => _Adapter.Platform;

        public async Task<T> Get<T>(string uri) where T : class => await _Adapter.Get<T>(uri);
        public async Task<bool> Post(string uri, object obj) => await _Adapter.Post(uri, obj);
        public async Task<bool> Delete(string uri) => await _Adapter.Delete(uri);
        public async Task<bool> Patch<T>(string uri, T obj) where T : class => await _Adapter.Patch<T>(uri, obj);
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