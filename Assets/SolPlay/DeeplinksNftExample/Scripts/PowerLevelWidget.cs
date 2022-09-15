using Frictionless;
using TMPro;
using UnityEngine;

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// Whenever a new NFT arrives this widget updates the total power level of all Nfts
    /// </summary>
    public class PowerLevelWidget : MonoBehaviour
    {
        public TextMeshProUGUI TotalPowerLevelText;

        void Start()
        {
            ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<NftArrivedMessage>(OnNftArrived);
        }

        private void OnNftArrived(NftArrivedMessage message)
        {
            var totalPowerLevel = ServiceFactory.Instance.Resolve<NftPowerLevelService>().GetTotalPowerLevel();
            TotalPowerLevelText.text = $"{totalPowerLevel}";
        }
    }
}