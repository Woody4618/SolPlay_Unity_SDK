using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using base58;
using Frictionless;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solnet.Programs;
using Solnet.Rpc.Builders;
using UnityEngine;
using UnityEngine.Networking;
using X25519;
using Account = Solnet.Wallet.Account;
using SystemProgram = Solana.Unity.Programs.SystemProgram;

namespace SolPlay.Deeplinks
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
        private string PhantomWalletPublicKey;

        private X25519KeyPair localKeyPairForPhantomConnection;
        private string base58PublicKey = "";
        private byte[] publicKey;
        private byte[] privateKey;
        public string EditorExampleWalletPublicKey = "AFEkH2vF1CYGJnPncDw6PzaitjQgdQipL2hxSWLh9iDs";
        private string phantomNonce;
        private string phantomEncryptionPubKey;
        
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

        public string GetPhantomPublicKey()
        {
            #if UNITY_EDITOR
            return EditorExampleWalletPublicKey;
            #endif
            return PhantomWalletPublicKey;
        }
        
        public bool TryGetPhantomPublicKey(out string phantomPublicKey)
        {
            if (!string.IsNullOrEmpty(PhantomWalletPublicKey))
            {
                phantomPublicKey = PhantomWalletPublicKey;
                return true;
            }
#if UNITY_EDITOR
            phantomPublicKey = EditorExampleWalletPublicKey;
            return true;
#else
            phantomPublicKey = "";
            return false;
#endif
        }

        private void OnDeepLinkActivated(string url)
        {
            DeeplinkURL = url;
            Debug.Log("On phantom connect: " + DeeplinkURL);

            if (url.Contains("transactionSuccessful"))
            {
                ParseSuccessfullTransaction(url);
                return;
            }
            
            string phantomResponse = url.Split("?"[0])[1];

            NameValueCollection result = HttpUtility.ParseQueryString(phantomResponse);
            phantomEncryptionPubKey = result.Get("phantom_encryption_public_key");
            phantomNonce = result.Get("nonce");
            string data = result.Get("data");
            string errorMessage = result.Get("errorMessage");

            if (!string.IsNullOrEmpty(errorMessage))
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage(errorMessage));
                return;
            }
            
            if (string.IsNullOrEmpty(data))
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage("Phantom connect canceled."));
                return;
            }
            
            byte[] uncryptedMessage = TweetNaCl.TweetNaCl.CryptoBoxOpen(Base58.Decode(data), Base58.Decode(phantomNonce),
                Base58.Decode(phantomEncryptionPubKey), localKeyPairForPhantomConnection.PrivateKey);
            Debug.Log("Decrypted message bytes: " + uncryptedMessage);

            string bytesToUtf8String = Encoding.UTF8.GetString(uncryptedMessage);
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

        private void ParseSuccessfullTransaction(string url)
        {
            string phantomResponse = url.Split("?"[0])[1];

            NameValueCollection result = HttpUtility.ParseQueryString(phantomResponse);
            var nonce = result.Get("nonce");
            string data = result.Get("data");
            string errorMessage = result.Get("errorMessage");

            if (!string.IsNullOrEmpty(errorMessage))
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage(errorMessage));
                return;
            }

            Debug.Log($"data {data}");

            byte[] uncryptedMessage = TweetNaCl.TweetNaCl.CryptoBoxOpen(Base58.Decode(data), Base58.Decode(nonce),
                Base58.Decode(phantomEncryptionPubKey), localKeyPairForPhantomConnection.PrivateKey);
            string bytesToUtf8String = Encoding.UTF8.GetString(uncryptedMessage);

            Debug.Log($"data {data} decrypted: " + bytesToUtf8String);
            
            PhantomWalletTransactionSuccessfull success = JsonUtility.FromJson<PhantomWalletTransactionSuccessfull>(bytesToUtf8String);
            
            ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new BlimpSystem.ShowBlimpMessage($"Phantom transaction sent successfully with signarture: {success.signature}"));
        }

        private void CreateNewKey()
        {
            localKeyPairForPhantomConnection = X25519KeyAgreement.GenerateKeyPair();
            publicKey = localKeyPairForPhantomConnection.PublicKey;
            privateKey = localKeyPairForPhantomConnection.PrivateKey;
            base58PublicKey = Base58.Encode(publicKey);
            Debug.Log($"Created new keypair: private key {Base58.Encode(privateKey)} public key: {Base58.Encode(publicKey)}");
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

        [Serializable]
        private class PhantomTransactionPayload
        {
            public string transaction;
            public string session;

            public PhantomTransactionPayload(string transaction, string session)
            {
                this.transaction = transaction;
                this.session = session;
            }
        } 
        
        public async void SolanaTransferTransaction(string toPublicKey)
        {
            #if UNITY_EDITOR
            CreateNewKey();
            phantomEncryptionPubKey = EditorExampleWalletPublicKey;
            #endif
            
            var nftService = ServiceFactory.Instance.Resolve<NftService>();
            
            var blockHash = await nftService.rpcClient.GetRecentBlockHashAsync();

            if (blockHash.Result == null)
            {
                ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new BlimpSystem.ShowBlimpMessage("Blockhash null. Connected to internet?"));
                return;
            }
            
            string redirectUri = $"{DeeplinkUrlSceme}://transactionSuccessful";
            
            byte[] randomNonce = new byte[24]; 
            TweetNaCl.TweetNaCl.RandomBytes(randomNonce);

            
            Transaction garblesTransaction = new Transaction();
            garblesTransaction.Instructions = new List<TransactionInstruction>();
            garblesTransaction.Instructions.Add(SystemProgram.Transfer(new PublicKey(GetPhantomPublicKey()), new PublicKey(toPublicKey), 100000000));
            garblesTransaction.FeePayer = new PublicKey(GetPhantomPublicKey());
            garblesTransaction.RecentBlockHash = blockHash.Result.Value.Blockhash;
            garblesTransaction.Signatures = new List<SignaturePubKeyPair>();
            
            
            byte[] compileMessage = garblesTransaction.Serialize();
            string base58Transaction = Base58.Encode(compileMessage);

            
            var transactionPayload = new PhantomTransactionPayload(base58Transaction, SessionId);
            string transactionPayloadJson = JsonUtility.ToJson(transactionPayload);
            Debug.Log(transactionPayloadJson);

            
            byte[] bytesJson = Encoding.UTF8.GetBytes(transactionPayloadJson);
         
            byte[] encryptedMessage = TweetNaCl.TweetNaCl.CryptoBox(bytesJson, randomNonce, Base58.Decode(phantomEncryptionPubKey), privateKey);
            
            string base58Payload = Base58.Encode(encryptedMessage);

            string url =
                $"https://phantom.app/ul/v1/signAndSendTransaction?dapp_encryption_public_key={base58PublicKey}&redirect_link={redirectUri}&nonce={Base58.Encode(randomNonce)}&payload={base58Payload}";

            Debug.Log("Transaction Url: " + url);
            Application.OpenURL(url);
        }

        private void CryptoHelloWorld()
        {
            var keyPair_local = X25519KeyAgreement.GenerateKeyPair();
            var keyPair_phantom = X25519KeyAgreement.GenerateKeyPair();

            string halloWeltString = "Hallo Welt";
            
            byte[] randomNonce = new byte[24]; 
            TweetNaCl.TweetNaCl.RandomBytes(randomNonce);
            byte[] encryptedMessage = TweetNaCl.TweetNaCl.CryptoBox(Encoding.UTF8.GetBytes(halloWeltString), randomNonce, keyPair_phantom.PublicKey, keyPair_local.PrivateKey);
            string utfString2 = Encoding.UTF8.GetString(encryptedMessage, 0, encryptedMessage.Length);   
            Debug.Log(utfString2);

            byte[] decryptedMessage = TweetNaCl.TweetNaCl.CryptoBoxOpen(encryptedMessage, randomNonce, keyPair_local.PublicKey, keyPair_phantom.PrivateKey);
            string utfString = Encoding.UTF8.GetString(decryptedMessage, 0, decryptedMessage.Length);   

            Debug.Log(utfString);
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

        [Serializable]
        public class PhantomWalletTransactionSuccessfull
        {
            public string signature;
        }

        public class PhantomWalletConnectedMessage
        {
        }
    }
}