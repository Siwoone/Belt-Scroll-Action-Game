using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SoundManager))]
public class SoundManager : MonoBehaviour
{

    //사운드를 재생할 오디오 소스 컴포넌트
    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    //사운드 파일
    [Header("Audio Clips")]
    public AudioClip[] bgmClips;
    public AudioClip[] sfxClips;

    //사운드 볼륨
    [Header("Volume")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;     //마스터 볼륨
    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;      //BGM 볼륨
    [Range(0f, 1f)]
    public float sfxVolume = 0.5f;      //효과음 볼륨

    //딕셔너리로 각 사운드 클립 저장
    Dictionary<string, AudioClip[]> bgmDictionary = new Dictionary<string, AudioClip[]>();
    Dictionary<string, AudioClip[]> sfxDictionary = new Dictionary<string, AudioClip[]>();

    public static SoundManager Instance;

    //싱글톤
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        AddBGM("Audio Clips", bgmClips);
        AddSFX("Audio Clips", sfxClips);

        //볼륨 초기화
        ApplyAllVolumes();
    }

    //BGM 파일 재생
    public void PlayBGM(string name, int index)
    {
        //null 체크
        if (!bgmDictionary.TryGetValue(name, out AudioClip[] clips)) return;
        if (clips == null) return;
        if (index < 0 || index >= clips.Length) return;


        //BGM 재생
        bgmSource.clip = clips[index];
        bgmSource.loop = true;
        bgmSource.Play();
    }

    //BGM 파일 정지
    public void StopBGM()
    {
        //null 체크
        if (bgmSource == null) return;

        //BGM이 재생중이라면 정지
        if (bgmSource != null && bgmSource.isPlaying) bgmSource.Stop();
    }

    //BGM이 재생 중인지 확인
    public bool isPlaying()
    {
        //return bgmSource != null && bgmSource.isPlaying;
        //null 체크
        if (bgmSource == null) return false;

        //BGM이 재생 중이면 true 반환
        if (bgmSource.isPlaying) return true;
        else return false;
    }

    //SFX 파일 재생
    public void PlaySFX(string name, int index)
    {
        //null 체크
        if (!sfxDictionary.TryGetValue(name,out AudioClip[] clips)) return;
        if (clips == null) return;
        if (index < 0 || index >= clips.Length) return;

        ////SFX 재생
        //sfxSource.clip = clips[index];

        ////효과음은 1번만 재생
        //sfxSource.loop = false;
        //sfxSource.Play();
        sfxSource.PlayOneShot(clips[index]);
    }
    
    //BGM 추가
    void AddBGM(string name, AudioClip[] clips)
    {
        if (bgmDictionary.ContainsKey(name))
        {
            bgmDictionary[name] = clips;
        }

        else
        {
            bgmDictionary.Add(name, clips);
        }
    }

    //SFX 추가
    void AddSFX(string name, AudioClip[] clips)
    {
        if (sfxDictionary.ContainsKey(name))
        {
            sfxDictionary[name] = clips;
        }

        else
        {
            sfxDictionary.Add(name, clips);
        }
    }

    //볼륨 설정을 모든 오디오 파일에 적용
    private void ApplyAllVolumes()
    {
        //BGM 볼륨
        if (bgmSource != null)
        {
            bgmSource.volume = masterVolume * bgmVolume;
        }

        //SFX 볼륨
        if (bgmSource != null)
        {
            sfxSource.volume = masterVolume * sfxVolume;
        }
    }
}
