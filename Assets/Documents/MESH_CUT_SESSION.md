# 메시 컷 — 세션 핸드오프 (짧게)

메시 컷 작업 시 **이 파일 §0만** 붙이거나 @ 한다. 전체 ToDo·씬은 `SETUP_TODO_AND_HIERARCHY.txt` [E], 규약은 루트 `CODING_CONVENTIONS.md`.

---

## §0 부트스트랩

- **코드:** `Assets/_Game/Scripts/Controllers/MeshCutting/` (`Core/`, `Camera/`, `Sandbox/`)
- **컷:** `MeshCutter.Cut(mesh, Plane local)` — 평면 1장씩; 연속·곡선은 상위에서 반복 호출.
- **규칙 C:** `n ∝ Normalize((B−A) × camera.forward)`; 드래그 중 레이 카메라는 Down 시 고정.
- **카메라 (`CutCameraRig`):** 시작 `TOP_ONLY` · **Q/E** → 탑+측면 · **R** → 탑만 · 패널별 레이 `GetRayCameraForScreenPoint`.
- **드래그 컷:** `MeshCutSandboxDragInput` → `TryCutFromWorldPlane(..., cutSource)`. Down·Up **같은 `MeshFilter`**만. 조각 스폰 시 **BoxCollider**(mesh.bounds). 원본 첫 컷은 씬 **Collider** 필요.
- **캡:** Above `-planeNormal`, Below `+planeNormal`. **UV 미구현** (`MeshCutCap`).

## 다음 작업

| 우선 | 항목 |
|------|------|
| 1 | 캡 UV (`MeshCutCap`) |
| 2 | Reset 일부(마지막 컷만 취소) — 현재 `ResetAll`만 |
| 3 | §C: 다중 평면·떨림 → 깊이 → W/S·A/D → 레이어 컷 → 파츠·키메라 |

*갱신: 2026-05 — 연속 컷·BoxCollider·탑 시작·캡 법선 반영.*
