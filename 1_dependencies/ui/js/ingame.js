class MemoryItem
{
    constructor(HTMLElement)
    {
        this.KeyElement = HTMLElement.querySelector(".key");
        this.ValueElement = HTMLElement.querySelector(".value");
        this.Key = HTMLElement.dataset["jsonKey"];
        this.Value = null;
        window.Databinding.Subscribe(this.Key, this.Update.bind(this));
    }

    Update(key, newValue)
    {
        if (this.Value === newValue)
        {
            return;
        }

        this.Value = newValue;
        this.ValueElement.innerHTML = this.Value;
    }
}

class GameContainer
{
    constructor(HTMLElement)
    {
        this.MemoryItems = {};
        const items = HTMLElement.querySelectorAll(".memoryItem");
        for (let i = 0; i < items.length; i++)
        {
            this.MemoryItems[items[i].dataset["jsonKey"]] = new MemoryItem(items[i]);
        }
    }
}

class DataWrapper
{
    constructor(HTMLElement)
    {
        this.GameContainer = {};
        const gameContainer = HTMLElement.querySelectorAll(".gameContainer");
        for (let i = 0; i < gameContainer.length; i++)
        {
            this.GameContainer[gameContainer[i].dataset["jsonKey"]] = new GameContainer(gameContainer[i]);
        }
    }

    IsRunningInBrowser()
    {
        return location.hash.length > 0 && !location.hash.includes("game");
    }

    IsDebug()
    {
        return location.hash.includes("debug");
    }

    SetGameWindowResolution(width, height)
    {
        document.querySelector(".gameCutout").style.width = width + "px";
        document.querySelector(".gameCutout").style.minWidth = width + "px";
        document.querySelector(".gameCutout").style.maxWidth = width + "px";
        document.querySelector(".gameWrapper").style.height = height + "px";
        document.querySelector(".gameWrapper").style.minHeight = height + "px";
        document.querySelector(".gameWrapper").style.maxHeight = height + "px";
    }
}