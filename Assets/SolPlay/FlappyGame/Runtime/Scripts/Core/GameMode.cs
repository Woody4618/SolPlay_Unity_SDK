using System.Collections;
using Frictionless;
using SolPlay.Deeplinks;
using UnityEngine;
using UnityEditor;

public class GameMode : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private PlayerDeathController _playerDeathController;
    [SerializeField] private EndlessPipeGenerator _pipeGenerator;
    [SerializeField] private ScreenController _screenController;
    [SerializeField] private GameSaver _gameSaver;
    [SerializeField] private AudioHandler _audioHandler;
    [SerializeField] private GroundParallax _groundParallax;
    [SerializeField] private PlayerFollow _playerFollow;

    [Header("Fade")] [SerializeField] private FadeScreen _fadeScreen;
    [SerializeField] private float _fadeTime = 0.05f;
    [SerializeField] private Color _fadeColor = Color.black;

    [Header("Data")] [SerializeField] private PlayerParameters _gameWaitingParameters;
    [SerializeField] private PlayerParameters _gameRunningParameters;
    [SerializeField] private PlayerParameters _gameOverParameters;

    [field: SerializeField] public int Score { get; private set; }
    [field: SerializeField] public int Boost { get; private set; }


    private void Awake()
    {
        _playerController.MovementParameters = _gameWaitingParameters;
        _playerController.enabled = false;

        AudioUtility.AudioHandler = _audioHandler;

        StartCoroutine(_fadeScreen.FadeOut(_fadeTime, _fadeColor));
        _screenController.ShowStartHud();
    }

    private void Start()
    {
        ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<NftSelectedMessage>(OnNftSelectedMessage);
        var selectedNft = ServiceFactory.Instance.Resolve<NftService>().SelectedNft;
        if (selectedNft != null)
        {
            _playerController.SetSpriteFromNft(selectedNft);
        }
    }

    private void OnNftSelectedMessage(NftSelectedMessage obj)
    {
        _playerController.Type = obj.NewNFt.MetaplexData.data.symbol == "ORCANAUT" ? PlayerController.NftType.Water : PlayerController.NftType.Default;
        _playerController.SetSpriteFromNft(obj.NewNFt);
        _playerFollow.UpdateBackgrounds(0);
    }

    public void StartWaiting()
    {
        _playerController.MovementParameters = _gameWaitingParameters;
        _screenController.ShowWaitingHud();
    }

    public void StartWithDelay(float delay)
    {
        StartCoroutine(StartGameRoutine(delay));
    }

    private IEnumerator StartGameRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartGame();
    }

    public void StartGame()
    {
        _playerController.Reset();
        _playerController.enabled = true;
        _playerController.MovementParameters = _gameRunningParameters;
        _playerController.Flap();
        _pipeGenerator.StartSpawn();
        _screenController.ShowInGameHud();
        ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new ScoreChangedMessage()
        {
            NewScore = Score
        });
    }

    public void GameOver()
    {
        _playerController.MovementParameters = _gameOverParameters;
        _screenController.ShowGameOverHud();
        HandleNewScore();
    }

    public void RestartGame()
    {
        StartCoroutine(RestartGameCoroutine());
    }

    private IEnumerator RestartGameCoroutine()
    {
        _playerController.gameObject.SetActive(false);
        _playerController.MovementParameters = _gameWaitingParameters;
        yield return StartCoroutine(_fadeScreen.FadeIn(_fadeTime, _fadeColor));
        _playerController.Reset();
        _playerDeathController.Reset();
        _pipeGenerator.Reset();
        _groundParallax.Reset();
        _screenController.ShowStartHud();
        Score = 0;
        Boost = 0;
        ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new ScoreChangedMessage()
        {
            NewScore = Score
        });
        yield return StartCoroutine(_fadeScreen.FadeOut(_fadeTime, _fadeColor));
        _playerController.gameObject.SetActive(true);
    }

    private void HandleNewScore()
    {
        int highScore = _gameSaver.CurrentSave.HighestScore;

        if (Score > highScore)
        {
            _gameSaver.SaveGame(new SaveGameData() {HighestScore = Score});
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0;
        _screenController.ShowPauseHud();
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
        _screenController.ShowInGameHud();
    }

    public void IncrementScore()
    {
        var nftService = ServiceFactory.Instance.Resolve<NftService>();
        int totalScoreIncrease = 0;
        if (nftService.SelectedNft != null && nftService.IsBeaverNft(nftService.SelectedNft))
        {
            totalScoreIncrease += 2;
        }
        else
        {
            totalScoreIncrease++;
        }

        Score += totalScoreIncrease;

        ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new ScoreChangedMessage()
        {
            NewScore = Score
        });
    }

    public void IncrementBoost()
    {
        Boost++;
        Score += Boost;
        ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new BlimpSystem.ShowBlimpMessage($"+{Boost}", BlimpSystem.BlimpType.Boost));
    }

    public void StopBoost()
    {
        Boost = 0;
        ServiceFactory.Instance.Resolve<MessageRouter>().RaiseMessage(new BlimpSystem.ShowBlimpMessage($"missed", BlimpSystem.BlimpType.Boost));
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}

public class ScoreChangedMessage
{
    public int NewScore;
}