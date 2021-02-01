using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoyAudio {
    private int channels = 2;
    private int freq = 44100;
    private int samplerate = 4096;
    private AudioSource audio;

    public GameBoyAudio() {
        initializeAPU();
    }

    private void initializeAPU() {
        GameObject gb = GameObject.Find("GameBoyCamera");
        gb.AddComponent(typeof(AudioListener));
        gb.AddComponent(typeof(AudioSource));
        AudioClip myClip = AudioClip.Create("GameBoyAudio", samplerate, channels, samplerate, false);
        audio = gb.GetComponent<AudioSource>();
        audio.clip = myClip;
    }
}
