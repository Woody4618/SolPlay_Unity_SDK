using Frictionless;
using SolPlay.Scripts.Services;
using TMPro;
using UnityEngine;

namespace SolPlay.Scripts.Ui
{
    /// <summary>
    /// Shows the sol balance of the connected wallet. Should be updated at certain points, after transactions for example.
    /// </summary>
    public class SolBalanceWidget : MonoBehaviour
    {
        public TextMeshProUGUI SolBalance;
        
        void Start()
        {
            MessageRouter.AddHandler<SolBalanceChangedMessage>(OnSolBalanceChangedMessage);
            MessageRouter.AddHandler<TokenValueChangedMessage>(OnTokenValueChangedMessage);
        }

        private void UpdateContent()
        {
            var wallet = ServiceFactory.Resolve<WalletHolderService>();
            SolBalance.text = wallet.SolBalance.ToString("F2") + " sol";
        }

        private void OnSolBalanceChangedMessage(SolBalanceChangedMessage message)
        {
            UpdateContent();
        }

        private async void OnTokenValueChangedMessage(TokenValueChangedMessage message)
        {
            UpdateContent();
        }
    }
}