using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class HelpUI : MonoBehaviour
{
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

    private void OnEnable()
    {
        PlayButtonSound();
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
