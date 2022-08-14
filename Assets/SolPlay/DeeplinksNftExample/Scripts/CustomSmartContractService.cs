using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Frictionless;
using Merkator.BitCoin;
using Org.BouncyCastle.Utilities.Encoders;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using SolPlay.Deeplinks;
using SolPlay.DeeplinksNftExample.Utils;
using UnityEngine;

namespace SolPlay.CustomSmartContractExample
{
    public class HelloWorldAccount
    {
        public UInt32 CurrentPlayerLevel = 0;

        public long GetAccountSize()
        {
            return sizeof(UInt32);
        }
    }

    public class CustomSmartContractService : MonoBehaviour
    {
        public int CurrentPlayerLevel = 0;
        public HelloWorldAccount HelloWorldAccount = new HelloWorldAccount();

        PublicKey HelloWorldProgramPublicKey = new PublicKey("F3qQ9mJep9hwCkJRtRSUcxov5etdRvQU9NBFpPjh4LKo");
        string AccountSeed = "HelloWorld";

        public void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);
        }

        public async Task<AccountInfo> GetHelloWorldAccountData()
        {
            var wallet = ServiceFactory.Instance.Resolve<WalletHolderService>().BaseWallet;

            if (!GetProgramDerivedAccount(wallet.Account.PublicKey, AccountSeed, out var programAccountPublicKey))
                return null;

            ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new BlimpSystem.ShowBlimpMessage("Request player level."));

            RequestResult<ResponseValue<AccountInfo>> accountInfoResult =
                await wallet.ActiveRpcClient.GetAccountInfoAsync(programAccountPublicKey);

            if (accountInfoResult != null && accountInfoResult.Result != null && accountInfoResult.Result.Value != null)
            {
                foreach (var entry in accountInfoResult.Result.Value.Data)
                {
                    try
                    {
                        byte[] message = Base64.Decode(entry);
                        uint uInt32 = BitConverter.ToUInt32(message);
                        var playerLevel = "Player level recieved: " + uInt32;
                        Debug.Log(playerLevel);
                        ServiceFactory.Instance.Resolve<MessageRouter>()
                            .RaiseMessage(new BlimpSystem.ShowBlimpMessage(playerLevel));

                        CurrentPlayerLevel = (int) uInt32;
                    }
                    catch (Exception e)
                    {
                        // Wasn't a base 64 string 
                    }
                }

                return accountInfoResult.Result.Value;
            }

            return null;
        }

        public async Task IncreasePlayerLevel()
        {
            var wallet = ServiceFactory.Instance.Resolve<WalletHolderService>().BaseWallet;
            double sol = await wallet.GetBalance() * SolanaUtils.SolToLamports;

            var levelAccount = await GetHelloWorldAccountData();

            var blockHash = await wallet.ActiveRpcClient.GetRecentBlockHashAsync();

            if (blockHash.Result == null)
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage("Block hash null. Connected to internet?"));
                return;
            }

            ulong fees = blockHash.Result.Value.FeeCalculator.LamportsPerSignature * 100;
            if (levelAccount == null)
            {
                var accountDataSize = HelloWorldAccount.GetAccountSize();
                RequestResult<ulong> costPerAccount =
                    await wallet.ActiveRpcClient.GetMinimumBalanceForRentExemptionAsync(accountDataSize);
                fees += costPerAccount.Result;
            }

            Debug.Log($"Pubkey: {wallet.Account.PublicKey} - SolAmount = " + sol + " needed for account: " + fees);

            if (sol <= fees)
            {
                string result = await wallet.RequestAirdrop(1000000000);

                Debug.Log($"Request airdrop: {result} Are you connected to the internet or on mainnet?");
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage("You dont have enough sol. Maybe try on dev net."));
                return;
            }

            await CreateAndSendUnsignedHelloWorldTransaction(blockHash.Result.Value, levelAccount == null);
        }

        private async Task CreateAndSendUnsignedHelloWorldTransaction(BlockHash blockHash, bool createAccount)
        {
            var walletHolderService = ServiceFactory.Instance.Resolve<WalletHolderService>();
            var localPublicKey = walletHolderService.BaseWallet.Account.PublicKey;
            var activeRpcClient = walletHolderService.BaseWallet.ActiveRpcClient;

            if (!await CheckIfProgramIsDeployed(activeRpcClient)) return;
            if (!GetProgramDerivedAccount(localPublicKey, AccountSeed, out var programAccountPublicKey)) return;

            Transaction increasePlayerLevelTransaction = new Transaction();
            increasePlayerLevelTransaction.FeePayer = localPublicKey;
            increasePlayerLevelTransaction.RecentBlockHash = blockHash.Blockhash;
            increasePlayerLevelTransaction.Signatures = new List<SignaturePubKeyPair>();

            increasePlayerLevelTransaction.Instructions = new List<TransactionInstruction>();

            if (createAccount)
            {
                var costToCreateTheAccount = await activeRpcClient.GetMinimumBalanceForRentExemptionAsync(4);

                TransactionInstruction createAccountInstruction = SystemProgram.CreateAccountWithSeed(
                    localPublicKey,
                    programAccountPublicKey,
                    localPublicKey,
                    AccountSeed,
                    costToCreateTheAccount.Result,
                    (ulong) new HelloWorldAccount().GetAccountSize(),
                    HelloWorldProgramPublicKey);

                increasePlayerLevelTransaction.Instructions.Add(createAccountInstruction);
            }

            List<AccountMeta> accountMetaList = new List<AccountMeta>()
            {
                AccountMeta.Writable(programAccountPublicKey, false),
                AccountMeta.ReadOnly(localPublicKey, true)
            };

            TransactionInstruction helloWorldTransactionInstruction = new TransactionInstruction()
            {
                ProgramId = Base58Encoding.Decode("F3qQ9mJep9hwCkJRtRSUcxov5etdRvQU9NBFpPjh4LKo"),
                Keys = (IList<AccountMeta>) accountMetaList,
                Data = Array.Empty<byte>()
            };

            increasePlayerLevelTransaction.Instructions.Add(helloWorldTransactionInstruction);

            var sendingTransactionUsing = "Sending transaction using: " + walletHolderService.BaseWallet.GetType();
            Debug.Log(sendingTransactionUsing);
            ServiceFactory.Instance.Resolve<MessageRouter>()
                .RaiseMessage(new BlimpSystem.ShowBlimpMessage(sendingTransactionUsing));

            var signedTransaction =
                await walletHolderService.BaseWallet.SignAndSendTransaction(increasePlayerLevelTransaction);

            var checkingSignatureNow = "Signed and sent: " + signedTransaction + " checking signature now";
            Debug.Log(checkingSignatureNow);
            ServiceFactory.Instance.Resolve<MessageRouter>()
                .RaiseMessage(new BlimpSystem.ShowBlimpMessage(checkingSignatureNow));

            CheckSignature(signedTransaction.Result, () =>
            {
                GetHelloWorldAccountData();
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new SolBalanceChangedMessage());
            });
        }

        private void CheckSignature(string signature, Action onSignatureFinalized)
        {
            ServiceFactory.Instance.Resolve<PhantomDeeplinkService>()
                .CheckSignatureStatus(signature, onSignatureFinalized);
        }

        private bool GetProgramDerivedAccount(PublicKey localPublicKey,
            string accountSeed, out PublicKey programAccountPublicKey)
        {
            if (!PublicKey.TryCreateWithSeed(
                    localPublicKey,
                    accountSeed,
                    HelloWorldProgramPublicKey,
                    out programAccountPublicKey))
            {
                Debug.LogError($"Could not create hello world programm account key");
                return false;
            }

            return true;
        }

        private async Task<bool> CheckIfProgramIsDeployed(IRpcClient activeRpcClient)
        {
            RequestResult<ResponseValue<AccountInfo>> programAccountInfo =
                await activeRpcClient.GetAccountInfoAsync(HelloWorldProgramPublicKey);

            if (programAccountInfo.Result != null && programAccountInfo.Result.Value != null)
            {
                Debug.Log("Program is available and executable: " + programAccountInfo.Result.Value.Executable);
            }
            else
            {
                Debug.Log("Program probably not deployed: ");
                return false;
            }

            return true;
        }
    }
}