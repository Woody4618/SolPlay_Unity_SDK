using Frictionless;
using Solplay.Deeplinks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Solplay.Deeplinks
{
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

        public void Open(NFTItemView nftItemView)
        {
            Root.gameObject.SetActive(true);
            NftNameText.text = nftItemView.CurrentNft.MetaplexData.data.name;
            transform.position = nftItemView.transform.position;
            var powerLevelService = ServiceFactory.Instance.Resolve<NftPowerLevelService>();
            PowerLevelText.text = $"Power level {powerLevelService.GetPowerLevelFromNft(nftItemView.CurrentNft)}";
        }
    }
}