using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField]
    private Slider bgmSlider;
    
    private Animator animator;
    private AudioSource audioSource;
    public AudioClip btnSoundClip;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        btnSoundClip = Resources.Load<AudioClip>("MultiGameSound/BtnsoundClip");

        if (btnSoundClip == null)
        {
            Debug.LogError("BtnsoundClip not found in Resources/MultiGameSound");
        }
    }

    private void Start()
    {
        // 슬라이더 초기 값 설정
        bgmSlider.value = SettingsManager.Instance.BGMVolume;

        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
    }

    private void OnEnable()
    {
        PlayButtonSound();
    }

    public void SetBGMVolume(float volume)
    {
        SettingsManager.Instance.BGMVolume = volume;
    }

    public void Close()
    {
        PlayButtonSound();
        StartCoroutine(CloseAfterDelay());
    }

    private IEnumerator CloseAfterDelay()
    {
        animator.SetTrigger("Close");
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
        animator.ResetTrigger("Close");
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
}