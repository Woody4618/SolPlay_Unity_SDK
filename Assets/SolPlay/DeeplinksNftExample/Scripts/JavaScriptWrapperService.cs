using System.Runtime.InteropServices;
using Frictionless;
using UnityEngine;

namespace SolPlay.Deeplinks
{
    public class JavaScriptWrapperService : MonoBehaviour
    {
#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void ConnectPhantom();
#endif
        public string PublicKey { get; set; }

        public void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);
        }

        public void OnPhantomConnected(string walletPubKey)
        {
            Debug.Log($"Wallet {walletPubKey} connected!");
            PublicKey = walletPubKey;
        }

        public void ConnectPhantomWallet()
        {
#if UNITY_WEBGL
            ConnectPhantom();
#endif
        }

    }
}