using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

//==============================================================================
// AudioSettingsController: 오디오 설정 적용 + Mixer + 포커스 음소거 책임
//==============================================================================
// - SettingsController의 자식 MonoBehaviour
// - SettingsData.Audio를 받아 AudioMixer/AudioListener에 반영
// - OnApplicationFocus에서 MuteOnFocusLost 자동 처리
//==============================================================================

public class AudioSettingsController : MonoBehaviour
{
    //--------------------------------------------------------------------------
    // [1] Inspector 연결
    //--------------------------------------------------------------------------

    [TabGroup("Mixer")]
    [Tooltip("오디오 믹서(선택). 미지정 시 AudioListener.volume만 적용.")]
    [SerializeField] private AudioMixer _audioMixer;

    [TabGroup("Mixer")]
    [Tooltip("AudioMixer의 마스터 볼륨 노출 파라미터 이름.")]
    [SerializeField] private string _masterVolumeParam = "MasterVolume";

    [TabGroup("Mixer")]
    [Tooltip("AudioMixer의 BGM 볼륨 노출 파라미터 이름.")]
    [SerializeField] private string _bgmVolumeParam = "BgmVolume";

    [TabGroup("Mixer")]
    [Tooltip("AudioMixer의 SFX 볼륨 노출 파라미터 이름.")]
    [SerializeField] private string _sfxVolumeParam = "SfxVolume";

    [TabGroup("Mixer")]
    [Tooltip("AudioMixer의 Voice 볼륨 노출 파라미터 이름.")]
    [SerializeField] private string _voiceVolumeParam = "VoiceVolume";

    [TabGroup("Mixer")]
    [Tooltip("AudioMixer의 UI 볼륨 노출 파라미터 이름.")]
    [SerializeField] private string _uiVolumeParam = "UiVolume";

    [TabGroup("Mixer")]
    [Tooltip("AudioMixer의 Ambient 볼륨 노출 파라미터 이름.")]
    [SerializeField] private string _ambientVolumeParam = "AmbientVolume";

    //--------------------------------------------------------------------------
    // [2] 캐시 (OnApplicationFocus에서 사용)
    //--------------------------------------------------------------------------

    /// <summary>가장 최근에 Apply된 데이터 참조(포커스 음소거 처리용).</summary>
    private SettingsData.Audio _current;

    //==========================================================================
    // [3] 시스템 적용
    //==========================================================================

    /// <summary>오디오 설정을 AudioMixer 또는 AudioListener에 반영.</summary>
    public void Apply(SettingsData.Audio a)
    {
        if (a == null) return;

        _current = a;

        float master = a.Mute ? 0f : Mathf.Clamp01(a.MasterVolume);

        if (_audioMixer != null)
        {
            TrySetMixerVolume(_masterVolumeParam, master);
            TrySetMixerVolume(_bgmVolumeParam, Mathf.Clamp01(a.BgmVolume));
            TrySetMixerVolume(_sfxVolumeParam, Mathf.Clamp01(a.SfxVolume));
            TrySetMixerVolume(_voiceVolumeParam, Mathf.Clamp01(a.VoiceVolume));
            TrySetMixerVolume(_uiVolumeParam, Mathf.Clamp01(a.UiVolume));
            TrySetMixerVolume(_ambientVolumeParam, Mathf.Clamp01(a.AmbientVolume));
        }
        else
        {
            // 폴백: AudioListener에 마스터만 반영
            AudioListener.volume = master;
        }
    }

    /// <summary>0~1 선형 볼륨을 dB로 변환해 AudioMixer 파라미터에 설정.</summary>
    private void TrySetMixerVolume(string param, float linear01)
    {
        if (_audioMixer == null || string.IsNullOrEmpty(param)) return;

        // 0이면 -80dB(거의 무음)로, 그 외에는 log10 변환
        float db = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
        _audioMixer.SetFloat(param, db);
    }

    //==========================================================================
    // [4] 포커스 변화 시 음소거
    //==========================================================================

    /// <summary>창 포커스 상실 시 MuteOnFocusLost가 켜져 있으면 즉시 음소거.</summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (_current == null) return;
        if (!_current.MuteOnFocusLost) return;

        if (!hasFocus)
        {
            // 일시 음소거: AudioListener 폴백 또는 마스터 채널 -80dB
            if (_audioMixer != null) TrySetMixerVolume(_masterVolumeParam, 0f);
            else AudioListener.volume = 0f;
        }
        else
        {
            // 포커스 복귀 시 현재 설정값으로 복원
            Apply(_current);
        }
    }
}
