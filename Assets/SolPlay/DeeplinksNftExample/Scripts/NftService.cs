using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Frictionless;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using Solana.Unity.SDK.Nft;
using SolPlay.CustomSmartContractExample;
using UnityEngine;

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// Handles all logic related to NFTs and calculating their power level or whatever you like to do with the NFTs
    /// </summary>
    public class NftService : MonoBehaviour
    {
        public List<SolPlayNft> MetaPlexNFts = new List<SolPlayNft>();
        public List<TokenAccount> TokenAccounts = new List<TokenAccount>();
        public int NftImageSize = 75;
        public bool IsLoadingTokenAccounts { get; private set; }
        public const string BeaverNftMintAuthority = "GsfNSuZFrT2r4xzSndnCSs9tTXwt47etPqU8yFVnDcXd";
        public SolPlayNft SelectedNft { get; private set; }
        public Texture2D LocalDummyNft;
        
        public void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);
        }

        public async Task RequestNftsFromPublicKey(string publicKey, bool tryUseLocalContent = true)
        {
            if (IsLoadingTokenAccounts)
            {
                ServiceFactory.Instance.Resolve<MessageRouter>()
                    .RaiseMessage(new BlimpSystem.ShowBlimpMessage("Loading in progress."));
                return;
            }

            var wallet = ServiceFactory.Instance.Resolve<WalletHolderService>().BaseWallet;

            ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new NftLoadingStartedMessage());

            IsLoadingTokenAccounts = true;

            TokenAccount[] tokenAccounts = await GetOwnedTokenAccounts(publicKey);

            if (tokenAccounts == null)
            {
                string error = "Could not load Token Accounts, are you connected to the internet?";
                ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new BlimpSystem.ShowBlimpMessage(error));
                IsLoadingTokenAccounts = false;
                return;
            }

            MetaPlexNFts.Clear();

            var dummyLocalNft = CreateDummyLocalNft(wallet);
            
            MetaPlexNFts.Add(dummyLocalNft);
            ServiceFactory.Instance.Resolve<MessageRouter>()
                .RaiseMessage(new NftArrivedMessage(dummyLocalNft));
            
            string result = $"{tokenAccounts.Length} token accounts loaded. Getting data now.";
            ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new BlimpSystem.ShowBlimpMessage(result));

            foreach (TokenAccount item in tokenAccounts)
            {
                if (float.Parse(item.Account.Data.Parsed.Info.TokenAmount.Amount) > 0)
                {
                    SolPlayNft solPlayNft = await SolPlayNft.TryGetNftData(item.Account.Data.Parsed.Info.Mint,
                        wallet.ActiveRpcClient, tryUseLocalContent);

                    if (solPlayNft != null)
                    {
                        solPlayNft.TokenAccount = item;
                        MetaPlexNFts.Add(solPlayNft);
                        ServiceFactory.Instance.Resolve<MessageRouter>()
                            .RaiseMessage(new NftArrivedMessage(solPlayNft));
                    }
                    else
                    {
                        TokenAccounts.Add(item);
                        ServiceFactory.Instance.Resolve<MessageRouter>()
                            .RaiseMessage(new TokenArrivedMessage(item.Account.Data.Parsed.Info));
                    }
                }
            }

            foreach (var nft in MetaPlexNFts)
            {
                var lastSelectedNft = GetSelectedNftPubKey();
                if (!string.IsNullOrEmpty(lastSelectedNft) && lastSelectedNft == nft.TokenAccount.PublicKey)
                {
                    SelectNft(nft);
                }
            }

            ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new NftLoadingFinishedMessage());
            IsLoadingTokenAccounts = false;
        }

        private SolPlayNft CreateDummyLocalNft(WalletBase wallet)
        {
            SolPlayNft dummyLocalNft = new SolPlayNft();
            dummyLocalNft.TokenAccount = new TokenAccount();
            dummyLocalNft.TokenAccount.PublicKey = wallet.Account.PublicKey;
            dummyLocalNft.MetaplexData = new Metaplex();
            dummyLocalNft.MetaplexData.nftImage = new NftImage()
            {
                name = "DummyNft",
                file = LocalDummyNft
            };
            dummyLocalNft.MetaplexData.mint = wallet.Account.PublicKey;
            dummyLocalNft.MetaplexData.data = new MetaplexData();
            dummyLocalNft.MetaplexData.data.symbol = "dummy";
            dummyLocalNft.MetaplexData.data.name = "Dummy Nft";
            dummyLocalNft.MetaplexData.data.json = new MetaplexJsonData();
            dummyLocalNft.MetaplexData.data.json.name = "Dummy nft";
            dummyLocalNft.MetaplexData.data.json.description = "A dummy nft which uses the wallet puy key";
            return dummyLocalNft;
        }

        public bool IsNftSelected(SolPlayNft nft)
        {
            return nft.TokenAccount?.PublicKey == GetSelectedNftPubKey();
        }

        private string GetSelectedNftPubKey()
        {
            return PlayerPrefs.GetString("SelectedNft");
        }
        
        private async Task<TokenAccount[]> GetOwnedTokenAccounts(string publicKey)
        {
            var wallet = ServiceFactory.Instance.Resolve<WalletHolderService>().BaseWallet;
            try
            {
                RequestResult<ResponseValue<List<TokenAccount>>> result =
                    await wallet.ActiveRpcClient.GetTokenAccountsByOwnerAsync(publicKey, null,
                        TokenProgram.ProgramIdKey);

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

        public List<SolPlayNft> GetAllNftsByMintAuthority(string mintAuthority)
        {
            List<SolPlayNft> result = new List<SolPlayNft>();
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

        public bool IsBeaverNft(SolPlayNft solPlayNft)
        {
            return solPlayNft.MetaplexData.authority == BeaverNftMintAuthority;
        }

        public void BurnNft(SolPlayNft currentNft)
        {
            ServiceFactory.Instance.Resolve<MetaPlexInteractionService>().BurnNFt(currentNft);
        }
        
        public void SelectNft(SolPlayNft nft)
        {
            if (nft == null)
            {
                return;
            }
            SelectedNft = nft;
            PlayerPrefs.SetString("SelectedNft", SelectedNft.TokenAccount.PublicKey);
            ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new NftSelectedMessage(SelectedNft));
        }
    }

    public class NftArrivedMessage
    {
        public SolPlayNft NewNFt;

        public NftArrivedMessage(SolPlayNft newNFt)
        {
            NewNFt = newNFt;
        }
    }
    
    public class NftSelectedMessage
    {
        public SolPlayNft NewNFt;

        public NftSelectedMessage(SolPlayNft newNFt)
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