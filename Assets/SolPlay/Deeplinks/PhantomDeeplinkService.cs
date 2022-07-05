using System;
using System.Collections.Specialized;
using System.Web;
using base58;
using Frictionless;
using UnityEngine;
using UnityEngine.Networking;
using X25519;

namespace Solplay.Deeplinks
{
    /// <summary>
    /// Establishes a secure connection with phantom wallet and gets the public key from the wallet. 
    /// </summary>
    public class PhantomDeeplinkService : MonoBehaviour
    {
        public string DeeplinkURL;
        public string AppMetaDataUrl = "https://beavercrush.com";
        [Header("You can find this in the player settings.")]
        public string DeeplinkUrlSceme = "SolPlay";

        public static string SessionId { get; private set; }
        public string PhantomWalletPublicKey { get; private set; }

        private X25519KeyPair localKeyPairForPhantomConnection;
        private string base58PublicKey = "";
        private byte[] publicKey;
        private byte[] privateKey;

        private void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);

            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!String.IsNullOrEmpty(Application.absoluteURL))
            {
                OnDeepLinkActivated(Application.absoluteURL);
            }
            else
            {
                DeeplinkURL = "[none]";
            }
        }

        public bool TryGetPhantomPublicKey(out string phantomPublicKey)
        {
            if (!string.IsNullOrEmpty(PhantomWalletPublicKey))
            {
                phantomPublicKey = PhantomWalletPublicKey;
                return true;
            }
#if UNITY_EDITOR
            phantomPublicKey = "pLAY7z6bY7SRvhCm8hSqyXaerAegsdGWkV1aSgjFpab";
            return true;
#else
            phantomPublicKey = "";
#endif
            return false;
        }

        private void OnDeepLinkActivated(string url)
        {
            DeeplinkURL = url;
            Debug.Log("On phantom connect: " + DeeplinkURL);

            string phantomResponse = url.Split("?"[0])[1];

            NameValueCollection result = HttpUtility.ParseQueryString(phantomResponse);
            string pubKey = result.Get("phantom_encryption_public_key");
            string nonce = result.Get("nonce");
            string data = result.Get("data");

            if (string.IsNullOrEmpty(data))
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage("Phantom connect canceled."));
                return;
            }

            byte[] uncryptedMessage = TweetNaCl.TweetNaCl.CryptoBoxOpen(Base58.Decode(data), Base58.Decode(nonce),
                Base58.Decode(pubKey), localKeyPairForPhantomConnection.PrivateKey);
            Debug.Log("Decrypted message bytes: " + uncryptedMessage);

            string bytesToUtf8String = System.Text.Encoding.UTF8.GetString(uncryptedMessage);
            Debug.Log("bytesToUtf8String: " + bytesToUtf8String);

            PhantomWalletSuccess success = JsonUtility.FromJson<PhantomWalletSuccess>(bytesToUtf8String);
            PhantomWalletError error = JsonUtility.FromJson<PhantomWalletError>(bytesToUtf8String);

            if (!string.IsNullOrEmpty(success.public_key))
            {
                Debug.Log("Pub key: " + success.public_key);
                PhantomWalletPublicKey = success.public_key;
                SessionId = success.session;
                ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new PhantomWalletConnectedMessage());
            }
            else
            {
                if (!string.IsNullOrEmpty(error.errorCode))
                {
                    Debug.LogError("Error: " + error.errorCode);
                }
            }
        }

        private void CreateNewKey()
        {
            localKeyPairForPhantomConnection = X25519KeyAgreement.GenerateKeyPair();
            publicKey = localKeyPairForPhantomConnection.PublicKey;
            privateKey = localKeyPairForPhantomConnection.PrivateKey;
            base58PublicKey = Base58.Encode(publicKey);
        }

        public void CallPhantomLogin()
        {
            CreateNewKey();

            string appMetaDataUrl = AppMetaDataUrl;
            string redirectUri = $"{DeeplinkUrlSceme}://onPhantomConnected";
            string url =
                $"https://phantom.app/ul/v1/connect?app_url={appMetaDataUrl}&dapp_encryption_public_key={base58PublicKey}&redirect_link={redirectUri}";

            Application.OpenURL(url);
        }

        public void OpenInPhantomMobileBrowser(string url)
        {
#if UNITY_EDITOR
            string phantomUrl = url;
#else
            string refUrl = UnityWebRequest.EscapeURL(AppMetaDataUrl);
            string phantomUrl = $"https://phantom.app/ul/browse/{url}?ref=refUrl";
#endif
            Application.OpenURL(phantomUrl);
        }

        [Serializable]
        public class PhantomWalletError
        {
            public string errorCode;
            public string errorMessage;
        }

        [Serializable]
        public class PhantomWalletSuccess
        {
            public string public_key;
            public string session;
        }

        public class PhantomWalletConnectedMessage
        {
        }
    }
}