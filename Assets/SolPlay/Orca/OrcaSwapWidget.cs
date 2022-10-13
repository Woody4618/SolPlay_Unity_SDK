using System;
using System.Collections.Generic;
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
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

public class OrcaSwapWidget : MonoBehaviour
{
    public PoolListItem PoolListItemPrefab;
    public GameObject PoolListItemRoot;

    public List<string> PoolIdWhiteList = new List<string>();

    private void Start()
    {
        ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<WalletLoggedInMessage>(OnWalletLoggedInMessage);
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
        if (whiteList)
        {
            foreach (var entry in PoolIdWhiteList)
            {
                try
                {
                    Whirlpool.Accounts.Whirlpool pool =
                        await ServiceFactory.Instance.Resolve<OrcaWhirlpoolService>().GetPool(entry);
                    pools.Add(pool);
                }
                catch (Exception)
                {
                    // May not exist on dev net
                }
            }
        }
        else
        {
            // You can get all pools, but its very expensive call. So need a good RPC. 
            pools = await ServiceFactory.Instance.Resolve<OrcaWhirlpoolService>().GetPools();
            if (pools == null)
            {
                ServiceFactory.Instance.Resolve<LoggingService>()
                    .LogWarning("Could not load pools. Are you connected to the internet?", true);
            }
        }

        initPools(pools);
    }

    private async void initPools(List<Whirlpool.Accounts.Whirlpool> pools)
    {
        var wallet = ServiceFactory.Instance.Resolve<WalletHolderService>().BaseWallet;

        string poolList = String.Empty;

        for (var index = 0; index < pools.Count; index++)
        {
            Whirlpool.Accounts.Whirlpool pool = pools[index];
            PublicKey whirlPoolPda = OrcaPDAUtils.GetWhirlpoolPda(OrcaWhirlpoolService.WhirlpoolProgammId,
                pool.WhirlpoolsConfig,
                pool.TokenMintA, pool.TokenMintB, pool.TickSpacing);

            if (!PoolIdWhiteList.Contains(whirlPoolPda))
            {
                // continue;
            }

            PoolData poolData = new PoolData();

            poolData.Pool = pool;
            poolData.PoolPda = whirlPoolPda;

            var metadataPdaA = MetaPlexPDAUtils.GetMetadataPDA(pool.TokenMintA);
            var metadataPdaB = MetaPlexPDAUtils.GetMetadataPDA(pool.TokenMintB);

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
        var spriteFromResources = SolPlayFileLoader.LoadFromResources(symbol);
        if (spriteFromResources != null)
        {
            return spriteFromResources;
        }

        string tokenIconUrl =
            $"https://github.com/solana-labs/token-list/blob/main/assets/mainnet/{mint}/logo.png?raw=true";
        var texture = await SolPlayFileLoader.LoadFile<Texture2D>(tokenIconUrl);
        Texture2D compressedTexture = Nft.Resize(texture, 75, 75);
        var sprite = Sprite.Create(compressedTexture,
            new Rect(0.0f, 0.0f, compressedTexture.width, compressedTexture.height), new Vector2(0.5f, 0.5f),
            100.0f);
        return sprite;
    }

    private void OpenSwapPopup(PoolListItem poolListItem)
    {
        ServiceFactory.Instance.Resolve<OrcaSwapPopup>().Open(poolListItem.PoolData);
    }
}