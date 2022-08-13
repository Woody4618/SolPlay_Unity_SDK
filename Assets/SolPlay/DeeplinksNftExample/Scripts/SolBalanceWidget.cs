using System.Threading.Tasks;
using Frictionless;
using TMPro;
using UnityEngine;

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// Shows the sol balance of the connected wallet. Should be updated at certain points, after transactions for example.
    /// </summary>
    public class SolBalanceWidget : MonoBehaviour
    {
        public TextMeshProUGUI SolBalance;
        
        async void Start()
        {
            ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<SolBalanceChangedMessage>(OnSolBalanceChangedMessage);
            await RequestSolBalance();
        }

        private void OnDestroy()
        {
            ServiceFactory.Instance.Resolve<MessageRouter>().RemoveHandler<SolBalanceChangedMessage>(OnSolBalanceChangedMessage);
        }

        private async Task RequestSolBalance()
        {
            var wallet = ServiceFactory.Instance.Resolve<WalletHolderService>().BaseWallet;
            double sol = await wallet.GetBalance();
            SolBalance.text = sol.ToString("F2") + " sol";
        }

        private void OnSolBalanceChangedMessage(SolBalanceChangedMessage message)
        {
            RequestSolBalance();
        }
    }
}