using TMPro;
using UnityEngine;

public class InGameHudScreen : MonoBehaviour
{
    [SerializeField] private GameMode _gameMode;
    [SerializeField] private TextMeshProUGUI _scoreText;

    public void PauseGame() => _gameMode.PauseGame();

    private void LateUpdate()
    {
        _scoreText.text = _gameMode.Score.ToString();
    }
}
