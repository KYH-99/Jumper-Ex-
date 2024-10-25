using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    private BGMManager bgmManager;
    private AudioSource audioSource;
    public AudioClip btnSoundClip;

    private void Awake()
    {
        bgmManager = BGMManager.Instance;
        if (bgmManager == null)
        {
            Debug.LogError("BGMManager not found in the scene.");
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        btnSoundClip = Resources.Load<AudioClip>("MultiGameSound/BtnsoundClip");

        if (btnSoundClip == null)
        {
            Debug.LogError("BtnsoundClip not found in Resources/MultiGameSound");
        }
    }

    public void ShopChange()
    {
        PlayButtonSound();
        StartCoroutine(LoadSceneAfterSound("ShopScene", true));
    }

    public void MainChange()
    {
        PlayButtonSound();
        StartCoroutine(LoadSceneAfterSound("MainMenu"));
    }

    public void SingleChange()
    {
        PlayButtonSound();
        StartCoroutine(LoadSceneAfterSound("SingleScence", true));
    }

    public void OnlineLobbyChange()
    {
        PlayButtonSound();
        StartCoroutine(LoadSceneAfterSound("OnlineLobbyScene"));
    }

    private void PlayButtonSound()
    {
        if (audioSource != null && btnSoundClip != null)
        {
            audioSource.PlayOneShot(btnSoundClip);
            Debug.Log("Playing button sound.");
        }
        else
        {
            Debug.LogError("AudioSource or btnSoundClip is missing.");
        }
    }

    private IEnumerator LoadSceneAfterSound(string sceneName, bool stopBGM = false)
    {
        // ��ư ���� Ŭ���� ���̸�ŭ ���
        yield return new WaitForSeconds(btnSoundClip.length);

        if (stopBGM && bgmManager != null)
        {
            bgmManager.StopBGM();
        }

        SceneManager.LoadScene(sceneName);
    }
}
