using System;
using Frictionless;
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
        public Image DummyIcon;
        public TextMeshProUGUI Headline;
        public TextMeshProUGUI Description;
        public TextMeshProUGUI PowerLevel;
        public Button Button;

        private Action<NftItemView> onButtonClickedAction;

        public void SetData(SolPlayNft solPlayNftData, Action<NftItemView> onButtonClicked)
        {
            Icon.gameObject.SetActive(false);
            DummyIcon.gameObject.SetActive(false);

            if (gameObject.activeInHierarchy)
            {
                Icon.gameObject.SetActive(true);
                if (solPlayNftData.MetaplexData.nftImage != null)
                {
                    Icon.texture = solPlayNftData.MetaplexData.nftImage.file;
                }
            }

            Headline.text = solPlayNftData.MetaplexData.data.name;
            Description.text = solPlayNftData.MetaplexData.data.json.description;
            var nftPowerLevelService = ServiceFactory.Instance.Resolve<NftPowerLevelService>();
            PowerLevel.text =
                $"Power: {nftPowerLevelService.GetPowerLevelFromNft(solPlayNftData)}";
            Button.onClick.AddListener(OnButtonClicked);
            onButtonClickedAction = onButtonClicked;
            currentSolPlayNft = solPlayNftData;
        }

        private void OnButtonClicked()
        {
            onButtonClickedAction?.Invoke(this);
        }
    }
}