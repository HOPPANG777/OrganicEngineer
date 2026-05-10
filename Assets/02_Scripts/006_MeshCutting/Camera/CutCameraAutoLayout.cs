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

        // 몬스터 Renderer Bounds 계산 기능
        private CutBounds _cutBounds = new CutBounds();

        // 카메라 배치 적용 기능
        private CutCameraPlace _cutCameraPlace = new CutCameraPlace();

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

        // Front 측면 카메라 슬롯 인덱스
        private int _sideFront = 0;

        // Right 측면 카메라 슬롯 인덱스
        private int _sideRight = 1;

        // Back 측면 카메라 슬롯 인덱스
        private int _sideBack = 2;

        // Left 측면 카메라 슬롯 인덱스
        private int _sideLeft = 3;

        // 측면 카메라 기대 개수
        private int _sideCount = 4;

        // Bounds 가 사실상 0일 때 카메라가 한 점에 몰리는 것을 막기 위한 임계값
        private float _minExtent = 1e-4f;

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
            if (_rig.SideCameraCount != _sideCount)
            {
                Debug.LogWarning($"[CutCameraAutoLayout] _rig.SideCameraCount({_rig.SideCameraCount}) != {_sideCount}");
                return;
            }

            // 자식 Renderer 들로부터 합산 Bounds 계산
            Bounds b = CalculateAnchorBounds();

            // Bounds 가 사실상 0이면 카메라가 한 점에 몰리므로 경고 후 종료
            if (b.extents.sqrMagnitude < _minExtent * _minExtent)
            {
                Debug.LogWarning("[CutCameraAutoLayout] Bounds 가 너무 작아 배치를 건너뜀.");
                return;
            }

            // 탑 카메라 배치
            _cutCameraPlace.ApplyTop(_rig.TopCamera, b, _paddingRatio, _extraDistance, _useOrthographic);

            // 측면 카메라 4대 배치(Front/Right/Back/Left 순), 위쪽 축은 모두 월드 Y 로 통일
            ApplySideAt(_sideFront, Vector3.back,    Vector3.up, Vector3.right,   b);
            ApplySideAt(_sideRight, Vector3.left,    Vector3.up, Vector3.forward, b);
            ApplySideAt(_sideBack,  Vector3.forward, Vector3.up, Vector3.right,   b);
            ApplySideAt(_sideLeft,  Vector3.right,   Vector3.up, Vector3.forward, b);
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
            // Bounds 계산 책임은 CutBounds 로 분리
            return _cutBounds.Calculate(_monsterAnchor);
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

            _cutCameraPlace.ApplySide(cam, worldDir, verticalAxis, horizontalAxis, b, _paddingRatio, _extraDistance, _useOrthographic);
        }
    }
}
