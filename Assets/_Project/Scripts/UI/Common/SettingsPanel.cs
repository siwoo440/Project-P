using System;
using ProjectP.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectP.UI
{
    /// <summary>
    /// 공통 설정 창. 지금은 볼륨 3종만 다룬다. 그래픽·조작·언어 옵션은 이후 확장한다.
    /// 설정 원본을 직접 고치지 않고 AudioManager에 변경을 요청한다. CLAUDE.md 2장 절대 규칙 1
    /// 닫을 때 디스크에 저장한다.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TMP_Text masterValueText;
        [SerializeField] private TMP_Text bgmValueText;
        [SerializeField] private TMP_Text sfxValueText;
        [SerializeField] private Button closeButton;

        public event Action Closed;

        private bool listenersAdded;

        public void Open()
        {
            if (!listenersAdded) AddListeners();

            var audio = GameServices.Audio;
            ShowValue(masterSlider, masterValueText, audio.GetVolume(VolumeChannel.Master));
            ShowValue(bgmSlider, bgmValueText, audio.GetVolume(VolumeChannel.Bgm));
            ShowValue(sfxSlider, sfxValueText, audio.GetVolume(VolumeChannel.Sfx));

            gameObject.SetActive(true);
        }

        public void Close()
        {
            try
            {
                GameServices.Save.SaveSettings();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void AddListeners()
        {
            masterSlider.onValueChanged.AddListener(v => OnSliderChanged(VolumeChannel.Master, v, masterValueText));
            bgmSlider.onValueChanged.AddListener(v => OnSliderChanged(VolumeChannel.Bgm, v, bgmValueText));
            sfxSlider.onValueChanged.AddListener(v => OnSliderChanged(VolumeChannel.Sfx, v, sfxValueText));
            closeButton.onClick.AddListener(Close);
            listenersAdded = true;
        }

        private static void ShowValue(Slider slider, TMP_Text label, int value)
        {
            slider.SetValueWithoutNotify(value);
            label.text = value.ToString();
        }

        private static void OnSliderChanged(VolumeChannel channel, float value, TMP_Text label)
        {
            var volume = Mathf.RoundToInt(value);
            GameServices.Audio.SetVolume(channel, volume);
            label.text = volume.ToString();
        }
    }
}
