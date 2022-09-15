using System.Collections;
using DG.Tweening;
using Frictionless;
using Solana.Unity.Rpc.Models;
using SolPlay.CustomSmartContractExample;
using SolPlay.Deeplinks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] GameMode _gameMode;
    [SerializeField] GameSaver _gameSaver;
    [SerializeField] MedalHud _medalHud;
    [SerializeField] TextMeshProUGUI _scoreText;
    [SerializeField] TextMeshProUGUI _highScoreText;
    [SerializeField] TextMeshProUGUI _costText;
    [SerializeField] GameObject  _newHud;
    [SerializeField] FadeScreen _fadeScreen;
    [SerializeField] XpWidget _xpWidget;
    [SerializeField] Button _submitHighscoreButton;
    [SerializeField] NftItemView _nftItemView;
    
    [Header("Containers")]
    [SerializeField] CanvasGroup _gameOverContainer;
    [SerializeField] CanvasGroup _statsContainer;

    [Header("GameOver Tween")]
    [SerializeField] Transform _gameOverReference;
    [SerializeField] float _gameOverAnimationTime = 1f;

    [Header("Stats Tween")]
    [SerializeField] Transform _statsReference;
    [SerializeField] float _statsAnimationDelay = 1f;
    [SerializeField] float _statsAnimationTime = 1f;

    [Header("Buttons Tween")]
    [SerializeField] Transform _buttonsReference;
    [SerializeField] float _buttonsAnimationDelay = 1f;
    [SerializeField] float _buttonsAnimationTime = 1f;  

    [Header("Audio")]
    [SerializeField] AudioFX _statsMoveAudio;
    [SerializeField] AudioFX _buttonsMoveAudio;

    private void Awake()
    {
        gameObject.SetActive(false);
        _submitHighscoreButton.onClick.AddListener(OnSubmitHighscoreButtonClicked);
    }

    private void Start()
    {
        ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<NftSelectedMessage>(OnNftSelectedMessage);
        ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<NewHighScoreLoadedMessage>(OnHighscoreLoadedMessage);

    }

    private void OnHighscoreLoadedMessage(NewHighScoreLoadedMessage message)
    {
        var nftService = ServiceFactory.Instance.Resolve<NftService>();
        if (nftService.SelectedNft != null)
        {
            _nftItemView.SetData(nftService.SelectedNft, view => { });

        }
    }

    private void OnNftSelectedMessage(NftSelectedMessage message)
    {
        _gameMode.RestartGame();
    }

    private void OnEnable()
    {
        UpdateHud();

        if(_fadeScreen != null)
            StartCoroutine(_fadeScreen.Flash());

        StartCoroutine(ShowUICoroutine());
    }

    public void Quit() => _gameMode.QuitGame();
    public void Restart() => _gameMode.RestartGame();

    private IEnumerator ShowUICoroutine()
    {
        _gameOverContainer.alpha = 0;
        _gameOverContainer.blocksRaycasts = false;

        _statsContainer.alpha = 0;
        _statsContainer.blocksRaycasts = false;

        yield return StartCoroutine(
                AnimateCanvasGroup(
                    _gameOverContainer,
                    _gameOverReference.position,
                    _gameOverContainer.transform.position,
                    _gameOverAnimationTime
                ));

        _statsMoveAudio.PlayAudio();

        StartCoroutine(
            AnimateCanvasGroup(
                _statsContainer,
                _statsReference.position,
                _statsContainer.transform.position,
                _statsAnimationTime
            ));

        yield return new WaitForSeconds(_buttonsAnimationDelay);

        _buttonsMoveAudio.PlayAudio();
    }

    private IEnumerator AnimateCanvasGroup(CanvasGroup group, Vector3 from, Vector3 to, float time)
    {
        group.alpha = 0;
        group.blocksRaycasts = false;

        Tween fade = group.DOFade(1, time);

        group.transform.localScale = Vector3.zero;
        group.transform.DOScale(Vector3.one, 0.5f);

        yield return fade.WaitForKill();

        group.blocksRaycasts = true;
    }

    private async void OnSubmitHighscoreButtonClicked()
    {
        _submitHighscoreButton.interactable = false;

        var smartContractService = ServiceFactory.Instance.Resolve<HighscoreService>();
        var nftService = ServiceFactory.Instance.Resolve<NftService>();
        var customSmartContractService = smartContractService;
        await customSmartContractService.SafeHighScore(nftService.SelectedNft, (uint) _gameMode.Score);
        AccountInfo account = await smartContractService.GetHighscoreAccountData(nftService.SelectedNft);
    }

    private void UpdateHud()
    {
        var highscoreService = ServiceFactory.Instance.Resolve<HighscoreService>();
        var nftService = ServiceFactory.Instance.Resolve<NftService>();
        
        int score = _gameMode.Score;
        int curHighScore = 0;
        bool hasHighscoreSaved = highscoreService.TryGetCurrentHighscore(out HighscoreEntry savedHighscore);
        if (hasHighscoreSaved)
        {
            curHighScore = (int) savedHighscore.Highscore;
        }

        if (nftService.SelectedNft != null)
        {
            _nftItemView.gameObject.SetActive(true);
            _nftItemView.SetData(nftService.SelectedNft, view => { });
        }
        else
        {
            _nftItemView.gameObject.SetActive(false);
        }
        
        var newHighscoreReached = score > curHighScore;
        int newHighScore = newHighscoreReached ? score : curHighScore;

        if (curHighScore == 0)
        {
            _costText.text = "0.00191 sol";
        }
        else
        {
            _costText.text = "0.001 sol";
        }
        _submitHighscoreButton.interactable = newHighscoreReached && savedHighscore.AccountLoaded;
#if UNITY_EDITOR
        _submitHighscoreButton.interactable = true;
#endif
        _medalHud.HandleScore(score);
        _scoreText.SetText(score.ToString());
        _highScoreText.SetText(newHighScore.ToString());

        _newHud.SetActive(newHighscoreReached);
    }
}