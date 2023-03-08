using Frictionless;
using SolPlay.Scripts.Services;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Scripts.Ui
{
    /// <summary>
    /// Screen will enable the connected root as soon as the wallet is logged in
    /// </summary>
    public class LoginScreen : MonoBehaviour
    {
        public Button PhantomLoginButton;
        public Button DevnetInGameWalletButton;
        public GameObject ConnectedRoot;
        public GameObject NotConnectedRoot;
        public GameObject TabBarRoot;
        public GameObject LoadingRoot;

        private void Awake()
        {
            PhantomLoginButton.onClick.AddListener(OnPhantomButtonClicked);
            DevnetInGameWalletButton.onClick.AddListener(OnDevnetInGameWalletButtonClicked);
            SetLoadingRoot(false);
        }

        private void Start()
        {
            UpdateContent();
        }

        private async void OnDevnetInGameWalletButtonClicked()
        {
            SetLoadingRoot(true);
            await ServiceFactory.Resolve<WalletHolderService>().Login(WalletType.Phantom,true);
            UpdateContent();
        }

        private async void OnPhantomButtonClicked()
        {
            SetLoadingRoot(true);
            await ServiceFactory.Resolve<WalletHolderService>().Login(WalletType.Phantom,false);
            UpdateContent();
        }

        private void UpdateContent()
        {
            SetLoadingRoot(false);
            bool isLoggedIn = ServiceFactory.Resolve<WalletHolderService>().IsLoggedIn;
            ConnectedRoot.gameObject.SetActive(isLoggedIn);
            NotConnectedRoot.gameObject.SetActive(!isLoggedIn);
            if (TabBarRoot != null)
            {
                TabBarRoot.gameObject.SetActive(isLoggedIn);
            }
        }

        private void SetLoadingRoot(bool active)
        {
            if (LoadingRoot != null)
            {
                LoadingRoot.SetActive(active);
            }
        }
    }
}
