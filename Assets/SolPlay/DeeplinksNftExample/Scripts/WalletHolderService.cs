using System.Threading.Tasks;
using Frictionless;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine;

namespace SolPlay.Deeplinks
{
    public class WalletHolderService : MonoBehaviour
    {
        public PhantomWallet DeeplinkWallet;
        public InGameWallet InGameWallet;

        public WalletBase BaseWallet;

        private void Awake()
        {
            ServiceFactory.Instance.RegisterSingleton(this);
        }

        public void ShowMessage(string message)
        {
            ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new BlimpSystem.ShowBlimpMessage(message));
        }
        
        public async Task<Account> Login(bool devNetLogin)
        {
            if (devNetLogin)
            {
                BaseWallet = InGameWallet;

                var account = await InGameWallet.Login() ?? await InGameWallet.CreateAccount();
                // Copy this if you want to import your wallet into phantom. Dont share it with anyone.
                // var privateKeyString = account.PrivateKey.Key;
                double sol = await BaseWallet.GetBalance();
                
                if (sol == 0)
                {
                    var messageRouter = ServiceFactory.Instance.Resolve<MessageRouter>();
                    messageRouter.RaiseMessage(new BlimpSystem.ShowBlimpMessage("Requesting airdrop"));
                    string result = await BaseWallet.RequestAirdrop(1000000000);
                    ServiceFactory.Instance.Resolve<TransactionService>().CheckSignatureStatus(result, () =>
                    {
                        messageRouter.RaiseMessage(new SolBalanceChangedMessage());
                    }, TransactionService.TransactionResult.finalized);
                }
            }
            else
            {
#if (UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            BaseWallet = DeeplinkWallet;
            Debug.Log(BaseWallet.ActiveRpcClient.NodeAddress);
            await BaseWallet.Login();
#endif
            }

            Debug.Log("Logged in: " + BaseWallet.Account.PublicKey);
            //ServiceFactory.Instance.Resolve<StakingService>().RefreshFarm();
            return BaseWallet.Account;
        }
        
        public bool TryGetPhantomPublicKey(out string phantomPublicKey)
        {
            if (BaseWallet.Account == null)
            {
                phantomPublicKey = string.Empty;
                return false;
            }
            
            phantomPublicKey = BaseWallet.Account.PublicKey;
            return true;
        }
    }
}