using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : Singleton<AudioManager> {
    [HideInInspector] public AudioSource listener;
    [SerializeField] AudioSource bgm;
    [SerializeField] Slider volumeSlider;

    protected override void Awake() {
        base.Awake();
        listener = GetComponent<AudioSource>();
    }

    private void Start() {
        volumeSlider.onValueChanged.AddListener(SetVolume);
        SetVolume(volumeSlider.value);
    }

    void SetVolume(float volume) {
        listener.volume = volume;
        bgm.volume = volume;
    }
}
