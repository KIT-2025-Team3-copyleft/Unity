using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip titleToLobbyBgm;
    [SerializeField] private float bgmVolume = 0.6f;

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
        // 씬 로드 직후 카메라/리스너가 생성되는 타이밍 때문에 1프레임 뒤 정리
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
            l.enabled = false; // 씬 카메라 리스너 비활성
        }

        // 볼륨이 씬에서 바뀌는 경우 대비
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
        // 자기 자신에 붙은 AudioSource만 사용(씬 다른 오브젝트 참조 방지)
        AudioSource[] sources = GetComponents<AudioSource>();

        while (sources.Length < 2)
        {
            gameObject.AddComponent<AudioSource>();
            sources = GetComponents<AudioSource>();
        }

        bgmSource = sources[0];
        sfxSource = sources[1];

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }


    public void PlayBgmIfNeeded(AudioClip clip, bool loop)
    {
        if (clip == null) return;

        EnsureAudioSources();

        // 같은 곡이 이미 재생 중이면 건드리지 않음 = 이어짐
        if (currentBgm == clip && bgmSource.isPlaying) return;

        currentBgm = clip;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;

        EnsureAudioSources();
        sfxSource.PlayOneShot(clip);
    }
}
