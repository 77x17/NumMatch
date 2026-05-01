using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public enum AudioType
    {
        ChooseNumber,
        PairClear,
        Pop,
        RowClear,
        GemCollect,
        Write,
        Wrong
    }


    public AudioSource audioSource;
    public AudioClip chooseNumberSound, pairClearSound, pop2Sound, rowClearSound;
    public AudioClip gemCollectSound, writeSound, wrongSound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        
        // Nếu muốn tồn tại xuyên suốt các Scene:
        // DontDestroyOnLoad(this.gameObject); 
    }
    public void PlaySound(AudioType type)
    {
        AudioClip clipToPlay = null;

        switch (type)
        {
            case AudioType.ChooseNumber: clipToPlay = chooseNumberSound; break;
            case AudioType.PairClear:    clipToPlay = pairClearSound;    break;
            case AudioType.Pop:          clipToPlay = pop2Sound;         break;
            case AudioType.RowClear:     clipToPlay = rowClearSound;     break;
            case AudioType.GemCollect:   clipToPlay = gemCollectSound;   break;
            case AudioType.Write:        clipToPlay = writeSound;        break;
            case AudioType.Wrong:        clipToPlay = wrongSound;        break;
        }

        if (clipToPlay != null && audioSource != null)
        {
            // Dùng PlayOneShot để các âm thanh có thể phát đè lên nhau không bị ngắt
            audioSource.PlayOneShot(clipToPlay);
        }
    }
}