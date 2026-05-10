using UnityEngine;

//==============================================================================
// 기능: ResolutionList
// 설명: 옵션 UI에서 사용할 해상도 목록, 라벨, 현재 인덱스, 데이터 갱신을 담당한다.
//==============================================================================
// TODO: 중복 해상도 필터링 옵션
//
//==============================================================================
public static class ResolutionList
{
    // 현재 디스플레이가 지원하는 해상도 목록 반환
    public static Resolution[] GetAvailable()
    {
        return Screen.resolutions;
    }

    //==============================================================================
    // 기능: ResolutionList::GetLabels
    // 설명: 해상도 배열을 UI Dropdown 에 넣기 쉬운 문자열 배열로 변환한다.
    //==============================================================================
    // TODO: 지역화 포맷 지원
    //
    //==============================================================================
    public static string[] GetLabels()
    {
        // 현재 플랫폼의 해상도 목록
        Resolution[] resolutions = Screen.resolutions;

        // 각 해상도에 대응하는 라벨 배열
        string[] labels = new string[resolutions.Length];
        for (int i = 0; i < resolutions.Length; i++)
        {
            // 해상도와 Hz 를 보기 좋은 문자열로 변환
            Resolution r = resolutions[i];
            labels[i] = $"{r.width} x {r.height} @ {Mathf.RoundToInt((float)r.refreshRateRatio.value)}Hz";
        }

        return labels;
    }

    // 현재 화면 해상도와 가장 가까운 인덱스 반환
    public static int GetCurrentIndex()
    {
        Resolution[] resolutions = Screen.resolutions;
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
            {
                return i;
            }
        }

        return -1;
    }

    // 선택한 해상도 인덱스를 Graphics 데이터에 반영
    public static void SetByIndex(SettingsData.Graphics g, int index)
    {
        // 데이터가 없으면 갱신 불가
        if (g == null) return;

        // 범위 밖 인덱스는 무시
        Resolution[] resolutions = Screen.resolutions;
        if (index < 0 || index >= resolutions.Length) return;

        // 선택한 해상도 값 저장
        Resolution r = resolutions[index];
        g.ResolutionIndex = index;
        g.Width = r.width;
        g.Height = r.height;
        g.RefreshRate = Mathf.RoundToInt((float)r.refreshRateRatio.value);
    }
}
