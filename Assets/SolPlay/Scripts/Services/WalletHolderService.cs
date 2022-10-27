using System.Collections;
using System.Threading.Tasks;
using Frictionless;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using SolPlay.Scripts.Ui;
using Unity.VisualScripting;
using UnityEngine;

namespace SolPlay.Scripts.Services
{
    public class WalletLoggedInMessage
    {
        public WalletBase Wallet;
    }

    public class WalletHolderService : MonoBehaviour, IMultiSceneSingleton
    {
        public RpcCluster InGameWalletCluster = RpcCluster.DevNet;

        [HideIfEnumValue("InGameWalletCluster", HideIf.NotEqual, (int) RpcCluster.Custom)]
        public string InGameWalletCustomRpcUrl = "";

        public RpcCluster PhantomWalletCluster = RpcCluster.DevNet;

        [HideIfEnumValue("PhantomWalletCluster", HideIf.NotEqual, (int) RpcCluster.Custom)]
        public string PhantomWalletCustomUrl = "";

        public PhantomWalletOptions PhantomWalletOptions;

        [DoNotSerialize] public WalletBase BaseWallet;

        public bool IsLoggedIn { get; private set; }

        private PhantomWallet DeeplinkWallet;
        private InGameWallet InGameWallet;

        private void Awake()
        {
            if (ServiceFactory.Resolve<WalletHolderService>() != null)
            {
                Destroy(gameObject);
                return; 
            }

            ServiceFactory.RegisterSingleton(this);
        }

        private void Start()
        {
            DeeplinkWallet =
                new PhantomWallet(PhantomWalletOptions, PhantomWalletCluster, PhantomWalletCustomUrl, true);

            if (InGameWalletCluster == RpcCluster.Custom)
            {
                InGameWallet = new InGameWallet(InGameWalletCluster, InGameWalletCustomRpcUrl, true);
            }
            else
            {
                InGameWallet = new InGameWallet(InGameWalletCluster, null, true);
            }
        }

        public async Task<Account> Login(bool devNetLogin)
        {
            if (devNetLogin)
            {
                BaseWallet = InGameWallet;

                var newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
                var account = await InGameWallet.Login("1234") ??
                              await InGameWallet.CreateAccount(newMnemonic.ToString(), "1234");
                // Copy this private key if you want to import your wallet into phantom. Dont share it with anyone.
                // var privateKeyString = account.PrivateKey.Key;
                double sol = await BaseWallet.GetBalance();

                if (sol < 0.8)
                {
                    MessageRouter.RaiseMessage(new BlimpSystem.ShowBlimpMessage("Requesting airdrop"));
                    string result = await BaseWallet.RequestAirdrop(1000000000, Commitment.Confirmed);
                    ServiceFactory.Resolve<TransactionService>().CheckSignatureStatus(result,
                        () => { MessageRouter.RaiseMessage(new SolBalanceChangedMessage()); },
                        TransactionService.TransactionResult.confirmed);
                }
            }
            else
            {
#if (UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL)
                BaseWallet = DeeplinkWallet;
                Debug.Log(BaseWallet.ActiveRpcClient.NodeAddress);
                await BaseWallet.Login();
#endif
            }

            IsLoggedIn = true;
            MessageRouter.RaiseMessage(new WalletLoggedInMessage()
            {
                Wallet = BaseWallet
            });
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

        public IEnumerator HandleNewSceneLoaded()
        {
            yield return null;
        }
    }
}