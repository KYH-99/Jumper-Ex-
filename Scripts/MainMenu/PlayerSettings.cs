using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public float BGMVolume
    {
        get { return PlayerPrefs.GetFloat("BGMVolume", 0.75f); }
        set
        {
            PlayerPrefs.SetFloat("BGMVolume", value);
            AudioListener.volume = value;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ApplySettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ApplySettings()
    {
        AudioListener.volume = BGMVolume;
    }
}