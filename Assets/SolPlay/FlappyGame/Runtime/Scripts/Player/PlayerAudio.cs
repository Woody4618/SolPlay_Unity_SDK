using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] AudioClip _flapAudio;
    [SerializeField] AudioClip _dieAudio;
    [SerializeField] AudioClip _hitGroundAudio;

    public void OnFlap() => AudioUtility.PlaySFX(_flapAudio);
    public void OnDie() => AudioUtility.PlaySFX(_dieAudio);
    public void OnHitGround() => AudioUtility.PlaySFX(_hitGroundAudio);
}
