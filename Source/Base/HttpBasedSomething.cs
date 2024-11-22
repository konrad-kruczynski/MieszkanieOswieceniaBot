using System;
using Flurl.Http;

namespace MieszkanieOswieceniaBot.Base;

public abstract class HttpBasedSomething
{
    protected IFlurlClient FlurlClient;

    protected HttpBasedSomething(string hostname, TimeSpan timeout = default)
    {
        var flurlClient = new FlurlClient($"http://{hostname}");
        if (timeout != default)
        {
            flurlClient = flurlClient.WithTimeout(timeout);
        }
            
        FlurlClient = flurlClient;
    }
}