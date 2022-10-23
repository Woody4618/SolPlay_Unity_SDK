using Frictionless;
using SolPlay.Deeplinks;
using UnityEngine;
using UnityEngine.UI;

public class LoginScreen : MonoBehaviour
{
    public Button PhantomLoginButton;
    public Button DevnetInGameWalletButton;
    public Button YoutubeButton;
    public Button RepositoryButton;
    public GameObject ConnectedRoot;
    public GameObject NotConnectedRoot;
    public GameObject TabBarRoot;

    private void Awake()
    {
        YoutubeButton.onClick.AddListener(OnYoutubeButtonClicked);
        RepositoryButton.onClick.AddListener(OnRepositoryButtonClicked);
        PhantomLoginButton.onClick.AddListener(OnPhantomButtonClicked);
        DevnetInGameWalletButton.onClick.AddListener(OnDevnetInGameWalletButtonClicked);
    }

    private void Start()
    {
        ConnectedRoot.gameObject.SetActive(false);
        NotConnectedRoot.gameObject.SetActive(true);
        if (TabBarRoot != null)
        {
            TabBarRoot.gameObject.SetActive(false);
        }
    }
    
    private void OnRepositoryButtonClicked()
    {
        Application.OpenURL("https://github.com/Woody4618/SolanaUnityDeeplinkExample");
    }

    private void OnYoutubeButtonClicked()
    {
        Application.OpenURL("https://www.youtube.com/watch?v=mS5Fx_yzcHw&ab_channel=SolPlay");
    }
    
    private async void OnDevnetInGameWalletButtonClicked()
    {
        await ServiceFactory.Instance.Resolve<WalletHolderService>().Login(true);
        OnLogin();
    }

    private async void OnPhantomButtonClicked()
    {
        await ServiceFactory.Instance.Resolve<WalletHolderService>().Login(false);
        OnLogin();
    }

    private void OnLogin()
    {
        ConnectedRoot.gameObject.SetActive(true);
        NotConnectedRoot.gameObject.SetActive(false);
        if (TabBarRoot != null)
        {
            TabBarRoot.gameObject.SetActive(true);
        }
    }
}
