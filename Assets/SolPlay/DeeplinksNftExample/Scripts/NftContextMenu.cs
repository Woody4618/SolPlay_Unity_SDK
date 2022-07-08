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

        public void Open(NftListItemView nftListItemView)
        {
            Root.gameObject.SetActive(true);
            NftNameText.text = nftListItemView.CurrentNft.MetaplexData.data.name;
            transform.position = nftListItemView.transform.position;
            var powerLevelService = ServiceFactory.Instance.Resolve<NftPowerLevelService>();
            PowerLevelText.text = $"Power level {powerLevelService.GetPowerLevelFromNft(nftListItemView.CurrentNft)}";
        }
    }
}