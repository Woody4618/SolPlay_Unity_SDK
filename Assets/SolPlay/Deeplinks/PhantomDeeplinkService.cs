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
using Solana.Unity.DeeplinkWallet;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
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

        public string EditorExampleWalletPublicKey;

        public IDeeplinkWallet DeeplinkWallet;

        private void Awake()
        {
            CryptoHelloWorld();

            ServiceFactory.Instance.RegisterSingleton(this);
            DeeplinkWallet = new DeeplinkWallet();
            DeeplinkWallet.Init("solplay", ServiceFactory.Instance.Resolve<NftService>().GarblesRpcClient,
                "https://www.beavercrush.com");

            DeeplinkWallet.OnDeeplinkTransactionSuccessful += OnDeeplinkTransactionSuccessful;
            DeeplinkWallet.OnDeeplinkWalletConnectionSuccess += OnDeeplinkWalletConnectionSuccess;
            DeeplinkWallet.OnDeeplinkWalletError += OnDeeplinkWalletError;
            DeeplinkWallet.OnDeepLinkTriggered += OnDeeplinkWalletOnOnDeepLinkTriggered;
        }

        private void OnDeeplinkWalletOnOnDeepLinkTriggered(string deeplinkUrl)
        {
            ServiceFactory.Instance.Resolve<MessageRouter>()
                .RaiseMessage(new BlimpSystem.ShowBlimpMessage(
                    $"Deeplink triggered: {deeplinkUrl}"));
        }

        private void OnDeeplinkWalletError(IDeeplinkWallet.DeeplinkWalletError error)
        {
            ServiceFactory.Instance.Resolve<MessageRouter>()
                .RaiseMessage(new BlimpSystem.ShowBlimpMessage(error.ErrorMessage));
        }

        private void OnDeeplinkWalletConnectionSuccess(
            IDeeplinkWallet.DeeplinkWalletConnectSuccess connectionSuccess)
        {
            var messageRouter = ServiceFactory.Instance.Resolve<MessageRouter>();
            messageRouter
                .RaiseMessage(new BlimpSystem.ShowBlimpMessage(
                    $"Phantom wallet connected pubkey: {connectionSuccess.PublicKey} session: {connectionSuccess.Session}"));
            ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new PhantomWalletConnectedMessage());
        }

        private async void OnDeeplinkTransactionSuccessful(
            IDeeplinkWallet.DeeplinkWalletTransactionSuccessful transactionSuccessful)
        {
            await CheckSignatureStatus(transactionSuccessful.Signature);
        }

        public void CallPhantomLogin()
        {
#if UNITY_WEBGL
            ServiceFactory.Instance.Resolve<JavaScriptWrapperService>().ConnectPhantomWallet();
#else
            DeeplinkWallet.Connect();
#endif
        }

        public bool TryGetPhantomPublicKey(out string phantomPublicKey)
        {
#if UNITY_EDITOR
            phantomPublicKey = EditorExampleWalletPublicKey;
            return true;
#endif
#if UNITY_WEBGL
            var javaScriptWrapperService = ServiceFactory.Instance.Resolve<JavaScriptWrapperService>();
            phantomPublicKey = javaScriptWrapperService.PublicKey;
            return string.IsNullOrEmpty(javaScriptWrapperService.PublicKey);
#else
            return DeeplinkWallet.TryGetWalletPublicKey(out phantomPublicKey);
#endif
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
                    messageRouter.RaiseMessage(
                        new BlimpSystem.ShowBlimpMessage($"There is no transaction for Signature: {signature}."));
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
                                new BlimpSystem.ShowBlimpMessage(
                                    $"Signature result {signatureStatusInfo.Confirmations}/31"));
                        }
                    }
                }

                if (!transactionFinalized)
                {
                    await Task.Delay(2000);
                }
            }
        }

        public async void TransferSolanaToPubkey(string toPublicKey)
        {
            var nftService = ServiceFactory.Instance.Resolve<NftService>();
            var blockHash = await nftService.GarblesRpcClient.GetRecentBlockHashAsync();

            if (blockHash.Result == null)
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage("Block hash null. Connected to internet?"));
                return;
            }

            var garblesSdkTransaction = CreateUnsignedTransferSolTransaction(toPublicKey, blockHash);
            DeeplinkWallet.SignAndSendTransaction(garblesSdkTransaction);
        }

        /// <summary>
        ///  WIP: Preparation for interacting with a custom smart contract on main net to save player data
        /// </summary>
        public async void SolanaHelloWorldTransaction()
        {
            var nftService = ServiceFactory.Instance.Resolve<NftService>();
            var blockHash = await nftService.GarblesRpcClient.GetRecentBlockHashAsync();

            if (blockHash.Result == null)
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage("Block hash null. Connected to internet?"));
                return;
            }

            CreateUnsignedHelloWorldTransaction(blockHash);
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
                new PublicKey(toPublicKey), 1000000));
            garblesSdkTransaction.FeePayer = new PublicKey(phantomPublicKey);
            garblesSdkTransaction.RecentBlockHash = blockHash.Result.Value.Blockhash;
            garblesSdkTransaction.Signatures = new List<SignaturePubKeyPair>();
            return garblesSdkTransaction;
        }

        // TODO: Does not work yet, since it needs a program account first: probably SystemProgram.createAccountWithSeed
        private async void CreateUnsignedHelloWorldTransaction(RequestResult<ResponseValue<BlockHash>> blockHash)
        {
            if (!TryGetPhantomPublicKey(out string phantomPublicKey))
            {
                return;
            }

            string Account_Seed = "HelloWorld";

            bool createdAccount = PublicKey.TryCreateWithSeed(
                new PublicKey(phantomPublicKey),
                Account_Seed,
                new PublicKey("F3qQ9mJep9hwCkJRtRSUcxov5etdRvQU9NBFpPjh4LKo"),
                out PublicKey tokenDerivedAcressFromSeed);

            var garblesRpcClient = ServiceFactory.Instance.Resolve<NftService>().GarblesRpcClient;
            RequestResult<ResponseValue<AccountInfo>> accountInfo = await garblesRpcClient.GetAccountInfoAsync(tokenDerivedAcressFromSeed);

            var lamports = await garblesRpcClient.GetMinimumBalanceForRentExemptionAsync(4);
            
            List<AccountMeta> accountMetaList = new List<AccountMeta>()
            {
                AccountMeta.Writable(tokenDerivedAcressFromSeed, false),
                AccountMeta.Writable(new PublicKey(phantomPublicKey), true)
            };

            Transaction createAccountTransaction = new Transaction();
            createAccountTransaction.Instructions = new List<TransactionInstruction>();
            if (accountInfo.Result == null)
            {
                TransactionInstruction createAccountInstruction = SystemProgram.CreateAccountWithSeed(
                    new PublicKey(phantomPublicKey),
                    tokenDerivedAcressFromSeed,
                    new PublicKey(phantomPublicKey),
                    Account_Seed,
                    lamports.Result,
                    4,
                    new PublicKey("F3qQ9mJep9hwCkJRtRSUcxov5etdRvQU9NBFpPjh4LKo"));
                
                createAccountTransaction.Instructions.Add(createAccountInstruction);
            }
            else
            {
                Debug.Log(accountInfo.Result);
            }
            
            createAccountTransaction.FeePayer = new PublicKey(phantomPublicKey);
            createAccountTransaction.RecentBlockHash = blockHash.Result.Value.Blockhash;
            createAccountTransaction.Signatures = new List<SignaturePubKeyPair>();

            TransactionInstruction helloWorldTransactionInstruction = new TransactionInstruction()
            {
                ProgramId = Base58.Decode("F3qQ9mJep9hwCkJRtRSUcxov5etdRvQU9NBFpPjh4LKo"),
                Keys = (IList<AccountMeta>) accountMetaList,
                Data = Array.Empty<byte>()
            };
            
            createAccountTransaction.Instructions.Add(helloWorldTransactionInstruction);
            DeeplinkWallet.SignAndSendTransaction(createAccountTransaction);
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

        public class PhantomWalletConnectedMessage
        {
        }
    }
}