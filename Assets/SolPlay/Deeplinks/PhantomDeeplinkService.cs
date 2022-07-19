using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using base58;
using Frictionless;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using UnityEngine;
using UnityEngine.Networking;
using X25519;
using SystemProgram = Solana.Unity.Programs.SystemProgram;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// Establishes a secure connection with phantom wallet and gets the public key from the wallet. 
    /// </summary>
    public class PhantomDeeplinkService : MonoBehaviour
    {
        public enum TransactionResult
        {
            processed = 0,
            confirmed = 1,
            finalized = 2
        }

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
            CryptoHelloWorld();

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

        public void CallPhantomLogin()
        {
            CreateNewKey();

            string appMetaDataUrl = AppMetaDataUrl;
            string redirectUri = $"{DeeplinkUrlSceme}://onPhantomConnected";
            string url =
                $"https://phantom.app/ul/v1/connect?app_url={appMetaDataUrl}&dapp_encryption_public_key={base58PublicKey}&redirect_link={redirectUri}";

            Application.OpenURL(url);
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

            byte[] uncryptedMessage = TweetNaCl.TweetNaCl.CryptoBoxOpen(Base58.Decode(data),
                Base58.Decode(phantomNonce),
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

        private async void ParseSuccessfullTransaction(string url)
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

            PhantomWalletTransactionSuccessfull success =
                JsonUtility.FromJson<PhantomWalletTransactionSuccessfull>(bytesToUtf8String);

            ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(
                new BlimpSystem.ShowBlimpMessage(
                    $"Phantom transaction sent successfully with signature: {success.signature}"));

            await CheckSignatureStatus(success.signature);
        }

    private async Task CheckSignatureStatus(string signature)
    {
        NftService nftService = ServiceFactory.Instance.Resolve<NftService>();
        MessageRouter messageRouter = ServiceFactory.Instance.Resolve<MessageRouter>();

        bool transactionFinalized = false;

        while (!transactionFinalized)
        {
            RequestResult<ResponseValue<List<SignatureStatusInfo>>> signatureResult =
                await nftService.GarblesRpcClient.GetSignatureStatusesAsync(new List<string>() {signature}, true);

            if (signatureResult.Result == null)
            {
                messageRouter.RaiseMessage(new BlimpSystem.ShowBlimpMessage($"There is no transaction for Signature: {signature}."));
                await Task.Delay(2000);
                continue;
            }

            foreach (var signatureStatusInfo in signatureResult.Result.Value)
            {
                if (signatureStatusInfo == null)
                {
                    messageRouter.RaiseMessage(
                        new BlimpSystem.ShowBlimpMessage("Signature is not yet processed. Retry in 2 seconds."));
                }
                else
                {
                    if (signatureStatusInfo.ConfirmationStatus == nameof(TransactionResult.finalized))
                    {
                        transactionFinalized = true;
                        messageRouter.RaiseMessage(new BlimpSystem.ShowBlimpMessage("Transaction finalized"));
                    }
                    else
                    {
                        messageRouter.RaiseMessage(
                            new BlimpSystem.ShowBlimpMessage($"Signature result {signatureStatusInfo.Confirmations}/31"));
                    }
                }
            }

            if (!transactionFinalized)
            {
                await Task.Delay(2000);
            }
        }
    }

        private void CreateNewKey()
        {
            localKeyPairForPhantomConnection = X25519KeyAgreement.GenerateKeyPair();
            publicKey = localKeyPairForPhantomConnection.PublicKey;
            privateKey = localKeyPairForPhantomConnection.PrivateKey;
            base58PublicKey = Base58.Encode(publicKey);
            Debug.Log(
                $"Created new keypair: private key {Base58.Encode(privateKey)} public key: {Base58.Encode(publicKey)}");
        }

        public async void SolanaTransferTransaction(string toPublicKey)
        {
#if UNITY_EDITOR
            CreateNewKey();
            phantomEncryptionPubKey = EditorExampleWalletPublicKey;
#endif

            var nftService = ServiceFactory.Instance.Resolve<NftService>();

            var blockHash = await nftService.GarblesRpcClient.GetRecentBlockHashAsync();

            if (blockHash.Result == null)
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage("Blockhash null. Connected to internet?"));
                return;
            }

            string redirectUri = $"{DeeplinkUrlSceme}://transactionSuccessful";

            var garblesSdkTransaction = CreateUnsignedTransferSolTransaction(toPublicKey, blockHash);
            byte[] serializedTransaction = garblesSdkTransaction.Serialize();
            string base58Transaction = Base58.Encode(serializedTransaction);

            var transactionPayload = new PhantomTransactionPayload(base58Transaction, SessionId);
            string transactionPayloadJson = JsonUtility.ToJson(transactionPayload);
            Debug.Log(transactionPayloadJson);


            byte[] bytesJson = Encoding.UTF8.GetBytes(transactionPayloadJson);


            byte[] randomNonce = new byte[24];
            TweetNaCl.TweetNaCl.RandomBytes(randomNonce);
            byte[] encryptedMessage = TweetNaCl.TweetNaCl.CryptoBox(bytesJson, randomNonce,
                Base58.Decode(phantomEncryptionPubKey), privateKey);

            string base58Payload = Base58.Encode(encryptedMessage);

            string url =
                $"https://phantom.app/ul/v1/signAndSendTransaction?dapp_encryption_public_key={base58PublicKey}&redirect_link={redirectUri}&nonce={Base58.Encode(randomNonce)}&payload={base58Payload}";

            Debug.Log("Transaction Url: " + url);
            Application.OpenURL(url);
        }

        private Transaction CreateUnsignedTransferSolTransaction(string toPublicKey,
            RequestResult<ResponseValue<BlockHash>> blockHash)
        {
            if (!TryGetPhantomPublicKey(out string phantomPublicKey))
            {
                return null;
            }

            Transaction garblesSdkTransaction = new Transaction();
            garblesSdkTransaction.Instructions = new List<TransactionInstruction>();
            garblesSdkTransaction.Instructions.Add(SystemProgram.Transfer(new PublicKey(phantomPublicKey),
                new PublicKey(toPublicKey), 100000000));
            garblesSdkTransaction.FeePayer = new PublicKey(phantomPublicKey);
            garblesSdkTransaction.RecentBlockHash = blockHash.Result.Value.Blockhash;
            garblesSdkTransaction.Signatures = new List<SignaturePubKeyPair>();
            return garblesSdkTransaction;
        }

        private void CryptoHelloWorld()
        {
            var keyPair_local = X25519KeyAgreement.GenerateKeyPair();
            var keyPair_phantom = X25519KeyAgreement.GenerateKeyPair();

            string halloWeltString = "Hallo Welt";

            byte[] randomNonce = new byte[24];
            TweetNaCl.TweetNaCl.RandomBytes(randomNonce);
            byte[] encryptedMessage = TweetNaCl.TweetNaCl.CryptoBox(Encoding.UTF8.GetBytes(halloWeltString),
                randomNonce, keyPair_phantom.PublicKey, keyPair_local.PrivateKey);
            string utfString2 = Encoding.UTF8.GetString(encryptedMessage, 0, encryptedMessage.Length);
            Debug.Log(utfString2);

            byte[] decryptedMessage = TweetNaCl.TweetNaCl.CryptoBoxOpen(encryptedMessage, randomNonce,
                keyPair_local.PublicKey, keyPair_phantom.PrivateKey);
            string utfString = Encoding.UTF8.GetString(decryptedMessage, 0, decryptedMessage.Length);

            Debug.Log(utfString);
        }

        [Serializable]
        private class PhantomTransactionPayload
        {
            public string transaction;
            public string session;

            public PhantomTransactionPayload(string serializedBase58EncodedTransaction, string session)
            {
                transaction = serializedBase58EncodedTransaction;
                this.session = session;
            }
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