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
        public TextMeshProUGUI Headline;
        public TextMeshProUGUI Description;
        public TextMeshProUGUI PowerLevel;
        public Button Button;
        public GameObject SelectionGameObject;

        private Action<NftItemView> onButtonClickedAction;

        public void SetData(SolPlayNft solPlayNftData, Action<NftItemView> onButtonClicked)
        {
            Icon.gameObject.SetActive(false);

            if (gameObject.activeInHierarchy)
            {
                Icon.gameObject.SetActive(true);
                if (solPlayNftData.MetaplexData.nftImage != null)
                {
                    Icon.texture = solPlayNftData.MetaplexData.nftImage.file;
                }
            }
            var nftService = ServiceFactory.Instance.Resolve<NftService>();

            SelectionGameObject.gameObject.SetActive(nftService.IsNftSelected(solPlayNftData));
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