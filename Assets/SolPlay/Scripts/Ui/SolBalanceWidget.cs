using System.Collections;
using System.Threading.Tasks;
using Frictionless;
using Solana.Unity.Rpc.Types;
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
        
        async void Start()
        {
            MessageRouter.AddHandler<SolBalanceChangedMessage>(OnSolBalanceChangedMessage);
            MessageRouter.AddHandler<TokenValueChangedMessage>(OnTokenValueChangedMessage);

            if (ServiceFactory.Resolve<WalletHolderService>().IsLoggedIn)
            {
                await OnLogin();
            }
            else
            {
                MessageRouter.AddHandler<WalletLoggedInMessage>(OnWalletLoggedIn);
            }
        }

        private async Task OnLogin()
        {
            await RequestSolBalance();
            StartCoroutine(PollSolBalance());
        }

        private async void OnWalletLoggedIn(WalletLoggedInMessage message)
        {
            await OnLogin();
        }

        private IEnumerator PollSolBalance()
        {
            while (true)
            {
                yield return new WaitForSeconds(10);
                RequestSol();
            }
        }

        private async Task RequestSolBalance()
        {
            var wallet = ServiceFactory.Resolve<WalletHolderService>().BaseWallet;
            double sol = await wallet.GetBalance(Commitment.Confirmed);
            SolBalance.text = sol.ToString("F2") + " sol";
        }

        private async void OnSolBalanceChangedMessage(SolBalanceChangedMessage message)
        {
            await RequestSolBalance();
        }

        private async void OnTokenValueChangedMessage(TokenValueChangedMessage message)
        {
            await RequestSolBalance();
        }

        private async void RequestSol()
        {
            await RequestSolBalance();
        }
    }
}