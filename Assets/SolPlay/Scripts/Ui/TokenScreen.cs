using Frictionless;
using SolPlay.Deeplinks;
using SolPlay.DeeplinksNftExample.Scripts;
using SolPlay.DeeplinksNftExample.Utils;
using SolPlay.Scripts.Services;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Scripts.Ui
{
    public class TokenScreen : MonoBehaviour
    {
        public Button GetSolPlayTokenButton;
        public Button PhantomTransactionButton;

        void Awake()
        {
            GetSolPlayTokenButton.gameObject.SetActive(false);
            PhantomTransactionButton.gameObject.SetActive(false);
            GetSolPlayTokenButton.onClick.AddListener(OnGetSolPlayTokenButtonClicked);
            PhantomTransactionButton.onClick.AddListener(OnPhantomTransactionButtonClicked);
        }

        private void OnPhantomTransactionButtonClicked()
        {
            var transactionService = ServiceFactory.Resolve<TransactionService>();
            // Transfer 0.1Sol 
            transactionService.TransferSolanaToPubkey(transactionService.EditorExampleWalletPublicKey,
                SolanaUtils.SolToLamports / 10);
        }

        private void OnGetSolPlayTokenButtonClicked()
        {
            // To let people buy a token just put the direct raydium link to your token and open it with a phantom deeplink. 
            PhantomUtils.OpenUrlInWalletBrowser(
                "https://raydium.io/swap/?inputCurrency=sol&outputCurrency=PLAyKbtrwQWgWkpsEaMHPMeDLDourWEWVrx824kQN8P&inputAmount=0.1&outputAmount=0.9&fixed=in");
        }
    }
}
