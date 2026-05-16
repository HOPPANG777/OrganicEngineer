using OrganicEngineer.MeshCutting.CutCamera;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OrganicEngineer.MeshCutting.Sandbox
{
    //==============================================================================
    // 기능: MeshCutPieceDragInput
    // 설명: 우클릭 드래그로 절단 조각을 카메라 기준 평면 위에서 이동시킨다.
    //==============================================================================
    // TODO: 선택 하이라이트와 이동 제한 영역 추가
    //
    //==============================================================================
    public class MeshCutPieceDragInput : MonoBehaviour
    {
        //-------------------- 맴버 변수 --------------------//

        [Tooltip("분할 컷 카메라 기준으로 레이를 선택할 때 사용합니다.")]
        [SerializeField]
        private CutCameraRig _cameraRig;

        [Tooltip("CameraRig가 없을 때 사용할 예비 카메라입니다.")]
        [SerializeField]
        private Camera _fallbackCamera;

        [Tooltip("조각 선택 Raycast 에 사용할 레이어 마스크입니다.")]
        [SerializeField]
        private LayerMask _raycastLayers = ~0;

        [Tooltip("조각 선택 Raycast 최대 거리입니다.")]
        [SerializeField]
        private float _maxRayDistance = 500f;

        [Tooltip("우클릭 드래그 중 휠 한 칸당 카메라 깊이 이동량입니다.")]
        [SerializeField]
        private float _wheelDepthSpeed = 0.05f;

        [Tooltip("우클릭 드래그 중 휠로 조절할 수 있는 최대 깊이 거리입니다.")]
        [SerializeField]
        private float _maxWheelDepthOffset = 50f;

        // 현재 우클릭으로 선택한 절단 조각
        private MeshCutPiece _dragPiece;

        // 드래그 중 사용할 카메라
        private Camera _dragCamera;

        // 선택 시점에 만든 카메라 기준 이동 평면
        private Plane _dragPlane;

        // 평면 위 포인터 위치에서 조각 중심까지의 월드 오프셋
        private Vector3 _dragOffset;

        // 휠 입력으로 조절한 카메라 깊이 방향 오프셋
        private float _dragDepthOffset;

        //-------------------- 수명 주기 --------------------//

        private void Awake()
        {
            // 런타임 자동 추가 시에도 부모 카메라 Rig 를 연결한다.
            CacheCameraRig();
        }

        private void Reset()
        {
            // 에디터에서 컴포넌트 추가 시 부모 카메라 Rig 를 연결한다.
            CacheCameraRig();
        }

        //==============================================================================
        // 기능: MeshCutPieceDragInput::Update
        // 설명: 우클릭 Down/Hold/Up 입력으로 조각 선택과 이동을 처리한다.
        //==============================================================================
        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 pointer = mouse.position.ReadValue();

            if (mouse.rightButton.wasPressedThisFrame)
                TryBeginDrag(pointer);

            if (mouse.rightButton.isPressed)
            {
                ApplyWheelDepth(mouse.scroll.ReadValue());
                UpdateDrag(pointer);
            }

            if (mouse.rightButton.wasReleasedThisFrame)
                EndDrag();
        }

        //==============================================================================
        // 기능: MeshCutPieceDragInput::TryBeginDrag
        // 설명: 포인터 아래의 MeshCutPiece 를 찾아 드래그 상태를 시작한다.
        //==============================================================================
        private void TryBeginDrag(Vector2 pointer)
        {
            _dragPiece = null;
            _dragCamera = ResolveRayCamera(pointer);
            if (_dragCamera == null) return;

            Ray ray = _dragCamera.ScreenPointToRay(ToScreenPoint(pointer));
            bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, _maxRayDistance, _raycastLayers, QueryTriggerInteraction.Ignore);
            if (!hit) return;

            MeshCutPiece piece = hitInfo.collider.GetComponentInParent<MeshCutPiece>();
            if (piece == null || !piece.CanMove) return;

            // 카메라를 바라보는 평면을 조각 중심에 고정해 화면상 드래그 이동을 만든다.
            _dragPiece = piece;
            _dragPlane = new Plane(-_dragCamera.transform.forward, piece.transform.position);
            if (!_dragPlane.Raycast(ray, out float distance)) return;
            _dragOffset = piece.transform.position - ray.GetPoint(distance);
            _dragDepthOffset = 0f;
        }

        //==============================================================================
        // 기능: MeshCutPieceDragInput::ApplyWheelDepth
        // 설명: 우클릭 드래그 중 휠 입력을 카메라 깊이 방향 이동량으로 누적한다.
        //==============================================================================
        private void ApplyWheelDepth(Vector2 scroll)
        {
            if (_dragPiece == null) return;
            if (Mathf.Abs(scroll.y) <= 0f) return;

            // 휠 위쪽은 카메라 쪽으로 당기고, 아래쪽은 멀어지도록 부호를 반대로 누적한다.
            _dragDepthOffset -= scroll.y * _wheelDepthSpeed;
            _dragDepthOffset = Mathf.Clamp(_dragDepthOffset, -_maxWheelDepthOffset, _maxWheelDepthOffset);
        }

        //==============================================================================
        // 기능: MeshCutPieceDragInput::UpdateDrag
        // 설명: 포인터 레이를 드래그 평면에 투영하고 선택 조각을 이동시킨다.
        //==============================================================================
        private void UpdateDrag(Vector2 pointer)
        {
            if (_dragPiece == null || _dragCamera == null) return;

            Ray ray = _dragCamera.ScreenPointToRay(ToScreenPoint(pointer));
            if (!_dragPlane.Raycast(ray, out float distance)) return;

            // 선택 시점 오프셋을 더해 클릭한 지점이 손에 붙은 것처럼 유지한다.
            Vector3 depthOffset = _dragCamera.transform.forward * _dragDepthOffset;
            Vector3 targetPosition = ray.GetPoint(distance) + _dragOffset + depthOffset;
            _dragPiece.MoveTo(targetPosition);
        }

        //==============================================================================
        // 기능: MeshCutPieceDragInput::EndDrag
        // 설명: 현재 드래그 상태를 정리한다.
        //==============================================================================
        private void EndDrag()
        {
            _dragPiece = null;
            _dragCamera = null;
            _dragOffset = Vector3.zero;
            _dragDepthOffset = 0f;
        }

        //==============================================================================
        // 기능: MeshCutPieceDragInput::ResolveRayCamera
        // 설명: 포인터 위치에 맞는 컷 카메라를 우선 선택하고 예비 카메라로 보완한다.
        //==============================================================================
        private Camera ResolveRayCamera(Vector2 pointer)
        {
            if (_cameraRig != null)
            {
                Camera rigCamera = _cameraRig.GetRayCameraForScreenPoint(pointer);
                if (rigCamera != null) return rigCamera;

                if (_cameraRig.ActiveCutCamera != null) return _cameraRig.ActiveCutCamera;
                if (_cameraRig.TopCamera != null) return _cameraRig.TopCamera;
            }

            return _fallbackCamera;
        }

        // 같은 오브젝트 또는 부모에 있는 카메라 Rig 를 캐싱한다.
        private void CacheCameraRig()
        {
            if (_cameraRig == null)
                _cameraRig = GetComponentInParent<CutCameraRig>();
        }

        // 스크린 픽셀 좌표를 ScreenPointToRay 입력 좌표로 변환한다.
        private Vector3 ToScreenPoint(Vector2 pointer)
        {
            return new Vector3(pointer.x, pointer.y, 0f);
        }
    }
}
