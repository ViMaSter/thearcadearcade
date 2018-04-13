if (typeof CefSharp == "undefined")
{
    const endOfMock = function(additionalData)
    {
        console.groupCollapsed("Reached end of mock-method%s", typeof additionalData == "undefined" ? "" : " - additional data: " + additionalData);
        console.log(new Error().stack);
        console.groupEnd();
    }

    CefSharp =
    {
        BindObjectAsync: function (objectName)
        {
            console.log("Skipping loading of object '%s' as we're mocking...", objectName);
        }
    }
    app =
    {
        exit: endOfMock
    };
}

(async function(){
    await CefSharp.BindObjectAsync("app");
})();
