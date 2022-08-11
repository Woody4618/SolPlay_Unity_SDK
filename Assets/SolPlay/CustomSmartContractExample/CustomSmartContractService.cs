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
using UnityEngine;

namespace SolPlay.CustomSmartContractExample
{
    public class CustomSmartContractService : MonoBehaviour
    {
        public int CurrentPlayerLevel = 0;

        PublicKey HelloWorldProgramPublicKey = new PublicKey("F3qQ9mJep9hwCkJRtRSUcxov5etdRvQU9NBFpPjh4LKo");
        string AccountSeed = "HelloWorld";

        public void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);
        }

        public async Task<AccountInfo> RefreshLevelAccountData()
        {
            var wallet = ServiceFactory.Instance.Resolve<WalletHolderService>().BaseWallet;
            
            if (!GetProgramDerivedAccount(wallet.Account.PublicKey, AccountSeed, out var programAccountPublicKey))
                return null;

            RequestResult<ResponseValue<AccountInfo>> accountInfoResult =
                await wallet.ActiveRpcClient.GetAccountInfoAsync(programAccountPublicKey);

            if (accountInfoResult != null && accountInfoResult.Result != null && accountInfoResult.Result.Value != null)
            {
                foreach (var entry in accountInfoResult.Result.Value.Data)
                {
                    try
                    {
                        byte[] message = Base64.Decode(entry);
                        var uInt32 = BitConverter.ToUInt32(message);
                        Debug.Log("Player level: " + uInt32);
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
            double sol = await wallet.GetBalance();

            Debug.Log($"Pubkey: {wallet.Account.PublicKey} - SolAmount = " + sol);

            if (sol == 0)
            {
                var result = await wallet.RequestAirdrop(1000000000);
                Debug.Log($"Request airdrop: {result} Are you connected to the internet or on mainnet?");
            }

            var blockHash = await wallet.ActiveRpcClient.GetRecentBlockHashAsync();

            if (blockHash.Result == null)
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage("Block hash null. Connected to internet?"));
                return;
            }

            var levelAccount = await RefreshLevelAccountData();

            blockHash = await wallet.ActiveRpcClient.GetRecentBlockHashAsync();

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
                    4,
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

            Debug.Log("Sending transaction using: " + walletHolderService.BaseWallet.GetType());
            var signedTransaction = await walletHolderService.BaseWallet.SignAndSendTransaction(increasePlayerLevelTransaction);

            Debug.Log("Signed and send: " + signedTransaction + " checking signature now");

            ServiceFactory.Instance.Resolve<PhantomDeeplinkService>().CheckSignatureStatus(signedTransaction.Result);
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
            RequestResult<ResponseValue<AccountInfo>> programmAccountInfo =
                await activeRpcClient.GetAccountInfoAsync(HelloWorldProgramPublicKey);

            if (programmAccountInfo.Result != null && programmAccountInfo.Result.Value != null)
            {
                Debug.Log("Program is available and executable: " + programmAccountInfo.Result.Value.Executable);
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