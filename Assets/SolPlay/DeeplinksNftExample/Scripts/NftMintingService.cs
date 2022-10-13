using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Frictionless;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using Solnet.Metaplex;
using SolPlay.Deeplinks;
using UnityEngine;

namespace SolPlay.DeeplinksNftExample.Scripts
{
    public class NftMintingService : MonoBehaviour
    {
        public void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);
        }

        public async Task<string> MintNftWithMetaData(string metaDataUri, string name, string symbol)
        {
            var walletHolderService = ServiceFactory.Instance.Resolve<WalletHolderService>();
            var wallet = walletHolderService.BaseWallet;
            var rpcClient = walletHolderService.BaseWallet.ActiveRpcClient;

            Account mintAccount = new Account();
            Account tokenAccount = new Account();

            var fromAccount = walletHolderService.BaseWallet.Account;
            //if (fromAccount.PrivateKey == null)
            //{
                fromAccount = new Account(new Account().PrivateKey.KeyBytes,
                    walletHolderService.BaseWallet.Account.PublicKey.KeyBytes);
            //}

            RequestResult<ResponseValue<ulong>> balance = await rpcClient.GetBalanceAsync(wallet.Account.PublicKey);
            
            Debug.Log($"Balance: {balance.Result.Value} ");
            Debug.Log($"Mint key : {mintAccount.PublicKey} ");

            var blockHash = await rpcClient.GetRecentBlockHashAsync();
            var rentMint = await  rpcClient.GetMinimumBalanceForRentExemptionAsync(
                TokenProgram.MintAccountDataSize,
                Commitment.Confirmed
            );
            var rentToken = await rpcClient.GetMinimumBalanceForRentExemptionAsync(
                TokenProgram.TokenAccountDataSize,
                Commitment.Confirmed
            );

            Debug.Log($"Token key : {tokenAccount.PublicKey} ");

            //2. create a mint and a token
            var createMintAccount = SystemProgram.CreateAccount(
                fromAccount,
                mintAccount,
                rentMint.Result,
                TokenProgram.MintAccountDataSize,
                TokenProgram.ProgramIdKey
            );
            var initializeMint = TokenProgram.InitializeMint(
                mintAccount.PublicKey,
                0,
                fromAccount.PublicKey
            );
            var createTokenAccount = SystemProgram.CreateAccount(
                fromAccount,
                tokenAccount,
                rentToken.Result,
                TokenProgram.TokenAccountDataSize,
                TokenProgram.ProgramIdKey
            );
            var initializeMintAccount = TokenProgram.InitializeAccount(
                tokenAccount.PublicKey,
                mintAccount.PublicKey,
                fromAccount.PublicKey
            );
            var mintTo = TokenProgram.MintTo(
                mintAccount.PublicKey,
                tokenAccount,
                1,
                fromAccount.PublicKey
            );

            // PDA METADATA
            PublicKey metadataAddressPDA;
            byte nonce;
            PublicKey.TryFindProgramAddress(
                new List<byte[]>() {
                    Encoding.UTF8.GetBytes("metadata"),
                    MetadataProgram.ProgramIdKey,
                    mintAccount.PublicKey
                },
                MetadataProgram.ProgramIdKey,
                out metadataAddressPDA,
                out nonce
            );

            Console.WriteLine($"PDA METADATA: { metadataAddressPDA}");

            // PDA MASTER EDITION
            PublicKey masterEditionAddress;

            PublicKey.TryFindProgramAddress(
                new List<byte[]>() {
                    Encoding.UTF8.GetBytes("metadata"),
                    MetadataProgram.ProgramIdKey,
                    mintAccount.PublicKey,
                    Encoding.UTF8.GetBytes("edition")
                },
                MetadataProgram.ProgramIdKey,
                out masterEditionAddress,
                out nonce
            );
            Console.WriteLine($"PDA MASTER: { masterEditionAddress }");
            
            // CREATORS
            var creator1 = new Creator( fromAccount.PublicKey, 100);

            // DATA
            var data = new MetadataV1()
            {
                name = name,
                symbol = symbol,
                uri = metaDataUri,
                creators = new List<Creator>() { creator1 } ,
                sellerFeeBasisPoints = 77,
            };

            var signers = new List<Account> {fromAccount, mintAccount, tokenAccount};
            byte[] transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(fromAccount)
                .AddInstruction(createMintAccount)
                .AddInstruction(initializeMint)
                .AddInstruction(createTokenAccount)
                .AddInstruction(initializeMintAccount)
                .AddInstruction(mintTo) 
                .AddInstruction(
                    MetadataProgram.CreateMetadataAccount(
                        metadataAddressPDA,                    // PDA
                        mintAccount,               
                        fromAccount.PublicKey,    
                        fromAccount.PublicKey,      
                        fromAccount.PublicKey, // update Authority 
                        data,                               // DATA
                        true,
                        true                        // ISMUTABLE
                    )
                )
                .AddInstruction(
                    MetadataProgram.SignMetada(
                        metadataAddressPDA,
                        creator1.key
                    )
                )
                .AddInstruction(
                    MetadataProgram.PuffMetada(
                        metadataAddressPDA
                    )
                )
                .AddInstruction(
                    MetadataProgram.CreateMasterEdition(
                        1,
                        masterEditionAddress,
                        mintAccount,
                        fromAccount.PublicKey,
                        fromAccount.PublicKey,
                        fromAccount.PublicKey,
                        metadataAddressPDA
                    )
                )
                .Build(signers);

            Transaction deserializedTransaction = Transaction.Deserialize(transaction);
            
            Console.WriteLine($"TX1.Length {transaction.Length}");
            
            var signedTransaction = await walletHolderService.BaseWallet.SignTransaction(deserializedTransaction);

            // This is a bit hacky, but in case of phantom wallet we need to replace the signature with the one that 
            // phantom produces
            signedTransaction.Signatures[0] = signedTransaction.Signatures[3];
            signedTransaction.Signatures.RemoveAt(3);
            var transactionSignature =
                await walletHolderService.BaseWallet.ActiveRpcClient.SendTransactionAsync(
                    Convert.ToBase64String(signedTransaction.Serialize()), false, Commitment.Confirmed);
            
            Debug.Log(transactionSignature.Reason);
            return transactionSignature.Result;
        }

        public async void MintNft()
        {
            var walletHolderService = ServiceFactory.Instance.Resolve<WalletHolderService>();
            var wallet = walletHolderService.BaseWallet;
            var rpcClient = walletHolderService.BaseWallet.ActiveRpcClient;

            Account mintAccount = new Account();
            Account tokenAccount = new Account();

            var fromAccount = walletHolderService.BaseWallet.Account;

            RequestResult<ResponseValue<ulong>> balance = await rpcClient.GetBalanceAsync(wallet.Account.PublicKey);
            
            Debug.Log($"Balance: {balance.Result.Value} ");
            Debug.Log($"Mint key : {mintAccount.PublicKey} ");

            var blockHash = await rpcClient.GetRecentBlockHashAsync();
            var rentMint = await  rpcClient.GetMinimumBalanceForRentExemptionAsync(
                TokenProgram.MintAccountDataSize,
                Commitment.Confirmed
            );
            var rentToken = await rpcClient.GetMinimumBalanceForRentExemptionAsync(
                TokenProgram.TokenAccountDataSize,
                Commitment.Confirmed
            );

            Debug.Log($"Token key : {tokenAccount.PublicKey} ");

            //2. create a mint and a token
            var createMintAccount = SystemProgram.CreateAccount(
                fromAccount,
                mintAccount,
                rentMint.Result,
                TokenProgram.MintAccountDataSize,
                TokenProgram.ProgramIdKey
            );
            var initializeMint = TokenProgram.InitializeMint(
                mintAccount.PublicKey,
                0,
                fromAccount.PublicKey
            );
            var createTokenAccount = SystemProgram.CreateAccount(
                fromAccount,
                tokenAccount,
                rentToken.Result,
                TokenProgram.TokenAccountDataSize,
                TokenProgram.ProgramIdKey
            );
            var initializeMintAccount = TokenProgram.InitializeAccount(
                tokenAccount.PublicKey,
                mintAccount.PublicKey,
                fromAccount.PublicKey
            );
            var mintTo = TokenProgram.MintTo(
                mintAccount.PublicKey,
                tokenAccount,
                1,
                fromAccount.PublicKey
            );

            byte[] transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(fromAccount)
                .AddInstruction(createMintAccount) // create
                .AddInstruction(initializeMint) // initMint
                .AddInstruction(createTokenAccount) // createaccount
                .AddInstruction(initializeMintAccount) // initAccount
                .AddInstruction(mintTo) // mintTo
                //.AddInstruction(instr6) // Create Metadata
                .Build(new List<Account> {fromAccount, mintAccount, tokenAccount});

            Console.WriteLine($"TX1.Length {transaction.Length}");

            var transactionSignature =
                await walletHolderService.BaseWallet.ActiveRpcClient.SendTransactionAsync(
                    Convert.ToBase64String(transaction), false, Commitment.Confirmed);

            Debug.Log(transactionSignature.Reason);

            AddMetaDataToMint(mintAccount, "https://metadata.y00ts.com/t/10225.json");
        }

        public async void AddMetaDataToMint(PublicKey mint, string metaDataUri)
        {
            var walletHolderService = ServiceFactory.Instance.Resolve<WalletHolderService>();
            var wallet = walletHolderService.BaseWallet;
            var rpcClient = walletHolderService.BaseWallet.ActiveRpcClient;

            var fromAccount = wallet.Account;
            var mintAccount = mint;

            var blockHash = await rpcClient.GetRecentBlockHashAsync();

            // PDA METADATA
            PublicKey metadataAddress;
            byte nonce;
            PublicKey.TryFindProgramAddress(
                new List<byte[]>() {
                    Encoding.UTF8.GetBytes("metadata"),
                    MetadataProgram.ProgramIdKey,
                    mintAccount
                },
                MetadataProgram.ProgramIdKey,
                out metadataAddress,
                out nonce
            );

            Console.WriteLine($"PDA METADATA: { metadataAddress}");

            // PDA MASTER EDITION
            PublicKey masterEditionAddress;

            PublicKey.TryFindProgramAddress(
                new List<byte[]>() {
                    Encoding.UTF8.GetBytes("metadata"),
                    MetadataProgram.ProgramIdKey,
                    mintAccount,
                    Encoding.UTF8.GetBytes("edition")
                },
                MetadataProgram.ProgramIdKey,
                out masterEditionAddress,
                out nonce
            );
            Console.WriteLine($"PDA MASTER: { masterEditionAddress }");
            
            // CREATORS
            var creator1 = new Creator( fromAccount.PublicKey, 100);

            // DATA
            var data = new MetadataV1()
            {
                name = "Super NFT",
                symbol = "SolPlay",
                uri = metaDataUri,
                creators = new List<Creator>() { creator1 } ,
                sellerFeeBasisPoints = 77,
            };

            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(fromAccount.PublicKey)
                .AddInstruction(
                    MetadataProgram.CreateMetadataAccount(
                        metadataAddress,                    // PDA
                        mintAccount,                        // MINT
                        fromAccount.PublicKey,    // mint AUTHORITY
                        fromAccount.PublicKey,      // PAYER
                        fromAccount.PublicKey, // update Authority 
                        data,                               // DATA
                        true,
                        true                        // ISMUTABLE
                    )
                )
                 .AddInstruction(
                     MetadataProgram.SignMetada(
                         metadataAddress,
                         creator1.key
                     )
                 )
                .AddInstruction(
                    MetadataProgram.PuffMetada(
                        metadataAddress
                    )
                )
                .AddInstruction(
                    MetadataProgram.CreateMasterEdition(
                        1,
                        masterEditionAddress,
                        mintAccount,
                        fromAccount.PublicKey,
                        fromAccount.PublicKey,
                        fromAccount.PublicKey,
                        metadataAddress
                    )
                )
            .Build(new List<Account> { fromAccount });

            Console.WriteLine($"TX2.Length {transaction.Length}");

            var txSim2 = await rpcClient.SimulateTransactionAsync(transaction);

            var transactionSignature =
                await walletHolderService.BaseWallet.ActiveRpcClient.SendTransactionAsync(
                    Convert.ToBase64String(transaction), false, Commitment.Confirmed);

            Debug.Log(transactionSignature.Reason);
        }
    }
}