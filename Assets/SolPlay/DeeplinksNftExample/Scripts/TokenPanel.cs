using Frictionless;
using TMPro;
using UnityEngine;

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// Shows the amount of the token "TokenMintAdress" from the connected Wallet.
    /// </summary>
    public class TokenPanel : MonoBehaviour
    {
        public TextMeshProUGUI TokenAmount;

        public string
            TokenMintAdress =
                "PLAyKbtrwQWgWkpsEaMHPMeDLDourWEWVrx824kQN8P"; // Solplay Token, replace with whatever token you like.

        void Start()
        {
            ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<TokenArrivedMessage>(OnTokenArrivedMessage);
        }

        private void OnTokenArrivedMessage(TokenArrivedMessage message)
        {
            if (message.TokenAccountInfoDetails != null && message.TokenAccountInfoDetails.Mint == TokenMintAdress)
            {
                TokenAmount.text = message.TokenAccountInfoDetails.TokenAmount.AmountDouble.ToString("F2");
            }
        }
    }
}