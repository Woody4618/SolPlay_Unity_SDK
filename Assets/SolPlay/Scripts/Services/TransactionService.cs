using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Frictionless;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using SolPlay.Scripts.Ui;
using UnityEngine;
using SystemProgram = Solana.Unity.Programs.SystemProgram;

namespace SolPlay.Scripts.Services
{
    /// <summary>
    /// Establishes a secure connection with phantom wallet and gets the public key from the wallet. 
    /// </summary>
    public class TransactionService : MonoBehaviour, IMultiSceneSingleton
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
            if (ServiceFactory.Resolve<TransactionService>() != null)
            {
                Destroy(gameObject);
                return;
            }

            ServiceFactory.RegisterSingleton(this);
        }

        /// <summary>
        /// await Task.Delay() does not work properly on webgl so we use a coroutine instead
        /// </summary>
        public void CheckSignatureStatus(string signature, Action onSignatureFinalized,
            TransactionResult transactionResult = TransactionResult.confirmed)
        {
            if (string.IsNullOrEmpty(signature))
            {
                MessageRouter.RaiseMessage(
                    new BlimpSystem.ShowBlimpMessage($"Signature was empty: {signature}."));
            }
            else
            {
                StartCoroutine(CheckSignatureStatusRoutine(signature, onSignatureFinalized, transactionResult));
            }
        }

        private IEnumerator CheckSignatureStatusRoutine(string signature, Action onSignatureFinalized,
            TransactionResult transactionResult = TransactionResult.confirmed)
        {
            var wallet = ServiceFactory.Resolve<WalletHolderService>().BaseWallet;

            bool transactionFinalized = false;

            int counter = 0;
            int maxTries = 30;

            while (!transactionFinalized && counter < maxTries)
            {
                counter++;
                Task<RequestResult<ResponseValue<List<SignatureStatusInfo>>>> task =
                    wallet.ActiveRpcClient.GetSignatureStatusesAsync(new List<string>() {signature}, true);
                yield return new WaitUntil(() => task.IsCompleted);

                RequestResult<ResponseValue<List<SignatureStatusInfo>>> signatureResult = task.Result;

                if (signatureResult.Result == null)
                {
                    MessageRouter.RaiseMessage(
                        new BlimpSystem.ShowBlimpMessage($"There is no transaction for Signature: {signature}."));
                    yield return new WaitForSeconds(1.5f);
                    continue;
                }

                foreach (var signatureStatusInfo in signatureResult.Result.Value)
                {
                    if (signatureStatusInfo == null)
                    {
                        MessageRouter.RaiseMessage(
                            new BlimpSystem.ShowBlimpMessage(
                                $"Signature is not yet processed. Try: {counter}. Retry in 2 seconds."));
                    }
                    else
                    {
                        if (signatureStatusInfo.ConfirmationStatus ==
                            Enum.GetName(typeof(TransactionResult), transactionResult) ||
                            signatureStatusInfo.ConfirmationStatus == Enum.GetName(typeof(TransactionResult),
                                TransactionResult.finalized))
                        {
                            ServiceFactory.Resolve<WalletHolderService>().RefreshSolBalance();
                            MessageRouter.RaiseMessage(new BlimpSystem.ShowBlimpMessage("Transaction finalized"));
                            MessageRouter.RaiseMessage(new TokenValueChangedMessage());
                            transactionFinalized = true;
                            onSignatureFinalized();
                        }
                        else
                        {
                            MessageRouter.RaiseMessage(
                                new BlimpSystem.ShowBlimpMessage(
                                    $"Signature result {signatureStatusInfo.Confirmations}/31 status: {signatureStatusInfo.ConfirmationStatus} target: {Enum.GetName(typeof(TransactionResult), transactionResult)}"));
                        }
                    }
                }

                yield return new WaitForSeconds(1.5f);
            }

            if (counter >= maxTries)
            {
                MessageRouter.RaiseMessage(
                    new BlimpSystem.ShowBlimpMessage(
                        $"Tried {counter} times. The transaction probably failed :( "));
            }
        }

        public async Task<RequestResult<string>> TransferTokenToPubkey(PublicKey destination, PublicKey tokenMint,
            ulong amount, Commitment commitment = Commitment.Confirmed)
        {
            var wallHolderService = ServiceFactory.Resolve<WalletHolderService>();

            PublicKey localTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
                wallHolderService.BaseWallet.Account.PublicKey,
                tokenMint);
            PublicKey senderTokenAccount =
                AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(destination, tokenMint);
            var tokenAccounts =
                await wallHolderService.BaseWallet.ActiveRpcClient.GetTokenAccountsByOwnerAsync(destination, tokenMint);

            var blockHash = await wallHolderService.BaseWallet.ActiveRpcClient.GetRecentBlockHashAsync();

            var transaction = new Transaction
            {
                RecentBlockHash = blockHash.Result.Value.Blockhash,
                FeePayer = wallHolderService.BaseWallet.Account.PublicKey,
                Instructions = new List<TransactionInstruction>(),
                Signatures = new List<SignaturePubKeyPair>()
            };

            // We only need to create the token account for the destination because we cant send anything anyway if 
            // there is no account yet and we only create it if it does not exist yet.
            if (tokenAccounts.Result == null || tokenAccounts.Result.Value.Count == 0)
            {
                transaction.Instructions.Add(
                    AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                        wallHolderService.BaseWallet.Account.PublicKey,
                        destination,
                        tokenMint));
            }

            transaction.Instructions.Add(
                TokenProgram.Transfer(
                    localTokenAccount,
                    senderTokenAccount,
                    amount,
                    wallHolderService.BaseWallet.Account.PublicKey
                ));

            var result = await wallHolderService.BaseWallet.SignAndSendTransaction(transaction, commitment);

            CheckSignatureStatus(result.Result,
                () => { MessageRouter.RaiseMessage(new SolBalanceChangedMessage()); });
            MessageRouter.RaiseMessage(new TokenValueChangedMessage());
            return result;
        }

        public async Task<RequestResult<string>> TransferSolanaToPubkey(string toPublicKey, ulong lamports)
        {
            var wallet = ServiceFactory.Resolve<WalletHolderService>().BaseWallet;
            var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
            var blockHash = await wallet.ActiveRpcClient.GetRecentBlockHashAsync();

            if (blockHash.Result == null)
            {
                ServiceFactory.Resolve<LoggingService>().Log("Block hash null. Connected to internet?", true);
                return null;
            }

            var transferSolTransaction = CreateUnsignedTransferSolTransaction(toPublicKey, blockHash, lamports);

            RequestResult<string> requestResult =
                await walletHolderService.BaseWallet.SignAndSendTransaction(transferSolTransaction);

            CheckSignatureStatus(requestResult.Result,
                () => { MessageRouter.RaiseMessage(new SolBalanceChangedMessage()); });
            MessageRouter.RaiseMessage(new SolBalanceChangedMessage());
            return requestResult;
        }

        private Transaction CreateUnsignedTransferSolTransaction(string toPublicKey,
            RequestResult<ResponseValue<BlockHash>> blockHash, ulong lamports)
        {
            var walletHolderService = ServiceFactory.Resolve<WalletHolderService>();
            if (!walletHolderService.TryGetPhantomPublicKey(out string phantomPublicKey))
            {
                return null;
            }

            Transaction transaction = new Transaction();
            transaction.Instructions = new List<TransactionInstruction>();

            var transactionInstruction = SystemProgram.Transfer(new PublicKey(phantomPublicKey),
                new PublicKey(toPublicKey), lamports);

            transaction.Instructions.Add(transactionInstruction);
            transaction.FeePayer = new PublicKey(phantomPublicKey);
            transaction.RecentBlockHash = blockHash.Result.Value.Blockhash;
            transaction.Signatures = new List<SignaturePubKeyPair>();
            return transaction;
        }

        public IEnumerator HandleNewSceneLoaded()
        {
            yield return null;
        }
    }

    public class SolBalanceChangedMessage
    {
    }
}