using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    private static BackgroundMusicManager instance;
    public AudioSource audioSource;
    public AudioClip menuMusicClip;
    public AudioClip shopMusicClip;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Load the shop background music clip
        shopMusicClip = Resources.Load<AudioClip>("MultiGameSound/ShopBgmClip");
        if (shopMusicClip == null)
        {
            Debug.LogError("ShopBgmClip not found in Resources/MultiGameSound");
        }
    }

    private void Start()
    {
        PlayMenuMusic();
    }

    public void PlayMenuMusic()
    {
        PlayMusic(menuMusicClip);
    }

    public void PlayShopMusic()
    {
        PlayMusic(shopMusicClip);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (audioSource.clip == clip)
        {
            return;
        }
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void StopMusic()
    {
        audioSource.Stop();
    }

    public static BackgroundMusicManager Instance
    {
        get
        {
            return instance;
        }
    }
}
