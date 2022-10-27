using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Frictionless;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK.Nft;
using Solana.Unity.Wallet;
using SolPlay.Deeplinks;
using SolPlay.DeeplinksNftExample.Scripts;
using SolPlay.DeeplinksNftExample.Scripts.OrcaWhirlPool;
using SolPlay.MetaPlex;
using SolPlay.Orca.OrcaWhirlPool;
using SolPlay.Scripts.Services;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class OrcaSwapWidget : MonoBehaviour
{
    public PoolListItem PoolListItemPrefab;
    public GameObject PoolListItemRoot;

    public List<string> PoolIdWhiteList = new List<string>();

    private void Start()
    {
        MessageRouter.AddHandler<WalletLoggedInMessage>(OnWalletLoggedInMessage);
        if (ServiceFactory.Resolve<WalletHolderService>().IsLoggedIn)
        {
            InitPools(true);
        }
    }

    private void OnWalletLoggedInMessage(WalletLoggedInMessage message)
    {
        InitPools(true);
    }

    /// <summary>
    /// Getting all pools without the white list is very expensive since it uses get programm accounts.
    /// public RPCs will have this call blocked. So only use it when you have our own rpc setup and want to get a list of
    /// pools to use. I added a list of main net pools in the file OrcaMainNetPoolList.txt.
    /// </summary>
    /// <param name="whiteList"></param>
    private async void InitPools(bool whiteList)
    {
        var pools = new List<Whirlpool.Accounts.Whirlpool>();
        /*
         // With this you can get a bunch of pools from the whirl pool list that is saved in the whirlpool service
       var list = ServiceFactory.Resolve<OrcaWhirlpoolService>().OrcaApiPoolsData.whirlpools;
       for (var index = 0; index < 20; index++)
       {
           var entry = list[index];
           try
           {
               Whirlpool.Accounts.Whirlpool pool =
                   await ServiceFactory.Resolve<OrcaWhirlpoolService>().GetPool(entry.address);
               pools.Add(pool);
               Debug.Log($"pool: {entry.address} {entry.tokenA.symbol} {entry.tokenB.symbol}");
           }
           catch (Exception)
           {
               // May not exist on dev net
           }
       }
 
       initPools(pools);
       return; */
        Debug.Log("Start getting pools" + PoolIdWhiteList.Count);
        if (whiteList)
        {
            foreach (var entry in PoolIdWhiteList)
            {
                try
                {
                    Whirlpool.Accounts.Whirlpool pool = await ServiceFactory.Resolve<OrcaWhirlpoolService>().GetPool(entry);
                    pools.Add(pool);

                    Debug.Log("add pool" + pool.TokenMintA);
                }
                catch (Exception e)
                {
                    // May not exist on dev net
                    Debug.Log($"Getting Pool error {e}");
                }
            }
        }
        else
        {
            // You can get all pools, but its very expensive call. So need a good RPC. public RPCs will permit it in general. 
            // You can also use the ORCA API which is saved in ServiceFactory.Resolve<OrcaWhirlpoolService>().OrcaApiPoolsData.whirlpools
            pools = await ServiceFactory.Resolve<OrcaWhirlpoolService>().GetPools();
            if (pools == null)
            {
                ServiceFactory.Resolve<LoggingService>()
                    .LogWarning("Could not load pools. Are you connected to the internet?", true);
            }
        }

        initPools(pools);
    }

    private async void initPools(List<Whirlpool.Accounts.Whirlpool> pools)
    {
        Thread.Sleep(3);
        var wallet = ServiceFactory.Resolve<WalletHolderService>().BaseWallet;

        string poolList = String.Empty;

        Debug.Log("pools" + pools.Count);
        for (var index = 0; index < pools.Count; index++)
        {
            Whirlpool.Accounts.Whirlpool pool = pools[index];
            PublicKey whirlPoolPda = OrcaPDAUtils.GetWhirlpoolPda(OrcaWhirlpoolService.WhirlpoolProgammId,
                pool.WhirlpoolsConfig,
                pool.TokenMintA, pool.TokenMintB, pool.TickSpacing);

            if (!PoolIdWhiteList.Contains(whirlPoolPda))
            {
                //continue;
            }

            PoolData poolData = new PoolData();

            poolData.Pool = pool;
            poolData.PoolPda = whirlPoolPda;

            var metadataPdaA = MetaPlexPDAUtils.GetMetadataPDA(pool.TokenMintA);
            var metadataPdaB = MetaPlexPDAUtils.GetMetadataPDA(pool.TokenMintB);

            /* Since this is very slow we get the data from the orca API instead 
            var accountInfoMintA = await wallet.ActiveRpcClient.GetTokenMintInfoAsync(pool.TokenMintA);
            var accountInfoMintB = await wallet.ActiveRpcClient.GetTokenMintInfoAsync(pool.TokenMintB);

            if (accountInfoMintA == null || accountInfoMintA.Result == null || accountInfoMintB == null ||
                accountInfoMintB.Result == null)
            {
                Debug.LogWarning($"Error:{accountInfoMintA.Reason} {accountInfoMintA} {accountInfoMintB}");
                continue;
            }
                poolData.TokenMintInfoA = accountInfoMintA.Result.Value;
                poolData.TokenMintInfoB = accountInfoMintB.Result.Value;
            */

            poolData.TokenA = ServiceFactory.Resolve<OrcaWhirlpoolService>().GetToken(pool.TokenMintA);
            poolData.TokenB = ServiceFactory.Resolve<OrcaWhirlpoolService>().GetToken(pool.TokenMintB);

            AccountInfo tokenAccountInfoA = await Nft.GetAccountData(metadataPdaA, wallet.ActiveRpcClient);
            AccountInfo tokenAccountInfoB = await Nft.GetAccountData(metadataPdaB, wallet.ActiveRpcClient);

            if (tokenAccountInfoA == null || tokenAccountInfoA.Data == null)
            {
                Debug.LogWarning($"Could not load meta data of mint {metadataPdaA}");
                continue;
            }

            if (tokenAccountInfoB == null || tokenAccountInfoB.Data == null)
            {
                Debug.LogWarning($"Could not load meta data of mint {metadataPdaB}");
                continue;
            }

            Metaplex metaPlexA = new Metaplex().ParseData(tokenAccountInfoA.Data[0], false);
            Metaplex metaPlexB = new Metaplex().ParseData(tokenAccountInfoB.Data[0], false);

            poolData.SymbolA = metaPlexA.data.symbol;
            poolData.SymbolB = metaPlexB.data.symbol;

            poolList +=
                $"\nSymbolA: {poolData.SymbolA} SymbolB: {poolData.SymbolB} PDA: {whirlPoolPda}  config: {pool.WhirlpoolsConfig}";

            poolData.SpriteA = await GetTokenIconSprite(pool.TokenMintA, poolData.SymbolA);
            poolData.SpriteB = await GetTokenIconSprite(pool.TokenMintB, poolData.SymbolB);

            PoolListItem poolListItem = Instantiate(PoolListItemPrefab, PoolListItemRoot.transform);
            Debug.Log("set data" + poolData.PoolPda);

            poolListItem.SetData(poolData, OpenSwapPopup);
        }

        Debug.Log(poolList);
    }

    /// <summary>
    /// For some reason when trying to load the icons from token list I get a cross domain error, so for now
    /// I just added some token icons on the resources folder. 
    /// </summary>
    private static async Task<Sprite> GetTokenIconSprite(string mint, string symbol)
    {
        foreach (var token in ServiceFactory.Resolve<OrcaWhirlpoolService>().OrcaApiTokenData.tokens)
        {
            var spriteFromResources = SolPlayFileLoader.LoadFromResources(symbol);
            if (spriteFromResources != null)
            {
                return spriteFromResources;
            }

            if (token.mint == mint)
            {
                string tokenIconUrl = token.logoURI;
                var texture = await SolPlayFileLoader.LoadFile<Texture2D>(tokenIconUrl);
                Texture2D compressedTexture = Nft.Resize(texture, 75, 75);
                var sprite = Sprite.Create(compressedTexture,
                    new Rect(0.0f, 0.0f, compressedTexture.width, compressedTexture.height), new Vector2(0.5f, 0.5f),
                    100.0f);
                return sprite;
            }
        }


        return null;
        /*
            Deprecated way of loading token icons from the Solana token-list
         string tokenIconUrl =
            $"https://github.com/solana-labs/token-list/blob/main/assets/mainnet/{mint}/logo.png?raw=true";
        var texture = await SolPlayFileLoader.LoadFile<Texture2D>(tokenIconUrl);
        Texture2D compressedTexture = Nft.Resize(texture, 75, 75);
        var sprite = Sprite.Create(compressedTexture,
            new Rect(0.0f, 0.0f, compressedTexture.width, compressedTexture.height), new Vector2(0.5f, 0.5f),
            100.0f);
        return sprite;*/
    }

    private void OpenSwapPopup(PoolListItem poolListItem)
    {
        var orcaSwapPopup = ServiceFactory.Resolve<OrcaSwapPopup>();
        if (orcaSwapPopup == null)
        {
            ServiceFactory.Resolve<LoggingService>()
                .Log("You need to add the OrcaSwapPopup to the scene.", true);
            return;
        }

        orcaSwapPopup.Open(poolListItem.PoolData);
    }
}