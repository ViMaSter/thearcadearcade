// If you happen to read this: Thanks for making UI-development less painful, knight666. <3
class Databinding
{
    constructor()
    {
        this.values = {};
        this.subscriptions = {};
    }

    GetValue(key)
    {
        if (!values.hasOwnProperty(key))
        {
            return this.values[key];
        }
        return null;
    }

    UpdateValue(key, value)
    {
        if (this.subscriptions.hasOwnProperty(key))
        {
            for (let i = 0; i < this.subscriptions[key].length; i++)
            {
                if (this.subscriptions[key])
                {
                    this.subscriptions[key][i](key, value);
                }
            }
        }

        this.values[key] = value;
    }

    Subscribe(key, callback)
    {
        if (!this.subscriptions.hasOwnProperty(key))
        {
            this.subscriptions[key] = [];
        }

        return this.subscriptions[key].push(callback);
    }
    Unsubscribe(key, id)
    {
        if (this.subscriptions.hasOwnProperty(key))
        {
            console.warn("Trying to unsubscribe id '%d' from key '%s' but nothing is subscribed to this key!", id, key);
            return false;
        }

        if (this.subscriptions[key].hasOwnProperty(id))
        {
            console.warn("Trying to unsubscribe id '%d' from key '%s' but there's only %d listeners subscribed!", id, key, this.subscriptions[key].length);
            return false;
        }

        this.subscriptions[key][id] = null;
    }
}