using System;

namespace Solnet.Rpc.Messages
{
    [Serializable]
    public class JsonRpcBase
    {
        public string jsonrpc;

        public int id;
    }
}
