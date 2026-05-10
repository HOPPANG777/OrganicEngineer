using UnityEngine;

namespace OrganicEngineer.MeshCutting.CutCamera
{
    //==============================================================================
    // 기능: CutCameraSize
    // 설명: 카메라가 Bounds 를 화면 안에 담기 위한 Orthographic 크기를 계산한다.
    //==============================================================================
    // TODO: Perspective 카메라 거리 계산 지원
    //
    //==============================================================================
    public class CutCameraSize
    {
        //==============================================================================
        // 기능: CutCameraSize::ComputeOrtho
        // 설명: 반높이와 반너비, aspect, padding 을 이용해 OrthographicSize 값을 계산한다.
        //==============================================================================
        // TODO: aspect 값이 비정상일 때 정책 세분화
        //
        //==============================================================================
        public float ComputeOrtho(float halfH, float halfW, float aspect, float paddingRatio)
        {
            // aspect 가 0 이하이면 1로 보정해 나눗셈 오류를 방지
            if (aspect <= 0f) aspect = 1f;

            // padding 이 0 이하이면 카메라 크기가 0이 될 수 있으므로 1로 보정
            if (paddingRatio <= 0f) paddingRatio = 1f;

            // 세로 기준과 가로 기준 중 큰 값을 사용해 화면 잘림 방지
            return Mathf.Max(halfH, halfW / aspect) * paddingRatio;
        }
    }
}
