using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioFX : MonoBehaviour
{
    [SerializeField] AudioClip _clip;

    public void PlayAudio() => AudioUtility.PlaySFX(_clip);
}
