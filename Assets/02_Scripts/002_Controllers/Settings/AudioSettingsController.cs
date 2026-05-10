using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

// 기능: AudioSettingsController
// 설명: 오디오 설정을 AudioMixer 또는 AudioListener 에 적용하고 포커스 상실 음소거를 처리한다.
//==============================================================================
// TODO: AudioApply 와 AudioFocusMute 컴포넌트 분리 범위 확장
//
//==============================================================================

public class AudioSettingsController : MonoBehaviour
{
    //-------------------- 맴버 변수 --------------------//

    // 오디오 시스템 적용 기능
    private AudioApply _audioApply = new AudioApply();

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

    //-------------------- 내부 변수 --------------------//

    // 가장 최근에 Apply 된 오디오 설정 데이터 참조
    private SettingsData.Audio _current;

    //-------------------- 기능 함수 --------------------//

    //==============================================================================
    // 기능: AudioSettingsController::Apply
    // 설명: 오디오 설정 데이터를 캐시하고 AudioMixer 또는 AudioListener 에 반영한다.
    //==============================================================================
    // TODO: 채널별 적용 실패 로그
    //
    //==============================================================================
    // a: 적용할 오디오 설정 데이터
    public void Apply(SettingsData.Audio a)
    {
        if (a == null) return;

        _current = a;

        // 실제 오디오 시스템 적용 책임은 AudioApply 로 분리
        _audioApply.Apply(_audioMixer, a, _masterVolumeParam, _bgmVolumeParam, _sfxVolumeParam, _voiceVolumeParam, _uiVolumeParam, _ambientVolumeParam);
    }

    //==============================================================================
    // 기능: AudioSettingsController::TrySetMixerVolume
    // 설명: 0~1 선형 볼륨을 dB 값으로 변환해 AudioMixer 파라미터에 설정한다.
    //==============================================================================
    // TODO: SetFloat 실패 여부 확인
    //
    //==============================================================================
    // param: AudioMixer 노출 파라미터 이름
    // linear01: 0~1 범위의 선형 볼륨 값
    private void TrySetMixerVolume(string param, float linear01)
    {
        // Mixer 볼륨 변환 책임은 AudioApply 로 분리
        _audioApply.SetMixerVolume(_audioMixer, param, linear01);
    }

    //==============================================================================
    // 기능: AudioSettingsController::OnApplicationFocus
    // 설명: 창 포커스 상실 시 MuteOnFocusLost 옵션이 켜져 있으면 마스터 볼륨을 임시 음소거한다.
    //==============================================================================
    // TODO: 포커스 복귀 시 이전 Mixer 값 정확히 복구
    //
    //==============================================================================
    // hasFocus: 현재 애플리케이션 포커스 보유 여부
    private void OnApplicationFocus(bool hasFocus)
    {
        if (_current == null) return;
        if (!_current.MuteOnFocusLost) return;

        if (!hasFocus)
        {
            // 일시 음소거: AudioListener 폴백 또는 마스터 채널 -80dB
            _audioApply.SetMasterMute(_audioMixer, _masterVolumeParam, true);
        }
        else
        {
            // 포커스 복귀 시 현재 설정값으로 복원
            Apply(_current);
        }
    }
}
