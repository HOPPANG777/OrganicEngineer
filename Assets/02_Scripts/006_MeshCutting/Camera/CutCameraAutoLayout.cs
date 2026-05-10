using Sirenix.OdinInspector;
using UnityEngine;

namespace OrganicEngineer.MeshCutting.CutCamera
{
    //==============================================================================
    // 기능: CutCameraAutoLayout
    // 설명: MonsterAnchor 의 Bounds 를 기준으로 탑·측면 카메라 5대의 위치/회전/Orthographic 사이즈를 자동 배치한다.
    //==============================================================================
    // TODO: SkinnedMesh.localBounds 활용, Perspective 거리 자동 계산, 슬롯 enum 도입
    //
    //==============================================================================
    public class CutCameraAutoLayout : MonoBehaviour
    {
        //-------------------- 맴버 변수 --------------------//

        [TabGroup("Camera")]
        [Tooltip("CutCameraRig")]
        [SerializeField] private CutCameraRig _rig;

        [TabGroup("Camera")]
        [Tooltip("중심·Bounds 기준이 되는 루트(몬스터 또는 그 부모)")]
        [SerializeField] private Transform _monsterAnchor;

        [TabGroup("Camera")]
        [Tooltip("Bounds 에 곱하는 여유율(1.0 = 딱 맞게, 1.2 = 20% 여유)")]
        [SerializeField] private float _paddingRatio = 1.2f;

        [TabGroup("Camera")]
        [Tooltip("Perspective 카메라일 때 추가로 띄울 거리(0 가능, Orthographic 에서는 시야 크기에 영향 없음)")]
        [SerializeField] private float _extraDistance = 0f;

        [TabGroup("Camera")]
        [Tooltip("true 면 5대 모두 Orthographic 으로 강제 + 사이즈 자동 적용")]
        [SerializeField] private bool _useOrthographic = true;

        [TabGroup("Camera")]
        [Tooltip("Start 시 자동으로 Recompute 호출 여부")]
        [SerializeField] private bool _recomputeOnStart = true;

        //-------------------- 내부 상수 --------------------//

        // 측면 카메라 슬롯 인덱스(_rig._sideCamera 의 순서와 일치해야 함)
        private const int SIDE_FRONT = 0;   // 0: Front  → 카메라 위치 = 중심의 -Z 쪽
        private const int SIDE_RIGHT = 1;   // 1: Right  → 카메라 위치 = 중심의 -X 쪽
        private const int SIDE_BACK  = 2;   // 2: Back   → 카메라 위치 = 중심의 +Z 쪽
        private const int SIDE_LEFT  = 3;   // 3: Left   → 카메라 위치 = 중심의 +X 쪽
        private const int SIDE_COUNT = 4;   // 측면 카메라 기대 개수

        // Bounds 가 사실상 0일 때 카메라가 한 점에 몰리는 것을 막기 위한 임계값
        private const float MIN_EXTENT = 1e-4f;

        //-------------------- 수명주기 --------------------//

        private void Start()
        {
            if (_recomputeOnStart)
            {
                Recompute();
            }
        }

        //==============================================================================
        // 기능: CutCameraAutoLayout::Recompute
        // 설명: Bounds 를 다시 계산하고 탑·측면 카메라 5대에 위치·회전·Orthographic 사이즈를 적용한다.
        //==============================================================================
        // TODO: 비활성 Renderer 포함 옵션, 결과 디버그 표시(Gizmo)
        //
        //==============================================================================
        [Button("Recompute Camera Layout")]
        public void Recompute()
        {
            // 리그가 없으면 카메라를 가져올 수 없음
            if (_rig == null)
            {
                Debug.LogWarning("[CutCameraAutoLayout] _rig 가 비어 있음.");
                return;
            }

            // 기준 트랜스폼이 없으면 Bounds 계산 불가
            if (_monsterAnchor == null)
            {
                Debug.LogWarning("[CutCameraAutoLayout] _monsterAnchor 가 비어 있음.");
                return;
            }

            // 측면 카메라 배열 길이 검증(4 강제)
            if (_rig.SideCameraCount != SIDE_COUNT)
            {
                Debug.LogWarning($"[CutCameraAutoLayout] _rig.SideCameraCount({_rig.SideCameraCount}) != {SIDE_COUNT}");
                return;
            }

            // 자식 Renderer 들로부터 합산 Bounds 계산
            Bounds b = CalculateAnchorBounds();

            // Bounds 가 사실상 0이면 카메라가 한 점에 몰리므로 경고 후 종료
            if (b.extents.sqrMagnitude < MIN_EXTENT * MIN_EXTENT)
            {
                Debug.LogWarning("[CutCameraAutoLayout] Bounds 가 너무 작아 배치를 건너뜀.");
                return;
            }

            // 탑 카메라 배치
            ApplyTop(b);

            // 측면 카메라 4대 배치(Front/Right/Back/Left 순), 위쪽 축은 모두 월드 Y 로 통일
            ApplySideAt(SIDE_FRONT, Vector3.back,    Vector3.up, Vector3.right,   b);
            ApplySideAt(SIDE_RIGHT, Vector3.left,    Vector3.up, Vector3.forward, b);
            ApplySideAt(SIDE_BACK,  Vector3.forward, Vector3.up, Vector3.right,   b);
            ApplySideAt(SIDE_LEFT,  Vector3.right,   Vector3.up, Vector3.forward, b);
        }

        //==============================================================================
        // 기능: CutCameraAutoLayout::CalculateAnchorBounds
        // 설명: _monsterAnchor 와 그 자식 Renderer 들의 월드 공간 Bounds 를 합쳐 돌려준다.
        //==============================================================================
        // TODO: 비활성 Renderer 포함 옵션, SkinnedMeshRenderer.localBounds 사용
        //
        //==============================================================================
        private Bounds CalculateAnchorBounds()
        {
            // 활성화된 자식 Renderer 만 모음
            Renderer[] renderers = _monsterAnchor.GetComponentsInChildren<Renderer>(false);

            // Renderer 가 없으면 anchor 위치 기준 0 크기 Bounds 반환(상위에서 가드)
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(_monsterAnchor.position, Vector3.zero);
            }

            // 첫 번째 Renderer 로 시작 → 나머지를 for 루프로 Encapsulate(컨벤션 2.2)
            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combined.Encapsulate(renderers[i].bounds);
            }
            return combined;
        }

        //==============================================================================
        // 기능: CutCameraAutoLayout::ApplyTop
        // 설명: 탑뷰 카메라를 Bounds 중심 위쪽에 배치하고, Orthographic 사이즈는 r.z(세로)·r.x(가로) 기준으로 맞춘다.
        //==============================================================================
        // TODO: Perspective 모드일 때 필요한 거리 자동 계산
        //
        //==============================================================================
        private void ApplyTop(Bounds b)
        {
            Camera top = _rig.TopCamera;
            // 탑 카메라가 없으면 탑 배치는 건너뜀(측면 배치는 계속 진행)
            if (top == null) return;

            Vector3 c = b.center;
            Vector3 r = b.extents;

            // 위쪽으로 r.y * pad + extra 만큼 띄움
            Vector3 pos = c + Vector3.up * (r.y * _paddingRatio + _extraDistance);

            // 중심을 바라보되, up 힌트는 월드 forward(탑뷰가 좌우로 비틀리지 않도록 명시)
            Quaternion rot = Quaternion.LookRotation(c - pos, Vector3.forward);

            top.transform.SetPositionAndRotation(pos, rot);

            // Orthographic 강제·사이즈 적용
            top.orthographic = _useOrthographic;
            if (_useOrthographic)
            {
                // 탑뷰: 화면 세로축 = 월드 z, 가로축 = 월드 x
                top.orthographicSize = ComputeOrthoSize(r.z, r.x, top.aspect);
            }
        }

        //==============================================================================
        // 기능: CutCameraAutoLayout::ApplySideAt
        // 설명: _rig 의 측면 카메라 슬롯에서 한 대를 꺼내 ApplySide 에 위임한다.
        //==============================================================================
        // TODO: 슬롯 라벨 enum(SideCameraSlot.FRONT 등) 도입
        //
        //==============================================================================
        private void ApplySideAt(int slotIndex, Vector3 worldDir, Vector3 verticalAxis, Vector3 horizontalAxis, Bounds b)
        {
            // 슬롯 범위 밖이면 무시(상위에서 길이 4 검증 후 호출)
            if (slotIndex < 0 || slotIndex >= _rig.SideCameraCount) return;

            Camera cam = _rig.GetSideCamera(slotIndex);
            if (cam == null) return;

            ApplySide(cam, worldDir, verticalAxis, horizontalAxis, b);
        }

        //==============================================================================
        // 기능: CutCameraAutoLayout::ApplySide
        // 설명: 측면 카메라를 Bounds 의 worldDir 방향 바깥에 배치하고, 가로/세로 축에 맞춰 Orthographic 사이즈를 적용한다.
        //==============================================================================
        // TODO: 카메라 up 힌트를 월드 up 외에 다른 축으로 바꿀 수 있도록 매개변수화
        //
        //==============================================================================
        private void ApplySide(Camera cam, Vector3 worldDir, Vector3 verticalAxis, Vector3 horizontalAxis, Bounds b)
        {
            Vector3 c = b.center;
            Vector3 r = b.extents;

            // worldDir 방향에서 본 Bounds 의 반치수(축에 정렬되어 있을 때 1차원 성분)
            float extentAlong = Mathf.Abs(Vector3.Dot(worldDir, r));

            // 카메라 위치: 중심에서 worldDir 방향으로 (반치수 * pad + extra) 만큼
            Vector3 pos = c + worldDir * (extentAlong * _paddingRatio + _extraDistance);

            // 중심을 바라보되, up 힌트는 월드 up
            Quaternion rot = Quaternion.LookRotation(c - pos, Vector3.up);

            cam.transform.SetPositionAndRotation(pos, rot);

            // Orthographic 강제·사이즈 적용
            cam.orthographic = _useOrthographic;
            if (_useOrthographic)
            {
                // 화면 세로/가로축에 해당하는 Bounds 반치수
                float halfH = Mathf.Abs(Vector3.Dot(verticalAxis, r));
                float halfW = Mathf.Abs(Vector3.Dot(horizontalAxis, r));
                cam.orthographicSize = ComputeOrthoSize(halfH, halfW, cam.aspect);
            }
        }

        //==============================================================================
        // 기능: CutCameraAutoLayout::ComputeOrthoSize
        // 설명: Bounds 의 반높이/반너비와 카메라 aspect 비를 받아 Orthographic 사이즈(반높이)를 계산한다.
        //==============================================================================
        // TODO: aspect 가 비정상일 때 더 정교한 보호
        //
        //==============================================================================
        private float ComputeOrthoSize(float halfH, float halfW, float aspect)
        {
            // aspect 가 비정상(0 이하)이면 안전한 기본값(1)으로 보정
            if (aspect <= 0f) aspect = 1f;

            // 세로 기준과 (가로 / aspect) 기준 중 더 큰 쪽을 채택해 잘림 방지
            return Mathf.Max(halfH, halfW / aspect) * _paddingRatio;
        }
    }
}
