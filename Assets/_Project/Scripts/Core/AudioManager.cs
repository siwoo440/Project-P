using System;
using ProjectP.Data;
using UnityEngine;

namespace ProjectP.Core
{
    public enum VolumeChannel
    {
        Master,
        Bgm,
        Sfx
    }

    /// <summary>
    /// BGM 1채널과 효과음 재생을 담당하고 설정의 볼륨을 적용한다.
    /// 실제 사운드 에셋과 세부 채널(UI·보이스·환경음)은 35일차에 확장한다.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private AudioSource bgmSource;
        private AudioSource sfxSource;
        private SettingsData settings;

        public void Initialize(SettingsData settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            ApplyVolume();
        }

        public int GetVolume(VolumeChannel channel) => channel switch
        {
            VolumeChannel.Master => settings.masterVolume,
            VolumeChannel.Bgm => settings.bgmVolume,
            VolumeChannel.Sfx => settings.sfxVolume,
            _ => throw new ArgumentOutOfRangeException(nameof(channel))
        };

        /// <summary>
        /// 볼륨(0~100)을 바꾸고 바로 적용한다. UI는 설정 원본을 직접 고치지 않고 이 메서드로 요청한다.
        /// 디스크 저장은 SaveManager.SaveSettings()로 따로 한다.
        /// </summary>
        public void SetVolume(VolumeChannel channel, int value)
        {
            value = Mathf.Clamp(value, 0, 100);
            switch (channel)
            {
                case VolumeChannel.Master: settings.masterVolume = value; break;
                case VolumeChannel.Bgm: settings.bgmVolume = value; break;
                case VolumeChannel.Sfx: settings.sfxVolume = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(channel));
            }

            ApplyVolume();
        }

        /// <summary>설정 볼륨을 다시 적용한다.</summary>
        public void ApplyVolume()
        {
            var master = settings.masterVolume / 100f;
            bgmSource.volume = master * settings.bgmVolume / 100f;
            sfxSource.volume = master * settings.sfxVolume / 100f;
        }

        public void PlayBgm(AudioClip clip)
        {
            if (clip == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            bgmSource.clip = clip;
            bgmSource.Play();
        }

        public void StopBgm() => bgmSource.Stop();

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip);
        }
    }
}
