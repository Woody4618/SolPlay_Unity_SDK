using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitingScreen : MonoBehaviour
{
    [SerializeField] GameMode _gameMode;
    [SerializeField] float _timeToWait;

    private void OnEnable() 
    {
        _gameMode.StartWithDelay(_timeToWait);
    }
}
