using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

// Every sound effect, the file in Resources/Sounds has the same name with spaces ("ShotBullet" is "Shot Bullet.wav")
public enum Sound
{
    ShotBullet,
    ShotMelee,
    ShotLaser,
    ShotExplosion,
    ShotBubble,
    UnitHit,
    EnemyPop,
    BossDeath,
    Summon,
    Stun,
    LifeLost,
    Place,
    Upgrade,
    Sell,
    Invalid,
    WaveStart,
    WaveComplete,
    GameOver,
    Win,
    Click,
    Buy
}

// Plays the sound effects and the background music. Made by itself when the game starts and kept when the scene
// loads again, so the music goes on after a restart. Also plays a click for every button that is pressed.
// The volumes are in the settings.
public class SoundManager : MonoBehaviour
{
    private const int SourceCount = 16;
    // A little higher or lower every time, so the same sound doesn't get boring
    private const float PitchVariation = 0.06f;

    private static SoundManager Instance;

    private AudioClip[] Clips;
    private float[] LastPlayed;
    private readonly AudioSource[] Sources = new AudioSource[SourceCount];
    private int NextSource;
    private readonly List<RaycastResult> UiHits = new List<RaycastResult>();
    private AudioSource MusicSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject managerObject = new GameObject("Sound Manager");
        DontDestroyOnLoad(managerObject);
        Instance = managerObject.AddComponent<SoundManager>();

        // Without a listener nothing can be heard, the scene doesn't have one
        if (FindAnyObjectByType<AudioListener>() == null)
        {
            managerObject.AddComponent<AudioListener>();
        }
    }

    void Awake()
    {
        int count = System.Enum.GetValues(typeof(Sound)).Length;
        Clips = new AudioClip[count];
        LastPlayed = new float[count];
        for (int i = 0; i < count; i++)
        {
            string fileName = System.Text.RegularExpressions.Regex.Replace(((Sound)i).ToString(), "(?<=[a-z])(?=[A-Z])", " ");
            Clips[i] = Resources.Load<AudioClip>("Sounds/" + fileName);
            LastPlayed[i] = -1f;
        }

        for (int i = 0; i < SourceCount; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            Sources[i] = source;
        }

        MusicSource = gameObject.AddComponent<AudioSource>();
        MusicSource.clip = Resources.Load<AudioClip>("Sounds/Music");
        MusicSource.loop = true;
        MusicSource.spatialBlend = 0f;
        UpdateMusicVolume();
        if (MusicSource.clip != null)
        {
            MusicSource.Play();
        }
        GameSettings.Changed += UpdateMusicVolume;
    }

    void OnDestroy()
    {
        GameSettings.Changed -= UpdateMusicVolume;
    }

    private void UpdateMusicVolume()
    {
        MusicSource.volume = GameSettings.MusicVolume;
    }

    public static void Play(Sound sound, float volume = 1f)
    {
        if (Instance != null)
        {
            Instance.PlaySound(sound, volume);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && IsOverButton())
        {
            PlaySound(Sound.Click, 1f);
        }
    }

    private void PlaySound(Sound sound, float volume)
    {
        AudioClip clip = Clips[(int)sound];
        if (clip == null)
        {
            return;
        }

        // Fast towers and many enemies at once would otherwise make a wall of noise
        float now = Time.unscaledTime;
        if (now - LastPlayed[(int)sound] < GetMinimumGap(sound))
        {
            return;
        }
        LastPlayed[(int)sound] = now;

        AudioSource source = Sources[NextSource];
        NextSource = (NextSource + 1) % SourceCount;
        source.Stop();
        source.clip = clip;
        source.volume = volume;
        source.pitch = 1f + Random.Range(-PitchVariation, PitchVariation);
        source.Play();
    }

    // Seconds the same sound has to wait before it plays again
    private static float GetMinimumGap(Sound sound)
    {
        switch (sound)
        {
            case Sound.ShotBullet:
            case Sound.ShotMelee:
            case Sound.ShotBubble:
            case Sound.UnitHit:
            case Sound.EnemyPop:
                return 0.06f;
            case Sound.ShotLaser:
            case Sound.ShotExplosion:
            case Sound.LifeLost:
            case Sound.Invalid:
                return 0.15f;
            default:
                return 0f;
        }
    }

    // A button, toggle or slider that can be used is under the mouse
    private bool IsOverButton()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        UiHits.Clear();
        EventSystem.current.RaycastAll(pointer, UiHits);
        if (UiHits.Count == 0)
        {
            return false;
        }

        Selectable selectable = UiHits[0].gameObject.GetComponentInParent<Selectable>();
        return selectable != null && selectable.IsInteractable();
    }
}
