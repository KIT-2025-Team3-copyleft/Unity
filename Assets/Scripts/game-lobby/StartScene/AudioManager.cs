using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;           
    [SerializeField] private AudioSource timerSource;         
    [SerializeField] private AudioSource judgmentSource;      

    [Header("Clips")]
    [SerializeField] private AudioClip titleToLobbyBgm;
    [SerializeField] private AudioClip timerTickClip;

    [SerializeField] public AudioClip step1StartSfx;
    [SerializeField] public AudioClip step2StartSfx;
    [SerializeField] public AudioClip trialSuccessSfx;
    [SerializeField] public AudioClip trialFailSfx;
    [SerializeField] public AudioClip gameOverSfx;
    // 🌟 심판 클립 (UIManager/GameManager 호환성을 위해 필요)

    [SerializeField] private AudioClip lightningClip;
    [SerializeField] private AudioClip flowerClip;

    [Header("Volume")]
    [SerializeField] private float bgmVolume = 0.6f;
    [SerializeField] private float tickVolume = 0.8f;
    [SerializeField] private float judgmentVolume = 0.9f;

    private AudioClip currentBgm;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        EnsureManagerListener();

        bgmSource.volume = bgmVolume;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (I == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayBgmIfNeeded(titleToLobbyBgm, true);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GamePlay")
        {
            StopBGM();
        }

        StartCoroutine(DisableOtherListenersNextFrame());
    }

    private IEnumerator DisableOtherListenersNextFrame()
    {
        yield return null;

        EnsureManagerListener();

        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener l = listeners[i];
            if (l == null) continue;
            if (l.gameObject == gameObject) { l.enabled = true; continue; }

            if (l.gameObject.transform.root.name != "LocalPlayer") 
            {
                l.enabled = false;
            }
        }

        if (bgmSource != null) bgmSource.volume = bgmVolume;
    }

    private void EnsureManagerListener()
    {
        AudioListener al = GetComponent<AudioListener>();
        if (al == null) al = gameObject.AddComponent<AudioListener>();
        al.enabled = true;
    }

    private void EnsureAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        while (sources.Length < 4)
        {
            gameObject.AddComponent<AudioSource>();
            sources = GetComponents<AudioSource>();
        }

        bgmSource = sources[0];
        sfxSource = sources[1];         
        timerSource = sources[2];       
        judgmentSource = sources[3];    

        // BGM 설정
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;

        // 단발성 SFX 설정 (sfxSource)
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;

        // 타이머 SFX 설정 (timerSource)
        timerSource.playOnAwake = false;
        timerSource.loop = false;
        timerSource.spatialBlend = 0f;
        timerSource.volume = tickVolume;

        // 심판 SFX 설정 (judgmentSource)
        judgmentSource.playOnAwake = false;
        judgmentSource.loop = false;
        judgmentSource.spatialBlend = 0f;
        judgmentSource.volume = judgmentVolume;
    }


    public void PlayBgmIfNeeded(AudioClip clip, bool loop)
    {
        if (clip == null) return;

        if (currentBgm == clip && bgmSource.isPlaying) return;

        currentBgm = clip;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
            currentBgm = null;
            Debug.Log("[AudioManager] BGM 정지 완료.");
        }
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }

    public void StartTimerTickSfx()
    {
        if (timerTickClip == null || timerSource == null) return;

        if (timerSource.clip == timerTickClip && timerSource.loop == true && timerSource.isPlaying) return;

        timerSource.volume = tickVolume;
        timerSource.loop = true;
        timerSource.clip = timerTickClip;
        timerSource.Play();
        Debug.Log("[AudioManager] Timer Tick Sfx 재생 시작 (Loop).");
    }

    public void StopTimerTickSfx()
    {
        if (timerSource != null && timerSource.isPlaying && timerSource.loop == true)
        {
            timerSource.Stop();
            timerSource.loop = false;
            timerSource.volume = 1.0f;
            timerSource.clip = null;
            Debug.Log("[AudioManager] Timer Tick Sfx 정지 완료.");
        }
    }

    public void StartJudgmentSfx(string effectName)
    {
        StopJudgmentSfx();

        AudioClip clipToPlay = null;

        if (effectName == "LIGHTNING")
        {
            clipToPlay = lightningClip;
        }
        else if (effectName == "FLOWER")
        {
            clipToPlay = flowerClip;
        }

        if (clipToPlay != null && judgmentSource != null)
        {
            judgmentSource.clip = clipToPlay;
            judgmentSource.loop = true;
            judgmentSource.Play();
            Debug.Log($"[AudioManager] Judgment Sfx '{effectName}' 재생 시작 (Loop).");
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Judgment Clip for '{effectName}' not found or Judgment Source is null.");
        }
    }

    public void StopJudgmentSfx()
    {
        if (judgmentSource != null && judgmentSource.isPlaying)
        {
            judgmentSource.Stop();
            judgmentSource.loop = false;
            judgmentSource.clip = null;
            Debug.Log("[AudioManager] Judgment Sfx 정지 완료.");
        }
    }
}