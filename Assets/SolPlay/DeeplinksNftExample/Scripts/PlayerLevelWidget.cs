using Frictionless;
using SolPlay.CustomSmartContractExample;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// Whenever a new NFT arrives this widget updates the total power level of all Nfts
    /// </summary>
    public class PlayerLevelWidget : MonoBehaviour
    {
        public TextMeshProUGUI CurrentPlayerLevelText;
        public Button LevelUpButton;
        public Button RefreshPlayerLevelButton;
        
        void Start()
        {
            LevelUpButton.onClick.AddListener(OnLevelUpButtonClicked);
            RefreshPlayerLevelButton.onClick.AddListener(OnRefreshPlayerLevelButtonClicked);
            OnRefreshPlayerLevelButtonClicked();
        }

        private async void OnRefreshPlayerLevelButtonClicked()
        {
            var account = await ServiceFactory.Instance.Resolve<CustomSmartContractService>().RefreshLevelAccountData();
        }

        private async void OnLevelUpButtonClicked()
        {
            await ServiceFactory.Instance.Resolve<CustomSmartContractService>().IncreasePlayerLevel();
            var account = await ServiceFactory.Instance.Resolve<CustomSmartContractService>().RefreshLevelAccountData();
        }

        private void Update()
        {
            CurrentPlayerLevelText.text = "Player level: " + ServiceFactory.Instance.Resolve<CustomSmartContractService>().CurrentPlayerLevel.ToString();
        }
    }
}