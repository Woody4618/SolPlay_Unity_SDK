using Frictionless;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using TMPro;
using UnityEngine;

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// Shows the amount of the token "TokenMintAddress" from the connected Wallet.
    /// </summary>
    public class TokenPanel : MonoBehaviour
    {
        public TextMeshProUGUI TokenAmount;

        public string
            TokenMintAdress =
                "PLAyKbtrwQWgWkpsEaMHPMeDLDourWEWVrx824kQN8P"; // Solplay Token, replace with whatever token you like.

        private PublicKey _associatedTokenAddress;
        
        void Start()
        {
            ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<TokenArrivedMessage>(OnTokenArrivedMessage);
            ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<WalletLoggedInMessage>(OnWalletLoggedInMessage);
            ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<TokenValueChangedMessage>(OnTokenValueChangedMessage);
        }

        private void OnTokenValueChangedMessage(TokenValueChangedMessage message)
        {
            UpdateTokenAmount();
        }

        private async void OnWalletLoggedInMessage(WalletLoggedInMessage message)
        {
            UpdateTokenAmount();
        }

        private async void UpdateTokenAmount()
        {
            var wallet = ServiceFactory.Instance.Resolve<WalletHolderService>().BaseWallet;
            if (wallet != null && wallet.Account.PublicKey != null)
            {
                _associatedTokenAddress =
                    AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(wallet.Account.PublicKey, new PublicKey(TokenMintAdress));
            }
            if (_associatedTokenAddress == null)
            {
                return;
            }
            
            var tokenBalance = await wallet.ActiveRpcClient.GetTokenAccountBalanceAsync(_associatedTokenAddress, Commitment.Confirmed);
            if (tokenBalance.Result == null || tokenBalance.Result.Value == null)
            {
                TokenAmount.text = "0 (No Account)";
                return;
            }
            TokenAmount.text = tokenBalance.Result.Value.UiAmountString;
        }
        
        private void OnTokenArrivedMessage(TokenArrivedMessage message)
        {
            if (message.TokenAccountInfoDetails != null && message.TokenAccountInfoDetails.Mint == TokenMintAdress)
            {
                UpdateTokenAmount();
            }
        }
    }
}