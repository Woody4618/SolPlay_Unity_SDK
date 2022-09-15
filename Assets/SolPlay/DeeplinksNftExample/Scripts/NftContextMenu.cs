using Frictionless;
using SolPlay.CustomSmartContractExample;
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
        public Button BurnButton;
        public Button SelectButton;
        public SolPlayNft currentNft;
        
        private void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);
            Root.gameObject.SetActive(false);
            CloseButton.onClick.AddListener(OnCloseButtonClicked);
            BurnButton.onClick.AddListener(OnBurnClicked);
            SelectButton.onClick.AddListener(OnSelectClicked);
        }

        private void OnSelectClicked()
        {
            ServiceFactory.Instance.Resolve<NftService>().SelectNft(currentNft);
            ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new BlimpSystem.ShowBlimpMessage($"{currentNft.MetaplexData.data.name} selected"));
            Close();
            ServiceFactory.Instance.Resolve<TabBarComponent>().HorizontalScrollSnap.ChangePage(1);
        }

        private void OnBurnClicked()
        {
            ServiceFactory.Instance.Resolve<NftService>().BurnNft(currentNft);
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
            currentNft = nftItemView.currentSolPlayNft;
            Root.gameObject.SetActive(true);
            NftNameText.text = nftItemView.currentSolPlayNft.MetaplexData.data.name;
            transform.position = nftItemView.transform.position;
            var powerLevelService = ServiceFactory.Instance.Resolve<HighscoreService>();
            PowerLevelText.text = $"High score: {powerLevelService.GetHighscoreForPubkey(nftItemView.currentSolPlayNft.MetaplexData.mint).Highscore}";
        }
    }
}