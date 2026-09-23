using System;
using ProjectP.Data;
using UnityEngine;

namespace ProjectP.Core
{
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

        /// <summary>설정 볼륨을 다시 적용한다. 설정 화면에서 값을 바꾼 뒤 호출한다.</summary>
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
