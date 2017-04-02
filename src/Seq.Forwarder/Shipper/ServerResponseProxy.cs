namespace Seq.Forwarder.Shipper
{
    class ServerResponseProxy
    {
        const string EmptyResponse = "{}";

        readonly object _syncRoot = new object();
        string _lastUsedApiKey;
        string _lastResponse;

        public void ResponseReturned(string apiKey, string response)
        {
            lock (_syncRoot)
            {
                _lastUsedApiKey = apiKey;
                _lastResponse = response;
            }
        }

        public string GetResponseText(string apiKey)
        {
            lock (_syncRoot)
            {
                if (apiKey == _lastUsedApiKey)
                    return _lastResponse;
                return EmptyResponse;
            }
        }
    }
}
