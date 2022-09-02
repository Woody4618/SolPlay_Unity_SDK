using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] GameMode _gameMode;
    [SerializeField] GameSaver _gameSaver;
    [SerializeField] MedalHud _medalHud;
    [SerializeField] TextMeshProUGUI _scoreText;
    [SerializeField] TextMeshProUGUI _highScoreText;
    [SerializeField] GameObject  _newHud;
    [SerializeField] FadeScreen _fadeScreen;
    [SerializeField] XpWidget _xpWidget;
    
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

    private void UpdateHud()
    {
        int score = _gameMode.Score;
        int curHighScore = _gameSaver.CurrentSave.HighestScore;
        int newHighScore = score > curHighScore ? score : curHighScore;

        _medalHud.HandleScore(score);
        _scoreText.SetText(score.ToString());
        _highScoreText.SetText(newHighScore.ToString());

        var playerXp = PlayerPrefs.GetInt(XpWidget.PlayerPrefsXpKey);
        playerXp += score;
        
        _xpWidget.AnimateXp(playerXp, score);

        PlayerPrefs.SetInt(XpWidget.PlayerPrefsXpKey, playerXp);

        _newHud.SetActive(score > curHighScore);
    }
}