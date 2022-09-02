using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartScreen : MonoBehaviour
{
    [SerializeField] GameMode _gameMode;

    public void StartGame() => _gameMode.StartWaiting();
}
