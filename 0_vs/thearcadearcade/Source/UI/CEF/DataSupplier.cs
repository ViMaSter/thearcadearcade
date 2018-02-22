using System;
using WebSocketSharp;
using WebSocketSharp.Server;

using Newtonsoft.Json;

namespace thearcadearcade.UI.CEF
{
    partial class AllReturnService<DataContainer> : WebSocketBehavior
    {
        DataContainer container;
        public AllReturnService(ref DataContainer dataContainer)
        {
            container = dataContainer;
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine("WebSocket message received: '{}'", e.Data);
            Send(JsonConvert.SerializeObject(container));
        }
    }

    partial class ByKeyReturnService<DataContainer> : WebSocketBehavior
    {
        struct Response
        {
            public string Type;
            public string Value;
            public Response(string type, string value)
            {
                Type = type;
                Value = value;
            }
        }
        struct Request
        {
            public string key;
        }

        DataContainer container;
        public ByKeyReturnService(ref DataContainer dataContainer)
        {
            container = dataContainer;
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine("WebSocket message received: '{}'", e.Data);
            Request request = JsonConvert.DeserializeObject<Request>(e.Data);
            Send(
                JsonConvert.SerializeObject(
                    new Response (container.GetType().GetProperty(request.key).PropertyType.Name, container.GetType().GetProperty(request.key).ToString() )
                )
            );
        }
    }


    partial class DataSupplierServer<DataContainer>
    {
        WebSocketServer server;
        AllReturnService<DataContainer> allReturnService;

        public WebSocketServer Server
        {
            get
            {
                return server;
            }
        }


        public DataSupplierServer(ref DataContainer dataContainer)
        {
            // 8228 == [T]he[A]rcade[A]rcade[W]ebsocket
            server = new WebSocketServer(8228);
#if DEBUG
            server.Log.Level = LogLevel.Trace;
#else
            server.Log.Level = LogLevel.Error;
#endif

            allReturnService = (AllReturnService<DataContainer>)Activator.CreateInstance(typeof(AllReturnService<DataContainer>), dataContainer);
            server.AddWebSocketService<AllReturnService<DataContainer>>("/alldata", () => allReturnService);
        }

        ~DataSupplierServer()
        {

        }
    }

    class ROMData
    {
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
            }
        }
    }

    class ROMDataSupplier : DataSupplierServer<ROMData>
    {
        public ROMDataSupplier(ref ROMData data)
            : base(ref data)
        {

        }
    }
}
