using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Frictionless;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using SolPlay.DeeplinksNftExample.Utils;
using UnityEngine;
using SystemProgram = Solana.Unity.Programs.SystemProgram;

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// Establishes a secure connection with phantom wallet and gets the public key from the wallet. 
    /// </summary>
    public class TransactionService : MonoBehaviour
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
        public void CheckSignatureStatus(string signature, Action onSignatureFinalized, TransactionResult transactionResult = TransactionResult.confirmed)
        {
            MessageRouter messageRouter = ServiceFactory.Instance.Resolve<MessageRouter>();
            
            if (string.IsNullOrEmpty(signature))
            {
                messageRouter.RaiseMessage(
                    new BlimpSystem.ShowBlimpMessage($"Signature was empty: {signature}."));
            }
            else
            {
                StartCoroutine(CheckSignatureStatusRoutine(signature, onSignatureFinalized, transactionResult));
            }
        }

        private IEnumerator CheckSignatureStatusRoutine(string signature, Action onSignatureFinalized, TransactionResult transactionResult = TransactionResult.confirmed)
        {
            MessageRouter messageRouter = ServiceFactory.Instance.Resolve<MessageRouter>();
            var wallet = ServiceFactory.Instance.Resolve<WalletHolderService>().BaseWallet;

            bool transactionFinalized = false;

            int counter = 0;
            int maxTries = 30;

            while (!transactionFinalized && counter < maxTries)
            {
                counter++;
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
                            new BlimpSystem.ShowBlimpMessage($"Signature is not yet processed. Try: {counter}. Retry in 2 seconds."));
                    }
                    else
                    {
                        if (signatureStatusInfo.ConfirmationStatus == Enum.GetName(typeof(TransactionResult), transactionResult) ||
                            signatureStatusInfo.ConfirmationStatus ==  Enum.GetName(typeof(TransactionResult), TransactionResult.finalized))
                        {
                            messageRouter.RaiseMessage(new BlimpSystem.ShowBlimpMessage("Transaction finalized"));
                            transactionFinalized = true;
                            onSignatureFinalized();
                        }
                        else
                        {
                            messageRouter.RaiseMessage(
                                new BlimpSystem.ShowBlimpMessage(
                                    $"Signature result {signatureStatusInfo.Confirmations}/31 status: {signatureStatusInfo.ConfirmationStatus} target: {Enum.GetName(typeof(TransactionResult), transactionResult)}"));
                        }
                    }
                }

                yield return new WaitForSeconds(1.5f);
            }

            if (counter >= maxTries)
            {
                messageRouter.RaiseMessage(
                    new BlimpSystem.ShowBlimpMessage(
                        $"Tried {counter} times. The transaction probably failed :( "));
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

            CheckSignatureStatus(requestResult.Result, () =>
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new SolBalanceChangedMessage());
            }, TransactionResult.finalized);
        }

        private Transaction CreateUnsignedTransferSolTransaction(string toPublicKey,
            RequestResult<ResponseValue<BlockHash>> blockHash)
        {
            var walletHolderService = ServiceFactory.Instance.Resolve<WalletHolderService>();
            if (!walletHolderService.TryGetPhantomPublicKey(out string phantomPublicKey))
            {
                return null;
            }

            Transaction transaction = new Transaction();
            transaction.Instructions = new List<TransactionInstruction>();
            
            var transactionInstruction = SystemProgram.Transfer(new PublicKey(phantomPublicKey),
                new PublicKey(toPublicKey), SolanaUtils.SolToLamports / 10);
            
            transaction.Instructions.Add(transactionInstruction);
            transaction.FeePayer = new PublicKey(phantomPublicKey);
            transaction.RecentBlockHash = blockHash.Result.Value.Blockhash;
            transaction.Signatures = new List<SignaturePubKeyPair>();
            return transaction;
        }
    }
    public class SolBalanceChangedMessage
    {
    }
}