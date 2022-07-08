using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AllArt.Solana.Nft;
using Frictionless;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solnet.Rpc;
using UnityEngine;

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// Handles all logic related to NFTs and calculating their power level or whatever you like to do with the NFTs
    /// </summary>
    public class NftService : MonoBehaviour
    {
        public List<Nft> MetaPlexNFts = new List<Nft>();
        public List<TokenAccount> TokenAccounts = new List<TokenAccount>();
        public string RpcUrl = "https://api.mainnet-beta.solana.com";
        public int NftImageSize = 75;
        public bool IsLoadingTokenAccounts { get; private set; }
        public const string BeaverNftMintAuthority = "GsfNSuZFrT2r4xzSndnCSs9tTXwt47etPqU8yFVnDcXd";

        public IRpcClient rpcClient;

        public void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);
            rpcClient = ClientFactory.GetClient(Cluster.MainNet);
        }

        public async Task RequestNftsFromPublicKey(string publicKey, bool tryUseLocalContent = true)
        {
            if (IsLoadingTokenAccounts)
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage("Loading in progress."));
                return;
            }

            ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new NftLoadingStartedMessage());

            IsLoadingTokenAccounts = true;

            var tokenAccounts = await GetOwnedTokenAccounts(publicKey);

            if (tokenAccounts == null)
            {
                string error = "Could not load Token Accounts, are you connected to the internet?";
                ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new BlimpSystem.ShowBlimpMessage(error));
                IsLoadingTokenAccounts = false;
                return;
            }

            MetaPlexNFts.Clear();

            string result = $"{tokenAccounts.Length} token accounts loaded. Getting data now.";
            ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new BlimpSystem.ShowBlimpMessage(result));

            foreach (var item in tokenAccounts)
            {
                if (float.Parse(item.Account.Data.Parsed.Info.TokenAmount.Amount) > 0)
                {
                    var solanaRpcClient = new SolanaRpcClient(RpcUrl);
                    Nft nft = await Nft.TryGetNftData(item.Account.Data.Parsed.Info.Mint, solanaRpcClient,
                        tryUseLocalContent);

                    if (nft != null)
                    {
                        MetaPlexNFts.Add(nft);
                        Debug.Log("NftName:" + nft.MetaplexData.data.name);
                        ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new NftArrivedMessage(nft));
                    }
                    else
                    {
                        TokenAccounts.Add(item);
                        ServiceFactory.Instance.Resolve<MessageRouter>()
                            .RaiseMessage(new TokenArrivedMessage(item.Account.Data.Parsed.Info));
                    }
                }
            }

            ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new NftLoadingFinishedMessage());
            IsLoadingTokenAccounts = false;
        }

        // TODO: Somehow not working, need to investigate 
        public async Task<TokenBalance> RequestTokenAccountBalance(string publicKey, string tokenAdress)
        {
            RequestResult<ResponseValue<List<TokenAccount>>> tokenAccountResult =
                await rpcClient.GetTokenAccountsByOwnerAsync(publicKey, tokenAdress, TokenProgram.ProgramIdKey);

            if (tokenAccountResult.WasSuccessful)
            {
                var result = await rpcClient.GetTokenAccountBalanceAsync(tokenAccountResult.Result.Value[0].PublicKey);

                if (result.Result != null && result.Result.Value != null)
                {
                    return result.Result.Value;
                }
            }

            return null;
        }

        private async Task<TokenAccount[]> GetOwnedTokenAccounts(string publicKey)
        {
            try
            {
                RequestResult<ResponseValue<List<TokenAccount>>> result =
                    await rpcClient.GetTokenAccountsByOwnerAsync(publicKey, null, TokenProgram.ProgramIdKey);

                if (result.Result != null && result.Result.Value != null)
                {
                    return result.Result.Value.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                IsLoadingTokenAccounts = false;
            }

            return null;
        }

        public bool OwnsNftOfMintAuthority(string authority)
        {
            foreach (var nft in MetaPlexNFts)
            {
                if (nft.MetaplexData.authority == authority)
                {
                    return true;
                }
            }

            return false;
        }

        public List<Nft> GetAllNftsByMintAuthority(string mintAuthority)
        {
            List<Nft> result = new List<Nft>();
            foreach (var nftData in MetaPlexNFts)
            {
                if (nftData.MetaplexData.authority != mintAuthority)
                {
                    continue;
                }

                result.Add(nftData);
            }

            return result;
        }

        public bool IsBeaverNft(Nft nft)
        {
            return nft.MetaplexData.authority == BeaverNftMintAuthority;
        }
    }

    public class NftArrivedMessage
    {
        public Nft NewNFt;

        public NftArrivedMessage(Nft newNFt)
        {
            NewNFt = newNFt;
        }
    }

    public class NftLoadingStartedMessage
    {
    }

    public class NftLoadingFinishedMessage
    {
    }

    public class TokenArrivedMessage
    {
        public TokenAccountInfoDetails TokenAccountInfoDetails;

        public TokenArrivedMessage(TokenAccountInfoDetails tokenAccountInfoDetails)
        {
            TokenAccountInfoDetails = tokenAccountInfoDetails;
        }
    }
}