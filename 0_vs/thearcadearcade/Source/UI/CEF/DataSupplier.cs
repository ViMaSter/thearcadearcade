using System;
using WebSocketSharp;
using WebSocketSharp.Server;

using Newtonsoft.Json;

namespace thearcadearcade.UI.CEF
{
    partial class ConstantUpdateByKey<DataContainer> : WebSocketBehavior
    {
        struct Response
        {
            public string Key;
            public string Value;
            public Response(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }
        struct Request
        {
            public string key;
        }

        DataContainer container;
        public ConstantUpdateByKey(ref DataContainer dataContainer)
        {
            container = dataContainer;
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine("WebSocket message received: '{}'", e.Data);
            Request request = JsonConvert.DeserializeObject<Request>(e.Data);
            Send(
                JsonConvert.SerializeObject(
                    new Response (container.GetType().GetProperty(request.key).Name, container.GetType().GetProperty(request.key).ToString() )
                )
            );
        }

        public void UpdateField(string key)
        {
            Sessions?.Broadcast(
                JsonConvert.SerializeObject(
                    new Response(container.GetType().GetProperty(key).Name, container.GetType().GetProperty(key).GetValue(container).ToString())
                )
            );
        }
    }


    partial class DataSupplierServer<DataContainer>
    {
        WebSocketServer server;
        ConstantUpdateByKey<DataContainer> constantUpdate;

        public WebSocketServer Server
        {
            get
            {
                return server;
            }
        }

        public void UpdateField(string key)
        {
            constantUpdate.UpdateField(key);
        }

        public DataSupplierServer(ref DataContainer dataContainer)
        {
            // 8229 == [T]he[A]rcade[A]rcade[W]ebsocket
            server = new WebSocketServer(8229);
#if DEBUG
            server.Log.Level = LogLevel.Trace;
#else
            server.Log.Level = LogLevel.Error;
#endif

            constantUpdate = (ConstantUpdateByKey<DataContainer>)Activator.CreateInstance(typeof(ConstantUpdateByKey<DataContainer>), dataContainer);
            server.AddWebSocketService<ConstantUpdateByKey<DataContainer>>("/update", () => constantUpdate);
            server.Start();
        }

        ~DataSupplierServer()
        {

        }
    }

    class ROMData
    {
        private ROMDataSupplier server;
        public ROMDataSupplier Server
        {
            set
            {
                if (server == null)
                {
                    server = value;
                }
            }
        }

        private int score;
        public int Score
        {
            get
            {
                return score;
            }
            set
            {
                score = value;
                server?.UpdateField("Score");
            }
        }

        private int lives;
        public int Lives
        {
            get
            {
                return lives;
            }
            set
            {
                lives = value;
                server?.UpdateField("Lives");
            }
        }

        private int coins;
        public int Coins
        {
            get
            {
                return coins;
            }
            set
            {
                coins = value;
                server?.UpdateField("Coins");
            }
        }

        private string time;
        public string Time
        {
            get
            {
                return time;
            }
            set
            {
                time = value;
                server?.UpdateField("Time");
            }
        }

        public string state;
        public string State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
                server?.UpdateField("State");
            }
        }
    }

    class ROMDataSupplier : DataSupplierServer<ROMData>
    {
        ROMData container;
        public ROMDataSupplier(ref ROMData data)
            : base(ref data)
        {
            container = data;
            container.Server = this;
        }
    }
}
