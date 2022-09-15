using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioUtility
{
    public static AudioHandler AudioHandler { private get; set; }

    public static void PlayMusic(AudioClip clip)
    {
        if(AudioHandler != null && clip != null)
            AudioHandler.PlayMusic(clip);
    } 

    public static void PlaySFX(AudioClip clip)
    {
        if(AudioHandler != null && clip != null)
            AudioHandler.PlaySFX(clip);
    } 
}
