using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseScreen : MonoBehaviour
{
    [SerializeField] GameMode _gameMode;

    public void ResumeGame() => _gameMode.ResumeGame();
}
