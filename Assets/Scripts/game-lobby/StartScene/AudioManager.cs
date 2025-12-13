using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;           // 🌟 단발성 SFX (버튼, PlaySfx) 전용
    [SerializeField] private AudioSource timerSource;         // 🌟 타이머 틱톡 (StartTimerTickSfx) 전용
    [SerializeField] private AudioSource judgmentSource;      // 🌟 심판 SFX (StartJudgmentSfx) 전용

    [Header("Clips")]
    [SerializeField] private AudioClip titleToLobbyBgm;
    [SerializeField] private AudioClip timerTickClip;
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
            l.enabled = false;
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
        // 🌟 4개의 AudioSource가 필요합니다: BGM, SFX(단발), Timer(루프), Judgment(심판)
        AudioSource[] sources = GetComponents<AudioSource>();

        while (sources.Length < 4)
        {
            gameObject.AddComponent<AudioSource>();
            sources = GetComponents<AudioSource>();
        }

        bgmSource = sources[0];
        sfxSource = sources[1];         // 단발성 SFX용
        timerSource = sources[2];       // 타이머 루프용
        judgmentSource = sources[3];    // 심판 SFX용

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

    // 🌟 sfxSource를 사용하여 단발성 효과음을 재생합니다. (충돌 없음)
    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }

    // 🌟 timerSource를 사용하여 루프 타이머 틱톡을 재생합니다.
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

    // 🌟 timerSource를 정지합니다.
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

    // 🌟 judgmentSource를 사용하여 심판 사운드를 재생합니다.
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

    // 🌟 judgmentSource를 정지합니다.
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