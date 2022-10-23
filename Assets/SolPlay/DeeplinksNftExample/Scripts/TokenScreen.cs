using Frictionless;
using SolPlay.Deeplinks;
using SolPlay.DeeplinksNftExample.Scripts;
using UnityEngine;
using UnityEngine.UI;

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
        var transactionService = ServiceFactory.Instance.Resolve<TransactionService>();
        transactionService.TransferSolanaToPubkey(transactionService.EditorExampleWalletPublicKey);
    }

    private void OnGetSolPlayTokenButtonClicked()
    {
        // To let people buy a token just put the direct raydium link to your token and open it with a phantom deeplink. 
        PhantomUtils.OpenUrlInWalletBrowser(
            "https://raydium.io/swap/?inputCurrency=sol&outputCurrency=PLAyKbtrwQWgWkpsEaMHPMeDLDourWEWVrx824kQN8P&inputAmount=0.1&outputAmount=0.9&fixed=in");
    }
}
