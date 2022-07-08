using System;
using AllArt.Solana.Nft;
using Frictionless;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// Show the image and the power level of a given Nft and can have a click handler
    /// </summary>
    public class NftListItemView : MonoBehaviour
    {
        public Nft CurrentNft;

        public RawImage Icon;
        public Image DummyIcon;
        public TextMeshProUGUI Headline;
        public TextMeshProUGUI Description;
        public TextMeshProUGUI PowerLevel;
        public Button Button;

        private Action<NftListItemView> onButtonClickedAction;

        public void SetData(Nft nftData, Action<NftListItemView> onButtonClicked)
        {
            Icon.gameObject.SetActive(false);
            DummyIcon.gameObject.SetActive(false);

            if (gameObject.activeInHierarchy)
            {
                Icon.gameObject.SetActive(true);
                if (nftData.MetaplexData.nftImage != null)
                {
                    Icon.texture = nftData.MetaplexData.nftImage.File;
                }
            }

            Headline.text = nftData.MetaplexData.data.name;
            Description.text = nftData.MetaplexData.data.json.description;
            PowerLevel.text =
                $"Power: {ServiceFactory.Instance.Resolve<NftPowerLevelService>().GetPowerLevelFromNft(nftData)}";
            Button.onClick.AddListener(OnButtonClicked);
            onButtonClickedAction = onButtonClicked;
            CurrentNft = nftData;
        }

        private void OnButtonClicked()
        {
            onButtonClickedAction?.Invoke(this);
        }
    }
}