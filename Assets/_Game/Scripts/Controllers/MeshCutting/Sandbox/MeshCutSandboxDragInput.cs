using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;
using OrganicEngineer.MeshCutting.CutCamera;

namespace OrganicEngineer.MeshCutting.Sandbox
{
    //==============================================================================
    // 기능: MeshCutSandboxDragInput (MonoBehaviour)
    // 설명: 활성 컷 카메라로 좌클릭 드래그 Down/Up 레이를 쏘고 규칙 C 로 법선을 정한 뒤 MeshCutSandboxController 에 컷을 요청한다.
    //       테스트용으로 드래그 구간을 LineRenderer 로 표시한다.
    //==============================================================================
    // TODO: UI 위 클릭 무시(EventSystem), InputAction 자산 연동
    //
    //==============================================================================
    public class MeshCutSandboxDragInput : MonoBehaviour
    {
        //-------------------- 맴버 변수 --------------------//

        [TabGroup("References")]
        [Tooltip("컷 패널·레이에 사용하는 CutCameraRig")]
        [SerializeField]
        private CutCameraRig _cameraRig;

        [TabGroup("References")]
        [Tooltip("절단·스폰을 수행하는 MeshCutSandboxController")]
        [SerializeField]
        private MeshCutSandboxController _sandbox;

        [TabGroup("Raycast")]
        [Tooltip("3D Physics.Raycast 에 사용할 레이어 마스크")]
        [SerializeField]
        private LayerMask _raycastLayers = ~0;

        [TabGroup("Raycast")]
        [Tooltip("레이 최대 거리")]
        [SerializeField]
        private float _maxRayDistance = 500f;

        [TabGroup("Raycast")]
        [Tooltip("(B−A)×forward 제곱이 이보다 작으면 카메라 깊이와 거의 평행해 컷 생략")]
        [SerializeField]
        private float _parallelCrossSqrEpsilon = 1e-10f;

        [TabGroup("Raycast")]
        [Tooltip("Down·Up 간 월드 거리 제곱이 이보다 작으면 컷 생략")]
        [SerializeField]
        private float _minDragWorldSqr = 1e-8f;

        [TabGroup("Preview")]
        [Tooltip("드래그 중 LineRenderer 로 구간 표시 여부")]
        [SerializeField]
        private bool _showDragPreviewLine = true;

        [TabGroup("Preview")]
        [Tooltip("타깃 표면에 레이가 맞을 때 선 색")]
        [SerializeField]
        private Color _dragLineColorValid = new Color(1f, 0.92f, 0.2f, 0.95f);

        [TabGroup("Preview")]
        [Tooltip("타깃 밖으로 레이가 빗나갈 때 선 색")]
        [SerializeField]
        private Color _dragLineColorMiss = new Color(0.5f, 0.5f, 0.5f, 0.45f);

        [TabGroup("Preview")]
        [Tooltip("LineRenderer 폭(월드 단위)")]
        [SerializeField]
        private float _dragLineWidth = 0.05f;

        [TabGroup("Preview")]
        [Tooltip("비우면 URP/Unlit 등으로 시도. 실패 시 인스펙터에서 지정")]
        [SerializeField]
        private Material _dragLineMaterial;

        [TabGroup("Debug")]
        [Tooltip("레이·타깃 검사 실패 시 콘솔 로그 출력")]
        [SerializeField]
        private bool _logDiagnostics;

        [TabGroup("Debug")]
        [Tooltip("켜면 Down 시 후보 카메라를 순서대로 레이쏴서 절단 타깃을 처음 맞춘 카메라를 씁니다. pixelRect 불일치 해소에 유리합니다.")]
        [SerializeField]
        private bool _probeAllCamerasForCutTargetHit = true;

        [TabGroup("Debug")]
        [Tooltip("게임 뷰 좌측 하단에 마우스·레이 카메라·히트 상태를 매 프레임 표시합니다.")]
        [SerializeField]
        private bool _showHudDiagnostics;

        // 다운 시점 월드 좌표(성공 시에만 유지)
        private Vector3? _dragStartWorld;

        // 다운 시점에 레이로 고른 절단 대상(연속 컷: Down·Up 이 같은 MeshFilter 여야 함)
        private MeshFilter _dragCutMeshSource;

        // 다운 시점에 고정한 레이 카메라(분할 뷰에서 제스처 중 일관성 유지)
        private Camera _gestureRayCamera;

        // 드래그 프리뷰 LineRenderer
        private LineRenderer _previewLine;

        // 런타임 생성 머티리얼이면 OnDestroy 에서 Destroy
        private bool _ownsPreviewMaterial;

        // 참조 미연결 경고를 한 번만 출력하기 위한 정적 플래그
        private static bool _loggedMissingRefs;

        // 후보 카메라 수집(프로브 순서) 버퍼, 할당 회피
        private readonly List<Camera> _cameraProbeScratch = new List<Camera>();

        // HUD 문자열 버퍼(프레임당 1회 갱신)
        private readonly StringBuilder _diagHudSb = new StringBuilder(384);

        // 마지막 포인터 다운 결과 요약(OnGUI 에 표시)
        private string _diagLastPointerDown = "-";

        // 진단 패널에 그릴 텍스트(프레임당 LateUpdate 에서 재구성)
        private string _hudDiagCached = string.Empty;

        // 진단 패널용 GUIStyle 재사용
        private GUIStyle _hudDiagStyle;

        //-------------------- 수명 주기 --------------------//

        private void Reset()
        {
            if (_cameraRig == null)
                _cameraRig = GetComponentInParent<CutCameraRig>();
            if (_sandbox == null)
                _sandbox = GetComponent<MeshCutSandboxController>();
        }

        //==============================================================================
        // 기능: MeshCutSandboxDragInput::OnDestroy
        // 설명: 런타임에서 만든 Line 머티리얼을 정리한다.
        //==============================================================================
        private void OnDestroy()
        {
            if (_ownsPreviewMaterial && _previewLine != null && _previewLine.material != null)
                Destroy(_previewLine.material);
        }

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutSandboxDragInput::Update
        // 설명: Input System 마우스로 Down/Up·프리뷰·규칙 C 컷을 처리한다.
        //==============================================================================
        private void LateUpdate()
        {
            if (!_showHudDiagnostics) return;

            RefreshDiagnosticsHud();
        }

        //==============================================================================
        // 기능: MeshCutSandboxDragInput::Update
        // 설명: Input System 마우스로 Down/Up·프리뷰·규칙 C 컷을 처리한다.
        //==============================================================================
        private void Update()
        {
            // 가드: 참조
            if (_cameraRig == null || _sandbox == null)
            {
                LogOnceMissingRefs();
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 pointer = mouse.position.ReadValue();

            Camera rectPickCam = _cameraRig.GetRayCameraForScreenPoint(pointer);
            Camera gestureBaseCam = rectPickCam;
            if (gestureBaseCam == null)
                gestureBaseCam = _cameraRig.ActiveCutCamera;
            if (gestureBaseCam == null)
                gestureBaseCam = _cameraRig.TopCamera;

            Camera camForRay = ResolvedOrGestureRayCamera(gestureBaseCam, mouse);

            if (mouse.leftButton.wasPressedThisFrame)
                HandlePointerDown(pointer);

            if (_showDragPreviewLine)
            {
                if (camForRay != null && _dragStartWorld.HasValue && mouse.leftButton.isPressed)
                    UpdateDragPreview(camForRay, pointer);
                else
                    HideDragPreview();
            }

            if (!mouse.leftButton.wasReleasedThisFrame || !_dragStartWorld.HasValue) return;

            Camera releaseRayCam = camForRay ?? _gestureRayCamera;
            if (releaseRayCam == null)
            {
                _dragStartWorld = null;
                _dragCutMeshSource = null;
                _gestureRayCamera = null;
                HideDragPreview();
                _diagLastPointerDown = "RELEASE: 레이 카메라 null";
                return;
            }

            TryCompleteDragOnRelease(releaseRayCam, pointer);
        }

        private void OnGUI()
        {
            if (!_showHudDiagnostics || string.IsNullOrEmpty(_hudDiagCached)) return;

            if (_hudDiagStyle == null)
            {
                _hudDiagStyle = new GUIStyle(GUI.skin.box);
                _hudDiagStyle.fontSize = 12;
                _hudDiagStyle.alignment = TextAnchor.LowerLeft;
                _hudDiagStyle.wordWrap = true;
            }

            float boxH = 168f;
            float boxW = Mathf.Min(Screen.width - 16f, 720f);
            GUI.Box(new Rect(8f, Screen.height - boxH - 8f, boxW, boxH), _hudDiagCached, _hudDiagStyle);
        }

        //==============================================================================
        // 기능: MeshCutSandboxDragInput::HandlePointerDown
        // 설명: 좌클릭 다운 시 레이캐스트로 드래그 시작점을 잡는다.
        //==============================================================================
        private void HandlePointerDown(Vector2 pointer)
        {
            _gestureRayCamera = null;
            _dragCutMeshSource = null;

            Camera camResolved = ResolveCameraForPointerDownRay(pointer);
            if (camResolved == null)
            {
                _dragStartWorld = null;
                _diagLastPointerDown = "MISS ray camera=null";
                if (_logDiagnostics)
                    Debug.Log("[MeshCutSandboxDragInput] Down: 레이 카메라 null.");
                return;
            }

            Ray ray = camResolved.ScreenPointToRay(ToScreenVector3(pointer));
            bool hitAnything = Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, _raycastLayers, QueryTriggerInteraction.Ignore);
            if (hitAnything && _sandbox.TryResolveCutMeshFilterFromHit(hit, out MeshFilter cutMf))
            {
                _dragStartWorld = hit.point;
                _dragCutMeshSource = cutMf;
                _gestureRayCamera = camResolved;
                _diagLastPointerDown = $"OK → {camResolved.name} ({cutMf.name})";
            }
            else
            {
                _dragStartWorld = null;

                _diagLastPointerDown = !hitAnything
                    ? $"MISS 전체 레이 미적중 cam={camResolved.name}"
                    : $"MISS 대상 불일치 hit={hit.collider.name}";

                if (!_logDiagnostics) return;
                if (!hitAnything)
                    Debug.Log($"[MeshCutSandboxDragInput] 레이 미적중. cam={camResolved.name}, mask={_raycastLayers.value}");
                else
                    Debug.Log($"[MeshCutSandboxDragInput] 히트했으나 절단 타깃 아님. hit={hit.collider.name}");
            }
        }

        //==============================================================================
        // 기능: MeshCutSandboxDragInput::TryCompleteDragOnRelease
        // 설명: 릴리스 시 두 번째 히트·최소 드래그·외적 임계 통과 시 Sandbox 컷을 호출한다.
        //==============================================================================
        private void TryCompleteDragOnRelease(Camera camForRay, Vector2 pointer)
        {
            MeshFilter downSource = _dragCutMeshSource;
            Ray upRay = camForRay.ScreenPointToRay(ToScreenVector3(pointer));
            MeshFilter upSource = null;
            bool hitUp = Physics.Raycast(upRay, out RaycastHit hitUpHit, _maxRayDistance, _raycastLayers, QueryTriggerInteraction.Ignore);
            if (hitUp)
                _sandbox.TryResolveCutMeshFilterFromHit(hitUpHit, out upSource);

            if (!hitUp || upSource == null || upSource != downSource)
            {
                _dragStartWorld = null;
                _dragCutMeshSource = null;
                _gestureRayCamera = null;
                if (_logDiagnostics)
                    Debug.Log("[MeshCutSandboxDragInput] 릴리스 시 레이가 타깃을 맞추지 못하거나 Down 과 다른 조각.");
                if (hitUp && upSource != null && downSource != null && upSource != downSource)
                    _diagLastPointerDown = "RELEASE: Up 조각 ≠ Down 조각";
                return;
            }

            Vector3 worldA = _dragStartWorld.Value;
            Vector3 worldB = hitUpHit.point;
            Vector3 ab = worldB - worldA;
            _dragStartWorld = null;
            _dragCutMeshSource = null;
            _gestureRayCamera = null;
            HideDragPreview();

            if (ab.sqrMagnitude < _minDragWorldSqr) return;

            Vector3 normal = Vector3.Cross(ab, camForRay.transform.forward);
            if (normal.sqrMagnitude < _parallelCrossSqrEpsilon) return;

            normal.Normalize();
            bool cutOk = _sandbox.TryCutFromWorldPlane(normal, worldA, downSource);
            if (!cutOk)
                _diagLastPointerDown = "RELEASE: TryCut false (평면 미적용 등)";
            else
                _diagLastPointerDown = "RELEASE: TryCut OK";
        }

        //==============================================================================
        // 기능: MeshCutSandboxDragInput::ResolvedOrGestureRayCamera
        // 설명: 제스처 중이면 다운 시 고정한 카메라로 ScreenPointToRay 를 맞춘다.
        //==============================================================================
        private Camera ResolvedOrGestureRayCamera(Camera camResolved, Mouse mouse)
        {
            if (_gestureRayCamera != null
                && _dragStartWorld.HasValue
                && (mouse.leftButton.isPressed || mouse.leftButton.wasReleasedThisFrame))
                return _gestureRayCamera;
            return camResolved;
        }

        //==============================================================================
        // 기능: MeshCutSandboxDragInput::ResolveCameraForPointerDownRay
        // 설명: Down 시 메인 레이를 쏠 카메라를 고른다. 옵션 시 후보 전부를 순회해 타깃 히트 우선.
        //==============================================================================
        private Camera ResolveCameraForPointerDownRay(Vector2 pointer)
        {
            if (!_probeAllCamerasForCutTargetHit || _sandbox == null)
                return _cameraRig.GetRayCameraForScreenPoint(pointer);

            BuildCameraCandidatesForProbe(pointer);
            int candidateCount = _cameraProbeScratch.Count;
            for (int i = 0; i < candidateCount; i++)
            {
                Camera cam = _cameraProbeScratch[i];
                if (cam == null || !cam.isActiveAndEnabled) continue;
                Ray ray = cam.ScreenPointToRay(ToScreenVector3(pointer));
                if (!Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, _raycastLayers, QueryTriggerInteraction.Ignore))
                    continue;
                if (_sandbox.IsRayHitOnCutTarget(hit))
                    return cam;
            }

            return _cameraRig.GetRayCameraForScreenPoint(pointer);
        }

        //==============================================================================
        // 기능: MeshCutSandboxDragInput::BuildCameraCandidatesForProbe
        // 설명: Rect 기준 카메라 → 탑 → 측면 순으로 중복 없이 후보 목록을 만든다.
        //==============================================================================
        private void BuildCameraCandidatesForProbe(Vector2 pointer)
        {
            _cameraProbeScratch.Clear();
            AppendUniqueProbeCamera(_cameraRig.GetRayCameraForScreenPoint(pointer));
            AppendUniqueProbeCamera(_cameraRig.TopCamera);
            int sideCount = _cameraRig.SideCameraCount;
            for (int i = 0; i < sideCount; i++)
                AppendUniqueProbeCamera(_cameraRig.GetSideCamera(i));
        }

        private void AppendUniqueProbeCamera(Camera cam)
        {
            if (cam == null || !cam.isActiveAndEnabled) return;
            if (_cameraProbeScratch.Contains(cam)) return;
            _cameraProbeScratch.Add(cam);
        }

        //==============================================================================
        // 기능: MeshCutSandboxDragInput::RefreshDiagnosticsHud
        // 설명: OnGUI 패널에 표시할 텍스트를 구성한다. LateUpdate 에서 호출.
        //==============================================================================
        private void RefreshDiagnosticsHud()
        {
            StringBuilder sb = _diagHudSb;
            sb.Length = 0;

            sb.Append("[MeshCut DragInput]").Append('\n');
            sb.Append($"Screen={Screen.width}x{Screen.height} ProbeAllHit={(_probeAllCamerasForCutTargetHit ? "ON" : "OFF")}").Append('\n');

            if (_cameraRig == null) sb.Append("CutCameraRig=NULL\n");
            if (_sandbox == null) sb.Append("Sandbox=NULL\n");

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                sb.Append("Mouse.current=NULL ← Game 창 포커스 또는 입력 장치 확인\n");
                _hudDiagCached = sb.ToString();
                return;
            }

            Vector2 pointer = mouse.position.ReadValue();
            Camera rectPick = _cameraRig != null ? _cameraRig.GetRayCameraForScreenPoint(pointer) : null;

            sb.Append($"Pointer=({pointer.x:F0},{pointer.y:F0}) LDown={mouse.leftButton.wasPressedThisFrame} held={mouse.leftButton.isPressed}").Append('\n');
            sb.Append($"RectPick={(rectPick != null ? rectPick.name : "null")}").Append('\n');

            if (_probeAllCamerasForCutTargetHit && _cameraRig != null && _sandbox != null)
            {
                BuildCameraCandidatesForProbe(pointer);
                sb.Append($"Probe({_cameraProbeScratch.Count}): ");
                int n = _cameraProbeScratch.Count;
                for (int i = 0; i < n; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(_cameraProbeScratch[i].name);
                }
                sb.Append('\n');
            }

            sb.Append($"LastDown={_diagLastPointerDown}").Append('\n');
            sb.Append($"Dragging={(_dragStartWorld.HasValue ? "YES" : "NO")} GestureCam=").Append((_gestureRayCamera != null ? _gestureRayCamera.name : "-")).Append('\n');

            if (_sandbox != null)
            {
                MeshFilter mf = _sandbox.CutTargetMeshFilter;
                sb.Append($"RootTarget=").Append((mf != null ? mf.name : "NULL")).Append(" Spawned=").Append(_sandbox.SpawnedPieceCount)
                    .Append(" AlreadyCut=").Append(_sandbox.HasCutCompleted).Append('\n');
            }

            _hudDiagCached = sb.ToString();
        }

        //==============================================================================
        // 기능: MeshCutSandboxDragInput::LogOnceMissingRefs
        // 설명: Rig/Sandbox 미연결 시 경고를 한 번만 출력한다.
        //==============================================================================
        private void LogOnceMissingRefs()
        {
            if (_loggedMissingRefs) return;
            _loggedMissingRefs = true;
            Debug.LogWarning("[MeshCutSandboxDragInput] Camera Rig 또는 Sandbox 가 비어 있습니다. 인스펙터에서 연결하세요.");
        }

        // 스크린 픽셀을 ScreenPointToRay 입력용 좌표로 변환한다.
        private static Vector3 ToScreenVector3(Vector2 pointer) => new Vector3(pointer.x, pointer.y, 0f);

        //==============================================================================
        // 기능: MeshCutSandboxDragInput::UpdateDragPreview
        // 설명: 드래그 중 시작점에서 현재 포인터 레이 히트까지 LineRenderer 를 갱신한다.
        //==============================================================================
        private void UpdateDragPreview(Camera cam, Vector2 pointer)
        {
            EnsurePreviewLine();
            if (_previewLine == null) return;

            Ray ray = cam.ScreenPointToRay(ToScreenVector3(pointer));
            Vector3 start = _dragStartWorld.Value;

            Vector3 end = start;
            Color previewColor = _dragLineColorMiss;
            if (_dragCutMeshSource != null
                && Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, _raycastLayers, QueryTriggerInteraction.Ignore)
                && _sandbox.TryResolveCutMeshFilterFromHit(hit, out MeshFilter dragHitMf)
                && dragHitMf == _dragCutMeshSource)
            {
                end = hit.point;
                previewColor = _dragLineColorValid;
            }

            _previewLine.enabled = true;
            _previewLine.SetPosition(0, start);
            _previewLine.SetPosition(1, end);
            _previewLine.startColor = previewColor;
            _previewLine.endColor = previewColor;
        }

        //==============================================================================
        // 기능: MeshCutSandboxDragInput::HideDragPreview
        // 설명: LineRenderer 를 끈다.
        //==============================================================================
        private void HideDragPreview()
        {
            if (_previewLine != null)
                _previewLine.enabled = false;
        }

        //==============================================================================
        // 기능: MeshCutSandboxDragInput::EnsurePreviewLine
        // 설명: 미리보기용 LineRenderer 자식을 한 번만 구성한다.
        //==============================================================================
        // TODO: 풀링·씬 전환 시 정리 정책
        //
        //==============================================================================
        private void EnsurePreviewLine()
        {
            if (_previewLine != null) return;

            GameObject previewGo = new GameObject("MeshCutDragPreview");
            previewGo.transform.SetParent(transform, false);
            _previewLine = previewGo.AddComponent<LineRenderer>();
            _previewLine.loop = false;
            _previewLine.positionCount = 2;
            _previewLine.useWorldSpace = true;
            _previewLine.textureMode = LineTextureMode.Stretch;
            _previewLine.numCapVertices = 2;
            _previewLine.numCornerVertices = 2;
            _previewLine.shadowCastingMode = ShadowCastingMode.Off;
            _previewLine.receiveShadows = false;
            _previewLine.widthMultiplier = 1f;
            _previewLine.startWidth = _dragLineWidth;
            _previewLine.endWidth = _dragLineWidth;

            Material lineMaterial;
            if (_dragLineMaterial != null)
            {
                lineMaterial = _dragLineMaterial;
                _ownsPreviewMaterial = false;
            }
            else
            {
                Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit")
                                    ?? Shader.Find("Unlit/Color")
                                    ?? Shader.Find("Sprites/Default");
                if (lineShader == null)
                {
                    Debug.LogWarning("[MeshCutSandboxDragInput] 드래그 선용 셰이더를 찾지 못했습니다. Drag Line Material 을 지정하세요.");
                    Destroy(previewGo);
                    _previewLine = null;
                    return;
                }

                lineMaterial = new Material(lineShader);
                _ownsPreviewMaterial = true;
                if (lineMaterial.HasProperty("_BaseColor"))
                    lineMaterial.SetColor("_BaseColor", _dragLineColorValid);
                else if (lineMaterial.HasProperty("_Color"))
                    lineMaterial.SetColor("_Color", _dragLineColorValid);
            }

            _previewLine.material = lineMaterial;
        }
    }
}
