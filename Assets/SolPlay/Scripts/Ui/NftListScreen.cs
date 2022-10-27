using System.Threading.Tasks;
using Frictionless;
using SolPlay.DeeplinksNftExample.Scripts;
using SolPlay.Scripts.Services;
using UnityEngine;
using UnityEngine.UI;

namespace SolPlay.Scripts.Ui
{
    /// <summary>
    /// The main screen of the deeplinks example. Handles the login and different application states.
    /// </summary>
    public class NftListScreen : MonoBehaviour
    {
        public Button GetNFtsDataButton;
        public Button GetNFtsNotCachedButton;
        public Button GetBeaverButton;
        public Button MintInAppButton;
        public NftItemListView NftItemListView;
        public GameObject YouDontOwnABeaverRoot;
        public GameObject YouOwnABeaverRoot;
        public GameObject LoadingSpinner;

        async void Start()
        {
            GetNFtsDataButton.onClick.AddListener(OnGetNftButtonClicked);
            GetNFtsNotCachedButton.onClick.AddListener(OnNFtsNotCachedButtonClicked);
            GetBeaverButton.onClick.AddListener(OnGetBeaverButtonClicked);
            MintInAppButton.onClick.AddListener(OnMintInAppButtonClicked);
// #if UNITY_IOS
            // If you have minting of NFTs in your app it may be hard to get into the AppStore
            //GetBeaverButton.gameObject.SetActive(false);
// #endif

            MessageRouter.AddHandler<NftArrivedMessage>(OnNftArrivedMessage);
            MessageRouter
                .AddHandler<NftLoadingStartedMessage>(OnNftLoadingStartedMessage);
            MessageRouter
                .AddHandler<NftLoadingFinishedMessage>(OnNftLoadingFinishedMessage);
            MessageRouter
                .AddHandler<NftMintFinishedMessage>(OnNftMintFinishedMessage);
            MessageRouter
                .AddHandler<WalletLoggedInMessage>(OnWalletLoggedInMessage);

            if (ServiceFactory.Resolve<WalletHolderService>().IsLoggedIn)
            {
                await RequestNfts(true);
                UpdateBeaverStatus();
            }
        }

        private async void OnWalletLoggedInMessage(WalletLoggedInMessage message)
        {
            await OnLogin();
        }

        private async Task OnLogin()
        {
            await RequestNfts(true);
        }

        private async void OnMintInAppButtonClicked()
        {
            // Mint a baloon beaver
            var signature = await ServiceFactory.Resolve<NftMintingService>()
                .MintNftWithMetaData(
                    "https://shdw-drive.genesysgo.net/2TvgCDMEcSGnfuSUZNHvKpHL9Z5hLn19YqvgeUpS6rSs/manifest.json",
                    "Balloon Beaver", "Beaver");
            
            // Mint a solandy
            /*ServiceFactory.Resolve<LoggingService>().Log("Start minting a 'SolAndy' nft", true);
            var signature = await ServiceFactory.Resolve<NftMintingService>().MintNftWithMetaData("https://shdw-drive.genesysgo.net/4JaYMUSY8f56dFzmdhuzE1QUqhkJYhsC6wZPaWg9Zx7f/manifest.json", "SolAndy", "SolPlay");
            ServiceFactory.Resolve<TransactionService>().CheckSignatureStatus(signature,
                () =>
                {
                    RequestNfts(true);
                    ServiceFactory.Resolve<LoggingService>().Log("Mint Successfull! Woop woop!", true);
                });*/
            
            // Mint from a candy machine (This one is from zen republic, i only used it for testing)
            //var signature = await ServiceFactory.Resolve<NftMintingService>()
            //    .MintNFTFromCandyMachineV2(new PublicKey("3eqPffoeSj7e2ZkyHJHyYPc7qm8rbGDZFwM9oYSW4Z5w"));
        }

        private void OnGetBeaverButtonClicked()
        {
            // Here you can just open the link to your minting page within phantom mobile browser
            PhantomUtils.OpenUrlInWalletBrowser("https://beavercrush.com");
        }

        private void OnNftArrivedMessage(NftArrivedMessage message)
        {
            NftItemListView.AddNFt(message.NewNFt);
            UpdateBeaverStatus();
        }

        private bool UpdateBeaverStatus()
        {
            var nftService = ServiceFactory.Resolve<NftService>();
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

        private void OnNftMintFinishedMessage(NftMintFinishedMessage message)
        {
            RequestNfts(true);
        }

        private void Update()
        {
            var nftService = ServiceFactory.Resolve<NftService>();
            if (nftService != null)
            {
                GetNFtsDataButton.interactable = !nftService.IsLoadingTokenAccounts;
                GetNFtsNotCachedButton.interactable = !nftService.IsLoadingTokenAccounts;
                LoadingSpinner.gameObject.SetActive(nftService.IsLoadingTokenAccounts);
            }
        }

        private async Task RequestNfts(bool tryUseLocalCache)
        {
            var phantomDeeplinkService = ServiceFactory.Resolve<WalletHolderService>();
            if (phantomDeeplinkService.TryGetPhantomPublicKey(out string phantomPublicKey))
            {
                await ServiceFactory.Resolve<NftService>()
                    .RequestNftsFromPublicKey(phantomPublicKey, tryUseLocalCache);
            }
        }
    }
}