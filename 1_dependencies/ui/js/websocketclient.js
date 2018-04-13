class WebsocketClient
{
    constructor(DataWrapper)
    {
        this.dw = DataWrapper;
        this.ws = new WebSocket("ws://localhost:8229/update");
        this.ws.onopen = this.OnOpen.bind(this);
        this.ws.onclose = this.OnClose.bind(this);
        this.ws.onmessage = this.OnMessage.bind(this);
    }

    OnOpen(event)
    {
        console.log("Open event!");
        console.log(event);
    }

    OnClose(event)
    {
        console.log("Close event!");
        console.log(event);
    }

    OnMessage(event)
    {
        const response = JSON.parse(event.data);
        this.OnKeyUpdate(response.Key, response.Value);
    }

    OnKeyUpdate(key, value)
    {
        window.Databinding.UpdateValue(key, value);
    }
}