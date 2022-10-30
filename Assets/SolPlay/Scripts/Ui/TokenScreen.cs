using Frictionless;
using Solana.Unity.Wallet;
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
        public Button TransferSolButton;
        public Button TokenTransactionButton;

        void Awake()
        {
            GetSolPlayTokenButton.onClick.AddListener(OnGetSolPlayTokenButtonClicked);
            TransferSolButton.onClick.AddListener(OnTransferSolButtonClicked);
            TokenTransactionButton.onClick.AddListener(OnTokenTransactionButtonClicked);
        }

        private async void OnTokenTransactionButtonClicked()
        {
            var transactionService = ServiceFactory.Resolve<TransactionService>();

            // Transfer one usdc token to to another wallet. USDC has 6 decimals to we need to send 1000000 for 1 USDC
            var result = await transactionService.TransferTokenToPubkey(
                new PublicKey(transactionService.EditorExampleWalletPublicKey),
                new PublicKey("EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v"), 1000000);
        }

        private async void OnTransferSolButtonClicked()
        {
            var transactionService = ServiceFactory.Resolve<TransactionService>();
            // Transfer 0.1Sol 
            var result = await transactionService.TransferSolanaToPubkey(transactionService.EditorExampleWalletPublicKey,
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
