using System;
using Frictionless;
using SolPlay.CustomSmartContractExample;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// Show the image and the power level of a given Nft and can have a click handler
    /// </summary>
    public class NftItemView : MonoBehaviour
    {
        public SolPlayNft currentSolPlayNft;

        public RawImage Icon;
        public TextMeshProUGUI Headline;
        public TextMeshProUGUI Description;
        public TextMeshProUGUI PowerLevel;
        public Button Button;
        public GameObject SelectionGameObject;
        public SolPlayNft CurrentNft;

        private Action<NftItemView> onButtonClickedAction;

        public void SetData(SolPlayNft solPlayNft, Action<NftItemView> onButtonClicked)
        {
            CurrentNft = solPlayNft;
            Icon.gameObject.SetActive(false);

            if (gameObject.activeInHierarchy)
            {
                Icon.gameObject.SetActive(true);
                if (solPlayNft.MetaplexData.nftImage != null)
                {
                    Icon.texture = solPlayNft.MetaplexData.nftImage.file;
                }
            }
            var nftService = ServiceFactory.Instance.Resolve<NftService>();

            SelectionGameObject.gameObject.SetActive(nftService.IsNftSelected(solPlayNft));
            Headline.text = solPlayNft.MetaplexData.data.name;
            Description.text = solPlayNft.MetaplexData.data.json.description;
            var nftPowerLevelService = ServiceFactory.Instance.Resolve<HighscoreService>();
            var highscoreForPubkey = nftPowerLevelService.GetHighscoreForPubkey(solPlayNft.MetaplexData.mint);
            if (highscoreForPubkey != null)
            {
                PowerLevel.text =
                    $"Score: {highscoreForPubkey.Highscore}";
            }
            else
            {
                PowerLevel.text = "Loading";
            }

            Button.onClick.AddListener(OnButtonClicked);
            onButtonClickedAction = onButtonClicked;
            currentSolPlayNft = solPlayNft;
        }

        private void OnButtonClicked()
        {
            onButtonClickedAction?.Invoke(this);
        }
    }
}