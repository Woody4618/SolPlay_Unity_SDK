using UnityEngine;
using UnityEngine.UI;

public class IntroductionComponent : MonoBehaviour
{
    public Button YoutubeButton;
    public Button RepositoryButton;

    private void Awake()
    {
        YoutubeButton.onClick.AddListener(OnYoutubeButtonClicked);
        RepositoryButton.onClick.AddListener(OnRepositoryButtonClicked);
    }

    private void OnRepositoryButtonClicked()
    {
        Application.OpenURL("https://github.com/Woody4618/SolanaUnityDeeplinkExample");
    }

    private void OnYoutubeButtonClicked()
    {
        Application.OpenURL("https://www.youtube.com/channel/UC517QSv61gMaABWIJ412_Lw");
    }
}
