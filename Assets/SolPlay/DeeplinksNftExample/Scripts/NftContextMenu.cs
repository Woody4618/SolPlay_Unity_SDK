using Frictionless;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// When clicking a Nft this context menu opens and shows some information about the Nft
    /// </summary>
    public class NftContextMenu : MonoBehaviour
    {
        public GameObject Root;
        public Button CloseButton;
        public TextMeshProUGUI NftNameText;
        public TextMeshProUGUI PowerLevelText;

        private void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);
            Root.gameObject.SetActive(false);
            CloseButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnCloseButtonClicked()
        {
            Close();
        }

        private void Close()
        {
            Root.gameObject.SetActive(false);
        }

        public void Open(NftItemView nftItemView)
        {
            Root.gameObject.SetActive(true);
            NftNameText.text = nftItemView.currentSolPlayNft.MetaplexData.data.name;
            transform.position = nftItemView.transform.position;
            var powerLevelService = ServiceFactory.Instance.Resolve<NftPowerLevelService>();
            PowerLevelText.text = $"Power level {powerLevelService.GetPowerLevelFromNft(nftItemView.currentSolPlayNft)}";
        }
    }
}