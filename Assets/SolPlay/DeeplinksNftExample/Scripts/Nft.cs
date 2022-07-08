using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AllArt.Solana.Utility;
using Frictionless;
using Solnet.Rpc;
using Solnet.Rpc.Models;
using Solnet.Rpc.Utilities;
using SolPlay.Deeplinks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AllArt.Solana.Nft
{
    [Serializable]
    public class NftImage : iNftFile<Texture2D>
    {
        public string Name { get; set; }
        public string Extension { get; set; }
        public string ExternalUrl { get; set; }
        public Texture2D File { get; set; }
    }

    [Serializable]
    public class IgnoreTokenList
    {
        public List<string> TokenList = new List<string>();
    }

    [Serializable]
    public class Nft
    {
        public Metaplex MetaplexData;

        public Nft()
        {
        }

        private const string IgnoredTokenListPlayerPrefsKey = "IgnoredTokenList";

        public Nft(Metaplex metaplexData)
        {
            this.MetaplexData = metaplexData;
        }

        public static async Task<NFTProData> TryGetNftPro(string mint, SolanaRpcClient connection)
        {
            AccountInfo accountInfo = await AccountUtility.GetAccountData(mint, connection);

            Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(accountInfo));

            if (accountInfo != null && accountInfo.Data != null && accountInfo.Data.Count > 0)
            {
                AccountLayout accountlayout = AccountLayout.DeserializeAccountLayout(accountInfo.Data[0]);
                Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(accountlayout));
            }

            return null;
        }

        public static async Task<Nft> TryGetNftData(string mint, SolanaRpcClient connection,
            bool tryUseLocalContent = true)
        {
            // We put broken tokens on an ignore list so we dont need to load the information every time. 
            if (IsTokenMintIgnored(mint))
            {
                return null;
            }

            Solnet.Wallet.PublicKey metaplexDataPubKey = FindProgramAddress(mint);

            if (metaplexDataPubKey != null)
            {
                if (tryUseLocalContent)
                {
                    Nft nft = TryLoadNftFromLocal(mint);
                    if (nft != null)
                    {
                        return nft;
                    }
                }

                AccountInfo accountInfo = await AccountUtility.GetAccountData(metaplexDataPubKey.Key, connection);

                if (accountInfo != null && accountInfo.Data != null && accountInfo.Data.Count > 0)
                {
                    try
                    {
                        Metaplex metaPlex = new Metaplex().ParseData(accountInfo.Data[0]);
                        MetaplexJsonData jsonData = await FileLoader.LoadFile<MetaplexJsonData>(metaPlex.data.url);

                        if (jsonData != null)
                        {
                            metaPlex.data.json = jsonData;

                            Texture2D texture = await FileLoader.LoadFile<Texture2D>(metaPlex.data.json.image);
                            var nftImageSize = ServiceFactory.Instance.Resolve<NftService>().NftImageSize;

                            Texture2D compressedTexture = Resize(texture, nftImageSize, nftImageSize);
                            FileLoader.SaveToPersistenDataPath(
                                Path.Combine(Application.persistentDataPath, $"{mint}.png"), compressedTexture);

                            if (compressedTexture)
                            {
                                NftImage nftImage = new NftImage();
                                nftImage.ExternalUrl = jsonData.image;
                                nftImage.File = compressedTexture;
                                metaPlex.nftImage = nftImage;
                            }
                        }

                        Nft newNft = new Nft(metaPlex);
                        FileLoader.SaveToPersistenDataPath(Path.Combine(Application.persistentDataPath, $"{mint}.json"),
                            newNft);
                        return newNft;
                    }
                    catch (Exception e)
                    {
                        AddToIgnoredTokenListAndSave(mint);
                        Debug.LogWarning($"Nft data could not be parsed: {e} -> Added to ignore list");
                    }
                }
                else
                {
                    AddToIgnoredTokenListAndSave(mint);
                    Debug.LogWarning($"Token seems to not be an NFT -> Added to ignore list");
                }
            }

            return null;
        }

        private static bool IsTokenMintIgnored(string mint)
        {
            if (GetIgnoreTokenList().TokenList.Contains(mint))
            {
                return true;
            }

            return false;
        }

        private static IgnoreTokenList GetIgnoreTokenList()
        {
            if (!PlayerPrefs.HasKey(IgnoredTokenListPlayerPrefsKey))
            {
                PlayerPrefs.SetString(IgnoredTokenListPlayerPrefsKey, JsonUtility.ToJson(new IgnoreTokenList()));
            }

            var json = PlayerPrefs.GetString(IgnoredTokenListPlayerPrefsKey);
            var ignoreTokenList = JsonUtility.FromJson<IgnoreTokenList>(json);
            return ignoreTokenList;
        }

        private static void AddToIgnoredTokenListAndSave(string mint)
        {
            string blimpMessage = $"Added {mint} to ignore list.";
            ServiceFactory.Instance.Resolve<MessageRouter>()
                .RaiseMessage(new BlimpSystem.ShowBlimpMessage(blimpMessage));

            var ignoreTokenList = GetIgnoreTokenList();
            ignoreTokenList.TokenList.Add(mint);
            PlayerPrefs.SetString(IgnoredTokenListPlayerPrefsKey, JsonUtility.ToJson(ignoreTokenList));
            PlayerPrefs.Save();
        }

        public static Nft TryLoadNftFromLocal(string mint)
        {
            Nft local = FileLoader.LoadFileFromLocalPath<Nft>(
                $"{Path.Combine(Application.persistentDataPath, mint)}.json");

            if (local != null)
            {
                Texture2D tex =
                    FileLoader.LoadFileFromLocalPath<Texture2D>(
                        $"{Path.Combine(Application.persistentDataPath, mint)}.png");
                if (tex)
                {
                    local.MetaplexData.nftImage = new NftImage();
                    local.MetaplexData.nftImage.File = tex;
                }
                else
                {
                    return null;
                }
            }

            return local;
        }

        /// <summary>
        /// Returns public key of nft
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="programId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Solnet.Wallet.PublicKey CreateAddress(List<byte[]> seed, string programId)
        {
            List<byte> buffer = new List<byte>();

            foreach (byte[] item in seed)
            {
                if (item.Length > 32)
                {
                    throw new Exception("Too long");
                }

                buffer.AddRange(item);
            }

            buffer.AddRange(seed[1]);
            byte[] derive = Encoding.UTF8.GetBytes("ProgramDerivedAddress");
            buffer.AddRange(derive);

            SHA256 sha256 = SHA256.Create();
            byte[] hash1 = sha256.ComputeHash(buffer.ToArray());

            if (hash1.IsOnCurve() != 0)
            {
                throw new Exception("Not on curve");
            }

            Solnet.Wallet.PublicKey publicKey = new Solnet.Wallet.PublicKey(hash1);
            return publicKey;
        }

        /// <summary>
        /// Returns metaplex data pubkey from mint pubkey and programId
        /// </summary>
        /// <param name="mintPublicKey"></param>
        /// <param name="programId"></param>
        /// <returns></returns>
        public static Solnet.Wallet.PublicKey FindProgramAddress(string mintPublicKey,
            string programId = "metaqbxxUerdq28cj1RbAWkYQm3ybzjb6a8bt518x1s")
        {
            List<byte[]> seeds = new List<byte[]>();

            int nonce = 255;
            seeds.Add(Encoding.UTF8.GetBytes("metadata"));
            seeds.Add(new Solnet.Wallet.PublicKey(programId).KeyBytes);
            seeds.Add(new Solnet.Wallet.PublicKey(mintPublicKey).KeyBytes);
            seeds.Add(new[] {(byte) nonce});

            Solnet.Wallet.PublicKey publicKey = null;

            while (nonce != 0)
            {
                try
                {
                    seeds[3] = new[] {(byte) nonce};
                    publicKey = CreateAddress(seeds, programId);
                    return publicKey;
                }
                catch
                {
                    nonce--;
                    continue;
                }
            }

            return publicKey;
        }

        /// <summary>
        /// Returns metaplex json data from forwarded jsonUrl
        /// </summary>
        public static async Task<T> GetMetaplexJsonData<T>(string jsonUrl)
        {
            HttpClient client = new HttpClient();

            HttpResponseMessage response = await client.GetAsync(jsonUrl);
            response.EnsureSuccessStatusCode();

            try
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                Debug.Log(responseBody);
                T data = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responseBody);
                client.Dispose();
                return data;
            }
            catch
            {
                client.Dispose();
                return default;
                throw;
            }
        }

        /// <summary>
        /// Resize great textures to small, because of performance
        /// </summary>
        private static Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
        {
            RenderTexture rt = new RenderTexture(targetX, targetY, 24);
            RenderTexture.active = rt;
            Graphics.Blit(texture2D, rt);
            Texture2D result = new Texture2D(targetX, targetY);
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
            result.Apply();
            Object.Destroy(rt);
            return result;
        }
    }
}