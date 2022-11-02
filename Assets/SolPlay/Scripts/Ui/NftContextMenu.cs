using Frictionless;
using SolPlay.Scripts.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Scripts.Ui
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
        public Button TransferButton;
        public SolPlayNft currentNft;
 
        private void Awake()
        {
            ServiceFactory.RegisterSingleton(this);
            Root.gameObject.SetActive(false);
            CloseButton.onClick.AddListener(OnCloseButtonClicked);
            BurnButton.onClick.AddListener(OnBurnClicked);
            SelectButton.onClick.AddListener(OnSelectClicked);
            TransferButton.onClick.AddListener(OnTransferClicked);
        }

        private void OnTransferClicked()
        {
            ServiceFactory.Resolve<UiService>().OpenPopup(UiService.ScreenType.TransferNftPopup, new TransferPopupUiData(currentNft));
        }

        private void OnSelectClicked()
        {
            ServiceFactory.Resolve<NftService>().SelectNft(currentNft);
            MessageRouter.RaiseMessage(
                new BlimpSystem.ShowBlimpMessage($"{currentNft.MetaplexData.data.name} selected"));
            Close();
            var tabBarComponent = ServiceFactory.Resolve<TabBarComponent>();
            if (tabBarComponent != null)
            {
                tabBarComponent.HorizontalScrollSnap.ChangePage(1);
            }
            else
            {
                // In case you want to load another scene please use the SolPlay instance
                // SolPlay.Instance.LoadScene("FlappyGameExample");
                ServiceFactory.Resolve<LoggingService>().Log("Add you select logic in NftContextMenu.cs", true);
            }
        }

        private void OnBurnClicked()
        {
            ServiceFactory.Resolve<NftService>().BurnNft(currentNft);
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
            currentNft = nftItemView.CurrentSolPlayNft;
            Root.gameObject.SetActive(true);
            NftNameText.text = nftItemView.CurrentSolPlayNft.MetaplexData.data.name;
            transform.position = nftItemView.transform.position;
            var powerLevelService = ServiceFactory.Resolve<HighscoreService>();
            PowerLevelText.text =
                $"High score: {powerLevelService.GetHighscoreForPubkey(nftItemView.CurrentSolPlayNft.MetaplexData.mint).Highscore}";
        }
    }
}