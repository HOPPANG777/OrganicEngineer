using UnityEngine;
using UnityEngine.Audio;

//==============================================================================
// 기능: AudioApply
// 설명: SettingsData.Audio 값을 AudioMixer 또는 AudioListener 에 적용한다.
//==============================================================================
// TODO: Mixer 파라미터 적용 성공 여부 검증
//
//==============================================================================
public class AudioApply
{
    //==============================================================================
    // 기능: AudioApply::Apply
    // 설명: 마스터/BGM/SFX/Voice/UI/Ambient 볼륨을 Mixer 에 적용하고, Mixer 가 없으면 AudioListener 로 폴백한다.
    //==============================================================================
    // TODO: 채널별 음소거 옵션
    //
    //==============================================================================
    public void Apply
    (
        AudioMixer audioMixer,
        SettingsData.Audio audio,
        string masterParam,
        string bgmParam,
        string sfxParam,
        string voiceParam,
        string uiParam,
        string ambientParam
    )
    {
        // 오디오 데이터가 없으면 적용 불가
        if (audio == null) return;

        // 전체 음소거가 켜져 있으면 마스터 볼륨 0
        float master = audio.Mute ? 0f : Mathf.Clamp01(audio.MasterVolume);

        // Mixer 가 있으면 각 채널을 dB 값으로 적용
        if (audioMixer != null)
        {
            SetMixerVolume(audioMixer, masterParam, master);
            SetMixerVolume(audioMixer, bgmParam, Mathf.Clamp01(audio.BgmVolume));
            SetMixerVolume(audioMixer, sfxParam, Mathf.Clamp01(audio.SfxVolume));
            SetMixerVolume(audioMixer, voiceParam, Mathf.Clamp01(audio.VoiceVolume));
            SetMixerVolume(audioMixer, uiParam, Mathf.Clamp01(audio.UiVolume));
            SetMixerVolume(audioMixer, ambientParam, Mathf.Clamp01(audio.AmbientVolume));
            return;
        }

        // Mixer 가 없으면 AudioListener 마스터 볼륨으로 폴백
        AudioListener.volume = master;
    }

    // Mixer 또는 AudioListener 에 마스터 음소거 상태 적용
    public void SetMasterMute(AudioMixer audioMixer, string masterParam, bool mute)
    {
        // Mixer 가 있으면 마스터 파라미터를 무음/최대 중 하나로 설정
        if (audioMixer != null)
        {
            SetMixerVolume(audioMixer, masterParam, mute ? 0f : 1f);
            return;
        }

        // Mixer 가 없으면 AudioListener 볼륨으로 대체
        AudioListener.volume = mute ? 0f : 1f;
    }

    // 0~1 선형 볼륨을 dB 로 바꿔 Mixer 파라미터에 적용
    public void SetMixerVolume(AudioMixer audioMixer, string param, float linear01)
    {
        // Mixer 또는 파라미터 이름이 없으면 적용 불가
        if (audioMixer == null || string.IsNullOrEmpty(param)) return;

        // 0 근처는 -80dB 로 처리
        float db = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
        audioMixer.SetFloat(param, db);
    }
}
