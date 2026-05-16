# 메시 컷 — 세션 핸드오프 (짧게)

메시 컷 작업 시 **이 파일 §0만** 붙이거나 @ 한다. 전체 ToDo·씬은 `SETUP_TODO_AND_HIERARCHY.txt` [E], 규약은 루트 `CODING_CONVENTIONS.md`.

---

## §0 부트스트랩

- **코드:** `Assets/_Game/Scripts/Controllers/MeshCutting/` (`Core/`, `Camera/`, `Sandbox/`)
- **컷:** `MeshCutter.Cut(mesh, Plane local)` — 평면 1장씩; 연속·곡선은 상위에서 반복 호출.
- **규칙 C:** `n ∝ Normalize((B−A) × camera.forward)`; 드래그 중 레이 카메라는 Down 시 고정.
- **카메라 (`CutCameraRig`):** 시작 `TOP_ONLY` · **Q/E** → 탑+측면 · **R** → 탑만 · 패널별 레이 `GetRayCameraForScreenPoint`.
- **드래그 컷:** `MeshCutSandboxDragInput` → `TryCutFromWorldPlane(..., cutSource)`. Down·Up **같은 `MeshFilter`**만. 조각 스폰 시 **BoxCollider**(mesh.bounds). 원본 첫 컷은 씬 **Collider** 필요.
- **캡:** Above `-planeNormal`, Below `+planeNormal`. `MeshCutCap` 캡 UV는 평면 `tangent/bitangent` 투영 후 0..1 정규화.
- **연속 컷:** 기존 캡 보존은 subMesh 유지가 핵심. `subMesh 0=외피`, `subMesh 1=캡`; 재절단 시 기존 캡 삼각형은 계속 `capTris`, 새 절단면도 `capTris`.
- **최종 방향:** 모델러가 내부 장기/층을 구현하고, 마스킹 쉐이더로 외부를 들어내 내부를 노출한다. 컷 캡은 현재 노출 레이어(피부/근육/장기 등)에 맞춰 생성·머티리얼 선택.

## 다음 작업

| 우선 | 항목 |
|------|------|
| 1 | 기존 캡 subMesh 보존 마무리 — `src.GetTriangles(subMesh)` + `isCap` 전달 |
| 2 | Reset 일부(마지막 컷만 취소) — `CutStep(source, above, below)` 스택; source는 Destroy 대신 비활성화 |
| 3 | 레이어 컷 설계 — 마스킹 깊이/레이어별 캡 머티리얼/내부 장기 파츠 연결 |
| 4 | §C: 다중 평면·떨림 → 깊이 → W/S·A/D → 레이어 컷 → 파츠·키메라 |

*갱신: 2026-05 — 캡 UV·기존 캡 보존 방향·마스킹 레이어 캡 목표 반영.*
