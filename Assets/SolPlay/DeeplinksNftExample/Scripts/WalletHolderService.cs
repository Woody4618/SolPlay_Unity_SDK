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

        public async Task<Account> Login()
        {
#if UNITY_EDITOR
            BaseWallet = InGameWallet;

            var account = await InGameWallet.Login();
            if (account == null)
            {
                account = await InGameWallet.CreateAccount();
            }
#endif

#if (UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            BaseWallet = DeeplinkWallet;
            Debug.Log(BaseWallet.ActiveRpcClient.NodeAddress);
            await BaseWallet.Login();
#endif

            Debug.Log("Logged in: " + BaseWallet.Account.PublicKey);
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