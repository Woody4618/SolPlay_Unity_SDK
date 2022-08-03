using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using base58;
using Frictionless;
using Org.BouncyCastle.Utilities.Encoders;
using Solana.Unity.DeeplinkWallet;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using Solana.Unity.SDK.Example;
using Solana.Unity.Wallet;
using SolPlay.Deeplinks;
using UnityEngine;
using WebSocketSharp;

namespace SolPlay.CustomSmartContractExample
{
    public class CustomSmartContractService : MonoBehaviour
    {
        
        private async void Start()
        {
            string menemon = "gym basket dizzy chest pact rubber canvas staff around shadow brain purchase hello parent digital degree window version still rather measure brass lock arrest";
            //GenerateNewMnemonic();
            SimpleWallet.instance.wallet =  SimpleWallet.instance.GenerateWalletWithMenmonic(menemon);
            MainThreadDispatcher.Instance().Enqueue(() => { SimpleWallet.instance.StartWebSocketConnection(); }); 
            

            double sol = await SimpleWallet.instance.GetSolAmmount(SimpleWallet.instance.wallet.GetAccount(0));
            Debug.Log("SolAmount = " + sol);

            if (sol == 0)
            {
                var result = await SimpleWallet.instance.RequestAirdrop(SimpleWallet.instance.wallet.GetAccount(0));
            }
            
            var blockHash = await SimpleWallet.instance.activeRpcClient.GetRecentBlockHashAsync();

            if (blockHash.Result == null)
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage("Block hash null. Connected to internet?"));
                return;
            }

            CreateUnsignedHelloWorldTransaction(blockHash.Result.Value);
        }
        
        private async void CreateUnsignedHelloWorldTransaction(BlockHash blockHash)
        {
            var localPublicKey = SimpleWallet.instance.wallet.GetAccount(0).PublicKey;

            string Account_Seed = "HelloWorld";

            bool createdAccount = PublicKey.TryCreateWithSeed(
                localPublicKey,
                Account_Seed,
                new PublicKey("F3qQ9mJep9hwCkJRtRSUcxov5etdRvQU9NBFpPjh4LKo"),
                out PublicKey tokenDerivedAcressFromSeed);

            var garblesRpcClient = SimpleWallet.instance.activeRpcClient;
            RequestResult<ResponseValue<AccountInfo>> accountInfo = await garblesRpcClient.GetAccountInfoAsync(tokenDerivedAcressFromSeed);
            RequestResult<ResponseValue<AccountInfo>> programmAccountInfo = await garblesRpcClient.GetAccountInfoAsync(localPublicKey);

            if (programmAccountInfo.Result != null)
            {
                Debug.Log("Progrmm is available and executable: " + programmAccountInfo.Result.Value.Executable);
            }
            else
            {
                Debug.Log("Programm probably not deployed: ");
                return;
            }
            
            var token = await garblesRpcClient.GetAccountInfoAsync(tokenDerivedAcressFromSeed);

            var lamports = await garblesRpcClient.GetMinimumBalanceForRentExemptionAsync(4);
            
            Transaction createAccountTransaction = new Transaction();
            createAccountTransaction.Instructions = new List<TransactionInstruction>();
            if (token.Result.Value == null)
            {
                TransactionInstruction createAccountInstruction = SystemProgram.CreateAccountWithSeed(
                    localPublicKey,
                    tokenDerivedAcressFromSeed,
                    localPublicKey,
                    Account_Seed,
                    lamports.Result,
                    4,
                    new PublicKey("F3qQ9mJep9hwCkJRtRSUcxov5etdRvQU9NBFpPjh4LKo"));
                
                createAccountTransaction.Instructions.Add(createAccountInstruction);
            }
            else
            {
                foreach (var entry in accountInfo.Result.Value.Data)
                {
                    if (!entry.IsNullOrEmpty() && entry != "Base64")
                    {
                        try
                        {
                            var message = Base64.Decode(entry);
                            Debug.Log("Player level: " + BitConverter.ToUInt32(message));
                        }
                        catch (Exception e)
                        {
                            // Wasnt a base 64 string 
                        }
                    }
                }
            }
            
            createAccountTransaction.FeePayer = localPublicKey;
            createAccountTransaction.RecentBlockHash = blockHash.Blockhash;

            List<AccountMeta> accountMetaList = new List<AccountMeta>()
            {
                AccountMeta.Writable(tokenDerivedAcressFromSeed, false),
                AccountMeta.ReadOnly(localPublicKey, true)
            };
            
            TransactionInstruction helloWorldTransactionInstruction = new TransactionInstruction()
            {
                ProgramId = Base58.Decode("F3qQ9mJep9hwCkJRtRSUcxov5etdRvQU9NBFpPjh4LKo"),
                Keys = (IList<AccountMeta>) accountMetaList,
                Data = Array.Empty<byte>()
            };
            createAccountTransaction.Instructions.Add(helloWorldTransactionInstruction);
            
            
            var result = createAccountTransaction.Build(SimpleWallet.instance.wallet.Account);
            //var signedTransaction = SimpleWallet.instance.wallet.Sign(createAccountTransaction.Serialize());
            RequestResult<string> requestResult = await SimpleWallet.instance.activeRpcClient.SendTransactionAsync(result);
            Debug.Log(requestResult.Result);
        }
    }
}