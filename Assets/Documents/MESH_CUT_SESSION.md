# 메시 컷 — 세션 핸드오프 (짧게)

메시 컷 작업 시 **이 파일 §0만** 붙이거나 @ 한다. 전체 ToDo·씬은 `SETUP_TODO_AND_HIERARCHY.txt` [E], 규약은 루트 `CODING_CONVENTIONS.md`.

---

## §0 부트스트랩

- **코드:** `Assets/_Game/Scripts/Controllers/MeshCutting/` (`Core/`, `Camera/`, `Sandbox/`)
- **컷:** `MeshCutter.Cut(mesh, Plane local, int capSubMeshIndex = -1)` — 평면 1장씩; 연속·곡선은 상위에서 반복 호출. `capSubMeshIndex=-1` 은 기존 캡 없음.
- **규칙 C:** `n ∝ Normalize((B−A) × camera.forward)`; 드래그 중 레이 카메라는 Down 시 고정.
- **카메라 (`CutCameraRig`):** 시작 `TOP_ONLY` · **Q/E** → 탑+측면 · **R** → 탑만 · 패널별 레이 `GetRayCameraForScreenPoint`.
- **드래그 컷:** `MeshCutSandboxDragInput` → `TryCutFromWorldPlane(..., cutSource)`. Down·Up **같은 `MeshFilter`**만. 조각 스폰 시 **BoxCollider**(mesh.bounds). 원본 첫 컷은 씬 **Collider** 필요.
- **캡:** Above `-planeNormal`, Below `+planeNormal`. `MeshCutCap` 캡 UV는 평면 `tangent/bitangent` 투영 후 0..1 정규화.
- **subMesh 규칙:** 원본 외피 subMesh는 번호별로 보존하고, 캡은 항상 **마지막 subMesh**. 원본 첫 컷은 `capSubMeshIndex=-1`, 스폰 조각 재절단은 `mesh.subMeshCount - 1`.
- **구현 상태:** `MeshBuffer` 는 외피를 `_shellTrisBySubMesh`, 캡을 `capTris` 로 분리. `MeshCutter` 는 `MeshCutterWorkBuffer` 를 재사용하고 `GetVertices/GetUVs/GetNormals/GetTriangles(List)` 로 배열 할당을 줄임.
- **머티리얼:** `MeshCutPieceSpawn` 은 source renderer의 외피 머티리얼 배열을 보존하고 마지막 슬롯에 cap material을 붙임. 스폰 조각 제거 시 런타임 생성 Mesh도 함께 Destroy.
- **DI:** 엄격한 DI 적용. Core 내부 helper 직접 `new` 금지. `MeshCutSandboxController.Awake/EnsureCore` → `BuildMeshCutter()` 에서 `MeshCutEdge`, `MeshCutTriangle`, `MeshCutSplitProcessor`, `MeshCutSplit`, `MeshCutHull`, `MeshCutWeld`, `MeshCutCap`, `MeshCutterProcessor`, `MeshCutterWorkBuffer` 를 조립. 테스트용 `Construct(...)` 유지.
- **검증:** 2026-05-17 `dotnet build "OrganicEngineer.sln"` 성공(오류 0). 기존 경고는 Easy Save/Settings 쪽. Cursor `ReadLints` 는 stale 진단을 보일 수 있으니 빌드 결과 우선.
- **최종 방향:** 모델러가 내부 장기/층을 구현하고, 마스킹 쉐이더로 외부를 들어내 내부를 노출한다. 컷 캡은 현재 노출 레이어(피부/근육/장기 등)에 맞춰 생성·머티리얼 선택.

## 다음 작업

| 우선 | 항목 |
|------|------|
| 1 | Unity Play Mode 검증 — 다중 외피 subMesh 모델 1회 컷/재절단, 머티리얼 슬롯, 마지막 캡 subMesh 확인 |
| 2 | DI 조립 위치 정리 검토 — 샌드박스 밖 실게임 진입점이 생기면 `BuildMeshCutter()` 역할을 별도 factory/composition root 로 이동 |
| 3 | Reset 일부(마지막 컷만 취소) — `CutStep(source, above, below)` 스택; source는 Destroy 대신 비활성화 |
| 4 | 캡/외피 subMesh 테스트 보강 — 순수 C# 테스트 가능하면 `MeshCutter.Cut(..., capSubMeshIndex)` 결과 subMeshCount/triangles 검증 |
| 5 | 레이어 컷 설계 — 마스킹 깊이/레이어별 캡 머티리얼/내부 장기 파츠 연결 |
| 6 | §C: 다중 평면·떨림 → 깊이 → W/S·A/D → 레이어 컷 → 파츠·키메라 |

*갱신: 2026-05-17 — 엄격한 DI, 반복 컷 작업 버퍼 재사용, 다중 외피 subMesh/마지막 캡 subMesh 규칙 반영.*
