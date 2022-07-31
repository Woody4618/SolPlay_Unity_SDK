using Frictionless;
using UnityEngine;

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// Handles all logic related to NFTs and calculating their power level or whatever you like to do with the NFTs
    /// </summary>
    public class NftPowerLevelService : MonoBehaviour
    {
        private void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);
        }

        public int GetPowerLevelFromNft(SolPlayNft solPlayNft)
        {
            var nftService = ServiceFactory.Instance.Resolve<NftService>();
            if (nftService.IsBeaverNft(solPlayNft))
            {
                return CalculateBeaverPower(solPlayNft);
            }

            return 1;
        }

        // Just some power level calculations, you could to what ever with this. For example take the value from 
        // one of the attributes as damage for you character for example.
        private int CalculateBeaverPower(SolPlayNft beaverSolPlayNft)
        {
            int bonusBeaverPower = 0;
            foreach (var entry in beaverSolPlayNft.MetaplexData.data.json.attributes)
            {
                switch (entry.value)
                {
                    case "none":
                        bonusBeaverPower += 1;
                        break;
                    case "EvilOtter":
                        bonusBeaverPower += 450;
                        break;
                    case "BlueEyes":
                        bonusBeaverPower += 25;
                        break;
                    case "GreenEyes":
                        bonusBeaverPower += 25;
                        break;
                    case "Sharingan":
                        bonusBeaverPower += 350;
                        break;
                    case "RabbitKing":
                        bonusBeaverPower += 9999;
                        break;
                    case "Chicken":
                        bonusBeaverPower += 7;
                        break;
                    case "Beer":
                        bonusBeaverPower += 6;
                        break;
                    case "SimpleStick":
                        bonusBeaverPower += 5;
                        break;
                    case "GolTooth":
                        bonusBeaverPower += 16;
                        break;
                    case "Brezel":
                        bonusBeaverPower += 22;
                        break;
                    case "PrideCap":
                        bonusBeaverPower += 17;
                        break;
                    case "Santahat":
                        bonusBeaverPower += 12;
                        break;
                    case "SunGlasses":
                        bonusBeaverPower += 13;
                        break;
                    case "Suit":
                        bonusBeaverPower += 12;
                        break;
                    case "LederHosen":
                        bonusBeaverPower += 14;
                        break;
                    case "XmasBulbs":
                        bonusBeaverPower += 17;
                        break;
                    case "BPhone":
                        bonusBeaverPower += 8;
                        break;
                    default:
                        bonusBeaverPower += 3;
                        break;
                }
            }

            return 5 + bonusBeaverPower;
        }

        public float GetTotalPowerLevel()
        {
            var nftService = ServiceFactory.Instance.Resolve<NftService>();
            int result = 0;
            foreach (SolPlayNft nft in nftService.MetaPlexNFts)
            {
                result += GetPowerLevelFromNft(nft);
            }

            return result;
        }
    }
}