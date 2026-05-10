using UnityEngine;

namespace OrganicEngineer.MeshCutting.CutCamera
{
    //==============================================================================
    // 기능: CutBounds
    // 설명: 몬스터 기준 Transform 아래 Renderer 들의 월드 Bounds 를 계산한다.
    //==============================================================================
    // TODO: 비활성 Renderer 포함 옵션, SkinnedMeshRenderer.localBounds 보정
    //
    //==============================================================================
    public static class CutBounds
    {
        //==============================================================================
        // 기능: CutBounds::Calculate
        // 설명: anchor 자식 Renderer 를 for 루프로 합쳐 카메라 자동 배치용 Bounds 를 반환한다.
        //==============================================================================
        // TODO: Renderer 가 없을 때 기본 크기 옵션 제공
        //
        //==============================================================================
        public static Bounds Calculate(Transform anchor)
        {
            // 기준 Transform 이 없으면 0 크기 Bounds 반환
            if (anchor == null) return new Bounds(Vector3.zero, Vector3.zero);

            // 활성화된 자식 Renderer 만 수집
            Renderer[] renderers = anchor.GetComponentsInChildren<Renderer>(false);

            // Renderer 가 없으면 anchor 위치 기준 Bounds 반환
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(anchor.position, Vector3.zero);
            }

            // 첫 Renderer Bounds 로 시작
            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                // 나머지 Renderer Bounds 를 하나로 합침
                combined.Encapsulate(renderers[i].bounds);
            }

            return combined;
        }
    }
}
