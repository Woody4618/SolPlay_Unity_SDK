using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Frictionless;
using Solana.Unity.Programs;
using Solana.Unity.Programs.Models;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using SolPlay.Deeplinks;
using SolPlay.DeeplinksNftExample.Scripts.OrcaWhirlPool;
using UnityEngine;
using Whirlpool;
using Whirlpool.Accounts;
using Whirlpool.Program;
using Whirlpool.Types;

namespace SolPlay.DeeplinksNftExample.Scripts
{
    public class OrcaWhirlpoolService : MonoBehaviour
    {
        private WhirlpoolClient _whirlpoolClient;
        public static PublicKey WhirlpoolProgammId = new PublicKey("whirLbMiicVdio4qvUfM5KAg6Ct8VwpYzGff3uctyCc");
        public static PublicKey WhirlpoolConfigId = new PublicKey("2LecshUwdy9xi7meFgHtFJQNSKk4KdTrcpvaB56dP2NQ");

        const string MAX_SQRT_PRICE = "79226673515401279992447579055";
        const string MIN_SQRT_PRICE = "4295048016";

        private void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);
        }

        private void Start()
        {
            ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<WalletLoggedInMessage>(OnWalletLoggedInMessage);
        }

        private void OnWalletLoggedInMessage(WalletLoggedInMessage message)
        {
            Init();
        }

        private void Init()
        {
            var wallet = ServiceFactory.Instance.Resolve<WalletHolderService>().BaseWallet;
            _whirlpoolClient = new WhirlpoolClient(wallet.ActiveRpcClient, null, WhirlpoolProgammId);
        }

        public async Task<Whirlpool.Accounts.Whirlpool> GetPool(string poolPDA)
        {
            var whirlpoolsAsync =
                await _whirlpoolClient.GetWhirlpoolAsync(poolPDA);
            var pool = whirlpoolsAsync.ParsedResult;
            return pool;
        }
        
        public async Task<List<Whirlpool.Accounts.Whirlpool>> GetPools()
        {
            ProgramAccountsResultWrapper<List<Whirlpool.Accounts.Whirlpool>> whirlpoolsAsync =
                await _whirlpoolClient.GetWhirlpoolsAsync(WhirlpoolProgammId);
            List<Whirlpool.Accounts.Whirlpool> allPools = whirlpoolsAsync.ParsedResult;
            return allPools;
        }

        public async Task<string> InitializePool(WalletBase wallet)
        {
            RequestResult<ResponseValue<BlockHash>> blockHash = await wallet.ActiveRpcClient.GetRecentBlockHashAsync();

            Transaction swapOrcaTokenTransaction = new Transaction();
            swapOrcaTokenTransaction.FeePayer = wallet.Account.PublicKey;
            swapOrcaTokenTransaction.RecentBlockHash = blockHash.Result.Value.Blockhash;
            swapOrcaTokenTransaction.Signatures = new List<SignaturePubKeyPair>();
            swapOrcaTokenTransaction.Instructions = new List<TransactionInstruction>();

            var initializePoolAccounts = new InitializePoolAccounts();
            initializePoolAccounts.Funder = new PublicKey("");
            initializePoolAccounts.Rent = new PublicKey("");
            initializePoolAccounts.Whirlpool = new PublicKey("");
            initializePoolAccounts.FeeTier = new PublicKey("");
            initializePoolAccounts.SystemProgram = new PublicKey("");
            initializePoolAccounts.TokenProgram = new PublicKey("");
            initializePoolAccounts.WhirlpoolsConfig = new PublicKey("");
            initializePoolAccounts.TokenMintA = new PublicKey("");
            initializePoolAccounts.TokenMintB = new PublicKey("");
            initializePoolAccounts.TokenVaultA = new PublicKey("");
            initializePoolAccounts.TokenVaultB = new PublicKey("");

            WhirlpoolProgram.InitializePool(initializePoolAccounts, new WhirlpoolBumps(), UInt16.MinValue,
                BigInteger.One, WhirlpoolProgammId);

            var signedTransaction = await wallet.SignTransaction(swapOrcaTokenTransaction);
            var signature =
                await wallet.ActiveRpcClient.SendTransactionAsync(Convert.ToBase64String(signedTransaction.Serialize()), false,
                    Commitment.Confirmed);
            Debug.Log(signature.Result + signature.RawRpcResponse);

            return signature.Result;
        }

        public async Task<string> Swap(WalletBase wallet, Whirlpool.Accounts.Whirlpool pool, UInt64 amount,
            bool aToB = true)
        {
            RequestResult<ResponseValue<BlockHash>> blockHash = await wallet.ActiveRpcClient.GetRecentBlockHashAsync();

            var whirlPoolConfigResult = await _whirlpoolClient.GetWhirlpoolsConfigAsync(pool.WhirlpoolsConfig);
            WhirlpoolsConfig whirlPoolConfig = whirlPoolConfigResult.ParsedResult;

            PublicKey whirlPoolPda = OrcaPDAUtils.GetWhirlpoolPda(WhirlpoolProgammId, pool.WhirlpoolsConfig,
                pool.TokenMintA, pool.TokenMintB, pool.TickSpacing);

            var getWhilepool = await _whirlpoolClient.GetWhirlpoolAsync(whirlPoolPda);
            if (getWhilepool.ParsedResult == null)
            {
                ServiceFactory.Instance.Resolve<LoggingService>()
                    .LogWarning($"Could not load whirlpool {whirlPoolPda}", true);
                return null;
            }

            Debug.Log(getWhilepool.ParsedResult.TickSpacing);

            Transaction swapOrcaTokenTransaction = new Transaction();
            swapOrcaTokenTransaction.FeePayer = wallet.Account.PublicKey;
            swapOrcaTokenTransaction.RecentBlockHash = blockHash.Result.Value.Blockhash;
            swapOrcaTokenTransaction.Signatures = new List<SignaturePubKeyPair>();
            swapOrcaTokenTransaction.Instructions = new List<TransactionInstruction>();

            PublicKey tokenOwnerAccountA =
                AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(wallet.Account.PublicKey, pool.TokenMintA);
            PublicKey tokenOwnerAccountB =
                AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(wallet.Account.PublicKey, pool.TokenMintB);

            var accountInfoA = await wallet.ActiveRpcClient.GetAccountInfoAsync(tokenOwnerAccountA);
            var accountInfoB = await wallet.ActiveRpcClient.GetAccountInfoAsync(tokenOwnerAccountB);

            if (accountInfoA.Result.Value == null)
            {
                var associatedTokenAccountA = AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                    wallet.Account.PublicKey,
                    wallet.Account.PublicKey, pool.TokenMintA);
                swapOrcaTokenTransaction.Instructions.Add(associatedTokenAccountA);
            }

            if (accountInfoB.Result.Value == null)
            {
                var associatedTokenAccountB = AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                    wallet.Account.PublicKey,
                    wallet.Account.PublicKey, pool.TokenMintB);
                swapOrcaTokenTransaction.Instructions.Add(associatedTokenAccountB);
            }
    
            int startTickIndex = TickUtils.GetStartTickIndex(pool.TickCurrentIndex, pool.TickSpacing, 0);
            var swapAccountsTickArray0 = OrcaPDAUtils.GetTickArray(WhirlpoolProgammId, whirlPoolPda, startTickIndex);

            SwapAccounts swapAccounts = new SwapAccounts();
            swapAccounts.TokenProgram = TokenProgram.ProgramIdKey;
            swapAccounts.TokenAuthority = wallet.Account.PublicKey;
            swapAccounts.TokenOwnerAccountA = true ? tokenOwnerAccountA : tokenOwnerAccountB;
            swapAccounts.TokenVaultA = true ? pool.TokenVaultA : pool.TokenVaultB;
            swapAccounts.TokenVaultB = true ? pool.TokenVaultB : pool.TokenVaultA;
            swapAccounts.TokenOwnerAccountB = true ? tokenOwnerAccountB : tokenOwnerAccountA;
            swapAccounts.TickArray0 = swapAccountsTickArray0;
            swapAccounts.TickArray1 = swapAccounts.TickArray0;
            swapAccounts.TickArray2 = swapAccounts.TickArray0;
            swapAccounts.Whirlpool = whirlPoolPda;
            swapAccounts.Oracle = OrcaPDAUtils.GetOracle(WhirlpoolProgammId, whirlPoolPda);

            var srqtPrice = BigInteger.Parse(aToB ? MIN_SQRT_PRICE : MAX_SQRT_PRICE);
            TransactionInstruction swapInstruction = WhirlpoolProgram.Swap(swapAccounts, amount, 0, srqtPrice,
                true, aToB, WhirlpoolProgammId);
            
            swapOrcaTokenTransaction.Instructions.Add(swapInstruction);

            Transaction signedTransaction = await wallet.SignTransaction(swapOrcaTokenTransaction);
            var signature =
                await wallet.ActiveRpcClient.SendTransactionAsync(Convert.ToBase64String(signedTransaction.Serialize()), false,
                    Commitment.Confirmed);

            if (!signature.WasSuccessful)
            {
                ServiceFactory.Instance.Resolve<LoggingService>().LogWarning(signature.Reason, true);
            }

            return signature.Result;
        }

        /*
        var getMetaDataA = getMetadata(pool.TokenMintA);
        var getMetaDataB = getMetadata(pool.TokenMintB);

        var tokenInfoA = await wallet.ActiveRpcClient.GetTokenAccountInfoAsync(getMetaDataA);
        var tokenInfoB = await wallet.ActiveRpcClient.GetTokenAccountInfoAsync(getMetaDataA);

        var seeds = new List<byte[]>();
        seeds.Add(Encoding.UTF8.GetBytes("metadata"));
        seeds.Add(new PublicKey("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s").KeyBytes);
        seeds.Add(pool.TokenMintA.KeyBytes);

        PublicKey.TryFindProgramAddress(
            seeds, 
            new PublicKey("metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s"),
            out PublicKey metaplexDataPubKey, out var _bump);
        
        var metaDataAccountInfo = await wallet.ActiveRpcClient.GetAccountInfoAsync(metaplexDataPubKey, Commitment.Confirmed, BinaryEncoding.JsonParsed);

        byte[] message = Base64.Decode(metaDataAccountInfo.Result.Value.Data[0]);
        var serializeObject2 = Newtonsoft.Json.JsonConvert.SerializeObject(message);
        var utf8string = Encoding.UTF8.GetString(message);
        ObjectToByte.DecodeBase58StringFromByte(message, 0, message.Length, out string parsedjson);*/
    }
}