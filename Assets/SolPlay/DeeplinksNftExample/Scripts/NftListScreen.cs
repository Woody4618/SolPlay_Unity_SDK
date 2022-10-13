using System.Threading.Tasks;
using Frictionless;
using SolPlay.DeeplinksNftExample.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Deeplinks
{
    /// <summary>
    /// The main screen of the deeplinks example. Handles the login and different application states.
    /// </summary>
    public class NftListScreen : MonoBehaviour
    {
        public Button PhantomLoginButton;
        public Button DevnetInGameWalletButton;
        public Button GetNFtsDataButton;
        public Button GetNFtsNotCachedButton;
        public Button GetBeaverButton;
        public Button MintInAppButton;
        public Button GetSolPlayTokenButton;
        public Button PhantomTransactionButton;
        public NftItemListView NftItemListView;
        public GameObject YouDontOwnABeaverRoot;
        public GameObject YouOwnABeaverRoot;
        public GameObject ConnectedRoot;
        public GameObject NotConnectedRoot;
        public GameObject TabBarRoot;
        public GameObject LoadingSpinner;
        public TextMeshProUGUI WalletPubKeyText;

        void Start()
        {
            PhantomLoginButton.onClick.AddListener(OnPhantomButtonClicked);
            DevnetInGameWalletButton.onClick.AddListener(OnDevnetInGameWalletButtonClicked);
            GetNFtsDataButton.onClick.AddListener(OnGetNftButtonClicked);
            GetNFtsNotCachedButton.onClick.AddListener(OnNFtsNotCachedButtonClicked);
            GetBeaverButton.onClick.AddListener(OnGetBeaverButtonClicked);
            MintInAppButton.onClick.AddListener(OnMintInAppButtonClicked);
#if UNITY_IOS
            // Not allowed on ios 
            GetBeaverButton.gameObject.SetActive(false);
            GetSolPlayTokenButton.gameObject.SetActive(false);
            PhantomTransactionButton.gameObject.SetActive(false);
#endif
            GetSolPlayTokenButton.onClick.AddListener(OnGetSolPlayTokenButtonClicked);
            PhantomTransactionButton.onClick.AddListener(OnPhantomTransactionButtonClicked);

            ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<NftArrivedMessage>(OnNftArrivedMessage);
            ServiceFactory.Instance.Resolve<MessageRouter>()
                .AddHandler<NftLoadingStartedMessage>(OnNftLoadingStartedMessage);
            ServiceFactory.Instance.Resolve<MessageRouter>()
                .AddHandler<NftLoadingFinishedMessage>(OnNftLoadingFinishedMessage);

            ConnectedRoot.gameObject.SetActive(false);
            NotConnectedRoot.gameObject.SetActive(true);
            TabBarRoot.gameObject.SetActive(false);

            UpdateBeaverStatus();
        }

        private async void OnMintInAppButtonClicked()
        {
            // Mint a SolAndy NFT
           var signature = await ServiceFactory.Instance.Resolve<NftMintingService>().MintNftWithMetaData("https://shdw-drive.genesysgo.net/4JaYMUSY8f56dFzmdhuzE1QUqhkJYhsC6wZPaWg9Zx7f/manifest.json", "SolAndy", "SolPlay");
           await RequestNfts(true);
        }

        private async void OnDevnetInGameWalletButtonClicked()
        {
            var account = await ServiceFactory.Instance.Resolve<WalletHolderService>().Login(true);
            WalletPubKeyText.text = account.PublicKey;
            ConnectedRoot.gameObject.SetActive(true);
            NotConnectedRoot.gameObject.SetActive(false);
            TabBarRoot.gameObject.SetActive(true);
            await RequestNfts(true);
        }

        private void OnPhantomTransactionButtonClicked()
        {
            var phantomDeeplinkService = ServiceFactory.Instance.Resolve<TransactionService>();
            phantomDeeplinkService.TransferSolanaToPubkey(phantomDeeplinkService.EditorExampleWalletPublicKey);
        }

        private void OnGetSolPlayTokenButtonClicked()
        {
            // To let people buy a token just put the direct raydium link to your token and open it with a phantom deeplink. 
            OpenUrlInWalletBrowser(
                "https://raydium.io/swap/?inputCurrency=sol&outputCurrency=PLAyKbtrwQWgWkpsEaMHPMeDLDourWEWVrx824kQN8P&inputAmount=0.1&outputAmount=0.9&fixed=in");
        }

        private void OnGetBeaverButtonClicked()
        {
            // Here you can just open the link to your minting page within phantom mobile browser
            OpenUrlInWalletBrowser("https://beavercrush.com");
        }

        public void OpenUrlInWalletBrowser(string url)
        {
#if UNITY_IOS || UNITY_ANROID
            string refUrl = UnityWebRequest.EscapeURL("SolPlay");
            string escapedUrl = UnityWebRequest.EscapeURL(url);
            string inWalletUrl = $"https://phantom.app/ul/browse/{url}?ref=refUrl";
#else
            string inWalletUrl = url;
#endif
            Application.OpenURL(inWalletUrl);
        }

        private void OnNftArrivedMessage(NftArrivedMessage message)
        {
            NftItemListView.AddNFt(message.NewNFt);
            UpdateBeaverStatus();
        }

        private bool UpdateBeaverStatus()
        {
            var nftService = ServiceFactory.Instance.Resolve<NftService>();
            bool ownsBeaver = nftService.OwnsNftOfMintAuthority(NftService.BeaverNftMintAuthority);
            YouDontOwnABeaverRoot.gameObject.SetActive(!ownsBeaver);
            YouOwnABeaverRoot.gameObject.SetActive(ownsBeaver);
            return ownsBeaver;
        }

        private async void OnGetNftButtonClicked()
        {
            await RequestNfts(true);
        }

        private async void OnNFtsNotCachedButtonClicked()
        {
            await RequestNfts(false);
        }

        private void OnNftLoadingStartedMessage(NftLoadingStartedMessage message)
        {
            NftItemListView.Clear();
            GetNFtsDataButton.interactable = false;
            GetNFtsNotCachedButton.interactable = false;
        }

        private void OnNftLoadingFinishedMessage(NftLoadingFinishedMessage message)
        {
            NftItemListView.UpdateContent();
        }

        private void Update()
        {
            var nftService = ServiceFactory.Instance.Resolve<NftService>();
            if (nftService != null)
            {
                GetNFtsDataButton.interactable = !nftService.IsLoadingTokenAccounts;
                GetNFtsNotCachedButton.interactable = !nftService.IsLoadingTokenAccounts;
                LoadingSpinner.gameObject.SetActive(nftService.IsLoadingTokenAccounts);
            }
        }

        private async Task RequestNfts(bool tryUseLocalCache)
        {
            var phantomDeeplinkService = ServiceFactory.Instance.Resolve<WalletHolderService>();
            if (phantomDeeplinkService.TryGetPhantomPublicKey(out string phantomPublicKey))
            {
                await ServiceFactory.Instance.Resolve<NftService>()
                    .RequestNftsFromPublicKey(phantomPublicKey, tryUseLocalCache);
            }
        }

        private async void OnPhantomButtonClicked()
        {
            var account = await ServiceFactory.Instance.Resolve<WalletHolderService>().Login(false);
            WalletPubKeyText.text = account.PublicKey;
            ConnectedRoot.gameObject.SetActive(true);
            NotConnectedRoot.gameObject.SetActive(false);
            TabBarRoot.gameObject.SetActive(true);
            await RequestNfts(true);
        }
    }
}