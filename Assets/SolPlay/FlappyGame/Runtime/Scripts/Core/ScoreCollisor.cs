using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreCollisor : MonoBehaviour
{
    [SerializeField] GameMode _gameMode;
    [SerializeField] AudioClip _audioOnIncrement;

    public void IncrementScore()
    {
        _gameMode.IncrementScore();
        AudioUtility.PlaySFX(_audioOnIncrement);
    }
}
