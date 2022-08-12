using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Frictionless;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using UnityEngine;
using SystemProgram = Solana.Unity.Programs.SystemProgram;

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

        private void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);
        }

        /// <summary>
        /// await Task.Delay() does not work properly on webgl so we use a coroutine instead
        /// </summary>
        public void CheckSignatureStatus(string signature)
        {
            StartCoroutine(CheckSignatureStatusRoutine(signature));
        }

        private IEnumerator CheckSignatureStatusRoutine(string signature)
        {
            MessageRouter messageRouter = ServiceFactory.Instance.Resolve<MessageRouter>();
            var wallet = ServiceFactory.Instance.Resolve<WalletHolderService>().BaseWallet;

            bool transactionFinalized = false;

            while (!transactionFinalized)
            {
                Task<RequestResult<ResponseValue<List<SignatureStatusInfo>>>> task = wallet.ActiveRpcClient.GetSignatureStatusesAsync(new List<string>() {signature}, true);
                yield return new WaitUntil(() => task.IsCompleted);

                RequestResult<ResponseValue<List<SignatureStatusInfo>>> signatureResult = task.Result;

                if (signatureResult.Result == null)
                {
                    messageRouter.RaiseMessage(
                        new BlimpSystem.ShowBlimpMessage($"There is no transaction for Signature: {signature}."));
                    yield return new WaitForSeconds(1.5f);
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
                            messageRouter.RaiseMessage(new BlimpSystem.ShowBlimpMessage("Transaction finalized"));
                            transactionFinalized = true;
                        }
                        else
                        {
                            messageRouter.RaiseMessage(
                                new BlimpSystem.ShowBlimpMessage(
                                    $"Signature result {signatureStatusInfo.Confirmations}/31"));
                        }
                    }
                }

                yield return new WaitForSeconds(1.5f);
            }
        }


        public async void TransferSolanaToPubkey(string toPublicKey)
        {
            var wallet = ServiceFactory.Instance.Resolve<WalletHolderService>().BaseWallet;
            var walletHolderService = ServiceFactory.Instance.Resolve<WalletHolderService>();
            var blockHash = await wallet.ActiveRpcClient.GetRecentBlockHashAsync();

            if (blockHash.Result == null)
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage("Block hash null. Connected to internet?"));
                return;
            }

            var transferSolTransaction = CreateUnsignedTransferSolTransaction(toPublicKey, blockHash);

            RequestResult<string> requestResult =
                await walletHolderService.BaseWallet.SignAndSendTransaction(transferSolTransaction);

            CheckSignatureStatus(requestResult.Result);
        }

        private Transaction CreateUnsignedTransferSolTransaction(string toPublicKey,
            RequestResult<ResponseValue<BlockHash>> blockHash)
        {
            var walletHolderService = ServiceFactory.Instance.Resolve<WalletHolderService>();
            if (!walletHolderService.TryGetPhantomPublicKey(out string phantomPublicKey))
            {
                return null;
            }

            Transaction garblesSdkTransaction = new Transaction();
            garblesSdkTransaction.Instructions = new List<TransactionInstruction>();
            var transactionInstruction = SystemProgram.Transfer(new PublicKey(phantomPublicKey),
                new PublicKey(toPublicKey), 1000000);
            garblesSdkTransaction.Instructions.Add(transactionInstruction);
            garblesSdkTransaction.FeePayer = new PublicKey(phantomPublicKey);
            garblesSdkTransaction.RecentBlockHash = blockHash.Result.Value.Blockhash;
            garblesSdkTransaction.NonceInformation = new NonceInformation()
            {
                Instruction = transactionInstruction,
                Nonce = blockHash.Result.Value.Blockhash
            };
            garblesSdkTransaction.Signatures = new List<SignaturePubKeyPair>();
            return garblesSdkTransaction;
        }
    }
}