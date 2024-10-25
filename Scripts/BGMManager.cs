using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    private static BGMManager instance;
    private AudioSource audioSource;
    public AudioClip mainMenuBGM;

    public static BGMManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BGMManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("BGMManager");
                    instance = go.AddComponent<BGMManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            DontDestroyOnLoad(gameObject);

            // Load the main menu BGM at the start
            mainMenuBGM = Resources.Load<AudioClip>("MultiGameSound/MainMenuBgmClip");
            PlayBGM(mainMenuBGM);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SingleScene")
        {
            Destroy(gameObject);
        }
        else if (scene.name == "MainMenu")
        {
            PlayBGM(mainMenuBGM);
        }
    }

    public void PlayBGM(AudioClip bgm)
    {
        if (audioSource.clip != bgm)
        {
            audioSource.clip = bgm;
            audioSource.Play();
        }
    }

    public void StopBGM()
    {
        audioSource.Stop();
        audioSource.clip = null;
    }
}
