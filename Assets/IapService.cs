using CandyMachineV2;
using Frictionless;
using Solana.Unity.Wallet;
using SolPlay.Deeplinks;
using UnityEngine;

/// <summary>
/// WIP This will make the game hopefully compliant with Apple Store Requirements.
/// The say we are not allowed to put it in if we use NFTs which are not also available via In app purchases.
/// </summary>
public class IapService : MonoBehaviour
{
    void Awake()
    {
        ServiceFactory.Instance.RegisterSingleton(this);
    }

    public async void OnNFtIapDone(bool wasBought)
    {
        var baseWallet = ServiceFactory.Instance.Resolve<WalletHolderService>().BaseWallet;
        var account = baseWallet.Account;
        var candy = await CandyMachineUtils.MintOneToken(account, new PublicKey("D1cEd7k6BK6cX8rwBMp5hSUzRheZxqdrmXQvMLst4Mrn"), baseWallet.ActiveRpcClient);
        Debug.Log(candy);
    }
}
