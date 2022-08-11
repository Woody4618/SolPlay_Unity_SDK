using System.Threading.Tasks;
using Frictionless;
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
        public Button GetNFtsDataButton;
        public Button GetNFtsNotCachedButton;
        public Button GetBeaverButton;
        public Button GetSolPlayTokenButton;
        public Button PhantomTransactionButton;
        public NftItemListView NftItemListView;
        public GameObject YouDontOwnABeaverRoot;
        public GameObject YouOwnABeaverRoot;
        public GameObject ConnectedRoot;
        public GameObject LoadingSpinner;
        public NftItemView ownedBeaverNftItemView;
        public TextMeshProUGUI BeaverNameText;

        void Start()
        {
            PhantomLoginButton.onClick.AddListener(OnPhantomButtonClicked);
            GetNFtsDataButton.onClick.AddListener(OnGetNftButtonClicked);
            GetNFtsNotCachedButton.onClick.AddListener(OnNFtsNotCachedButtonClicked);
            GetBeaverButton.onClick.AddListener(OnGetBeaverButtonClicked);
            GetSolPlayTokenButton.onClick.AddListener(OnGetSolPlayTokenButtonClicked);
            PhantomTransactionButton.onClick.AddListener(OnPhantomTransactionButtonClicked);

            ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<NftArrivedMessage>(OnNftArrivedMessage);
            ServiceFactory.Instance.Resolve<MessageRouter>()
                .AddHandler<NftLoadingStartedMessage>(OnNftLoadingStartedMessage);
            ServiceFactory.Instance.Resolve<MessageRouter>()
                .AddHandler<NftLoadingFinishedMessage>(OnNftLoadingFinishedMessage);
            
            ConnectedRoot.gameObject.SetActive(false);
            PhantomLoginButton.gameObject.SetActive(true);

            UpdateBeaverStatus();
        }

        private void OnPhantomTransactionButtonClicked()
        {
            var phantomDeeplinkService = ServiceFactory.Instance.Resolve<PhantomDeeplinkService>();
            phantomDeeplinkService.TransferSolanaToPubkey(phantomDeeplinkService.EditorExampleWalletPublicKey);
        }

        private void OnGetSolPlayTokenButtonClicked()
        {
            // To let people buy a token just put the direct raydium link to your token and open it with a phantom deeplink. 
            ServiceFactory.Instance.Resolve<WalletHolderService>().DeeplinkWallet.OpenUrlInWalletBrowser(
                "https://raydium.io/swap/?inputCurrency=sol&outputCurrency=PLAyKbtrwQWgWkpsEaMHPMeDLDourWEWVrx824kQN8P&inputAmount=0.1&outputAmount=0.9&fixed=in");
        }

        private void OnGetBeaverButtonClicked()
        {
            // Here you can just open the link to your minting page within phantom mobile browser
            ServiceFactory.Instance.Resolve<WalletHolderService>().DeeplinkWallet
                .OpenUrlInWalletBrowser("https://beavercrush.com");
        }


        private void OnNftArrivedMessage(NftArrivedMessage message)
        {
            NftItemListView.AddNFt(message.NewNFt);

            var nftService = ServiceFactory.Instance.Resolve<NftService>();
            var ownsBeaver = UpdateBeaverStatus();
            if (ownsBeaver)
            {
                var allBeavers = nftService.GetAllNftsByMintAuthority(NftService.BeaverNftMintAuthority);
                var metaPlexNft = allBeavers[0];
                ownedBeaverNftItemView.SetData(metaPlexNft,
                    view => { ServiceFactory.Instance.Resolve<NftContextMenu>().Open(ownedBeaverNftItemView); });
                BeaverNameText.text = metaPlexNft.MetaplexData.data.name;
            }
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
            // 
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
            var account = await ServiceFactory.Instance.Resolve<WalletHolderService>().Login();
            ConnectedRoot.gameObject.SetActive(true);
            PhantomLoginButton.gameObject.SetActive(false);
            await RequestNfts(true);
        }
    }
}