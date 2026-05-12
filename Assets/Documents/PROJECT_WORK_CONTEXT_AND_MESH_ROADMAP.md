# OrganicEngineer — 작업 이어하기용 컨텍스트 문서

이 파일이 **메시 컷·카메라·컷 플레이 플로우** 관련 세션 컨텍스트의 단일 진실 원천이다. 씬 허브·설정 등은 `SCENE_FLOW.md`, `SETUP_TODO_AND_HIERARCHY.txt`, 루트 `CODING_CONVENTIONS.md`를 본다. 알고리즘 채팅 내보내기는 레포 바깥 `Downloads/cursor_3d_mesh_cutting*.md` 등.

---

## 0. 새 세션·에이전트용 부트스트랩 (이 블록만 붙여도 됨)

- Unity **URP**; 메시 컷은 **외부 에셋 없이** `Assets/02_Scripts/006_MeshCutting/` 에 구현됨.
- **한 번에 하나의 평면**만 `MeshCutter.Cut(mesh, Plane local)`. 다중/곡선 컷은 **평면을 여러 번** 호출하는 상위 레이어.
- **규칙 C**: 레이로 얻은 표면 두 점 `A`,`B` 와 **활성 컷 카메라**의 `forward` 로 법선 `n ∝ (B−A) × forward`. 마우스 레이와 전환 모두 **같은 활성 카메라** 기준.
- **카메라**: 탑 1 + 측면 4, 화면은 탑 + 측면 1; **Q/E** 로 측면 전환. 리그 클래스는 **`CutCameraRig`** (`Camera/` 폴더).
- **다음 코딩 우선순위**: 활성 카메라 + Down/Up 레이 → 규칙 C → 로컬 `Plane` → `Cut`; 그다음 연속 컷 시 **어느 조각을 자를지** 규칙·Reset 범위. **파츠 저장·키메라·도구 종류 분기**는 후순위.

---

## 1. 프로젝트·게임 성격

- **타이틀**: OrganicEngineer.
- **허브 씬**(문서화됨): `BootScene` → `TitleScene` → `LobbyScene` → `GameBattle` / `GameAnalysis` / `GameDissection` / `GameAssembly` 후 로비 복귀. (`SCENE_FLOW.md` 등.)
- **컨셉**: 몬스터 **분해 + 재조립**(자르기 → 파츠 저장 → 키메라). 프로토타입에서는 **자르기(평면 컷)** 우선.
- **3D 프로토 목표**: 표면 따라 노출되는 단면, 이후 저장·조립 규약은 미완.

---

## 2. 기술 방향 요약

| 영역 | 방향 |
|------|------|
| **메시 컷(Core)** | 평면 1개당 `MeshCutter.Cut`. 다중 평면은 상위 파이프라인에서 순차 호출. 단면 캡은 현재 **볼록**(hull + fan); 오목은 ear-clipping 등 후속. |
| **입력(최종)** | 마우스 드래그 컷; 손 **떨림**은 단면에 굴곡으로 반영(다평면·노이즈 등 후속). |
| **평면(활성 카메라)** | **규칙 C**: `n ∝ Normalize((B−A) × 카메라.forward)`. Q/E 로 바뀐 측면 카메라가 `ScreenPointToRay`·`forward` 기준. |
| **내부 레이어** | 근육·뼈 등 **별도 메시**, 한 캐릭터 안 고정; 없으면 비워도 됨. 단면에서는 URP **클립/마스크 쉐이더**로 내부 가시화 예정. |
| **레이어 컷 정책** | **갈래 A**: 같은 평면으로 외피·내부 레이어를 **함께** 자르기. 레이어가 비면 호출 순서만 준비. |
| **렌더링** | URP + Shader Graph. |
| **참고만** | EzySlice 등 외부 슬라이서는 학습용; 런타임 의존 없음. |

---

## 3. 카메라·컨트롤 확정 사항

### 카메라 5대 구성

- **탑 1**: 몬스터를 눕혔을 때 위에서 보는 탑뷰.
- **측면 4**: 서로 다른 방향 4대. 화면에는 **탑 + 선택된 측면 1** (예: `Camera.rect` 분할).
- **Q / E**: 표시되는 측면만 바꿈(활성 1대, 나머지 `enabled=false` 등).

### 측면 카메라의 목적

- 도구 **깊이·각도** 확인용 참고 뷰. **라면 컵을 세로로 반 자른 단면**처럼 단면 형태 확인. 네 방향으로 같은 단면 종류를 서로 다른 시점에서 검증.

### 다른 입력 W/S · A/D (기획 확인)

| 키 | 의미 |
|----|------|
| W/S | 측면 뷰 기준 도구 높낮이 |
| A/D | 도구 각도(외과 시뮬레이터 류 회전 컨셉) |
| 마우스 | 좌클릭 드래그로 접촉선 따라 컷 |

### 깊이(Depth)

- 왕복 애니 **용도 아님**.
- 목적은 **연결된 덩어리 유지**(일정 깊이에서 멈춤 등). 예: 지붕 꼭짓점까지만 들어가고 뚫고 나가면 안 됨.

### 플레이 초기 단순화

- 펜/톱 등은 **도구 하나**로 통합. 펜 선 단계 없이 **드래그·평면 컷만**.

### 구현 참고 (기획과 대응)

- **`MonsterAnchor`** 의 Renderer Bounds 로 거리·`orthographicSize` 등을 맞추려면 **`CutCameraAutoLayout`**.
- **`CutCameraSwitchInput`**: Update + `GetKeyDown` 으로 Q/E, **`CutCameraRig`** 에 위임(`CODING_CONVENTIONS.md`: 코루틴 미사용).

---

## 4. 코드베이스 현황

### 루트

- `Assets/02_Scripts/006_MeshCutting/`

### Core

- **`MeshCutter.cs`** — 로컬 `Plane` 절단, `MeshBuffer`, 캡 등.
- **`CutResult.cs`** — `CutPlaneRelation`, `aboveMesh` / `belowMesh`, `CutEdge` 등.
- **`MeshBuffer.cs`**, **`MeshCutEdge.cs`**, **`MeshCutTriangle.cs`** — 버퍼·교차 처리.
- **`MeshCutHull.cs`**, **`MeshCutCap.cs`** — 단면 볼록 캡.
- **`MeshCutSplit.cs`**, **`MeshCutWeld.cs`** — 분할 보조·울드(있으면 호출 경로 확인).

### Camera (`006_MeshCutting/Camera/`)

- **`CutCameraRig`** — 탑 1·측면 4 보유, 활성 측면 인덱스·모드.

  - 헬퍼: `CutCameraActive`, `CutCameraIndex`, 모드 관련 타입 등이 같은 폴더에 있음 (`CutCameraSwitch`, `CutCameraTop`, …).

- **`CutCameraSwitchInput`** — Q/E 입력.

- 레이아웃·배치:**`CutCameraAutoLayout`**, `CutCameraPlace`, `CutCameraSize`, `CutBounds` 등은 씬/앵커에 맞춰 조정할 때 참고.

### Sandbox

- **`MeshCutSandboxController.cs`** — Odin `[Button]` Cut/Reset, 월드→로컬 `Plane`, 컷 플로우 실험.
- **`CutPlaneConvert.cs`** — 평면 변환 보조.
- **`MeshCutPieceSpawn.cs`** — 절단 메시를 조각 오브젝트로 생성·목록 관리.

### 검증 상태

- **평면 1회 컷**은 안정 검증 완료로 가정(샌드박스 기준).

---

## 5. 지금 해야 하는 것 · 순서

### A. 즉시(입력 레이어만)

1. **활성 컷 카메라** 하나로 `ScreenPointToRay`·`forward` 고정(Q/E 변경 시 갱신).
2. 마우스 Down/Up 레이캐스트로 `A`,`B` (실패 시 컷 안 함; **Collider 필요**).
3. 규칙 C → 월드 `Plane` → 타깃 **로컬**(`InverseTransformDirection` / `InverseTransformPoint`) → `MeshCutter.Cut`, 기존 스폰 로직과 맞춤.
4. **외적 근처 0**(드래그가 카메라 깊이와 거의 평행)이면 컷 스킵.

### B. 바로 다음

5. **연속 재컷** 시 레이캐스트 등으로 **`MeshFilter`/조각 선택 규칙**을 명문화한 뒤 다시 `Cut`.
6. **Reset** 범위(전부 vs 일부).

### C. 후순위

7. 마우스 궤적 **다중 평면** + **떨림**.
8. 깊이(불완전 절단): 평행 이중 평면·슬립 두께 등 기획 확정 후 연동.
9. W/S·A/D 높낮이·각도를 **컷 평면**에 반영.
10. 내부 레이어 메시 준비 시 **동일 평면으로 레이어별 `Cut`** + 쉐이더 클립 동기화.
11. **파츠 저장·키메라·추가 도구 타입**(펜 등).

---

## 6. 기초 지식 초간 레퍼런스

- **메시**: `vertices`, `triangles`, UV, normals(필요 시 tangents).
- **평면**: 법선 `n`, 평면 위 점 `p0`; `Dot(n, p - p0)` 부호(Unity `Plane`). `MeshCutter`는 정점 side 후 삼각형별 복사/분할.
- **선분–평면**: 교차 비 `t`, UV·법선도 `t` 보간(`EdgeIntersectEpsilon` 등).
- **레이**: `ScreenPointToRay` 에 넘기는 카메라 = **활성 컷 카메라**(규칙 C 와 동일).
- **`Cross(u,v)`**: 두 벡터에 동시에 수직인 법선 방향; **두 점만으로는 평면 미정**이므로 두 번째 축으로 `forward` 사용.
- **볼록 캡**: 교차 에지 → 평면 투영 → 볼록 껍질 → fan. 오목 단면에서는 오차 가능.
- **월드 ↔ 로컬**: 법선 `InverseTransformDirection`, 점 `InverseTransformPoint`; 스케일 주의.

---

## 7. 참고 파일

| 경로 | 용도 |
|------|------|
| `CODING_CONVENTIONS.md` | 코딩 규약 |
| `Assets/01_Resources/001_Documents/SETUP_TODO_AND_HIERARCHY.txt` | 할 일·히어라키 |
| `Assets/01_Resources/001_Documents/SCENE_FLOW.md` | 씬 순서 |
| 본 문서 (`PROJECT_WORK_CONTEXT_AND_MESH_ROADMAP.md`) | 메시 컷·카메라 세션 부트스트랩 |
| `Assets/02_Scripts/006_MeshCutting/**` | 구현 코드 |
| (레포 외) `Downloads/cursor_3d_mesh_cutting*.md` | 알고 채팅 복구용 |

---

## 8. 문서 유지

기획·코드 변경 시 **`§0`**, **`§2`**, **`§3`**, **`§4`**, **`§5`** 를 우선 수정한다. 용어만 바뀌면 **`§6`**. 장문 부록은 **`§9 Appendix`** 추가.

---

*최근 반영: 대화에서 쌓인 캐시(핸드오프)·코드 상태를 통합했다. 평면 1컷 검증 완료 가정, 규칙 C + 활성 카메라, 측면 단면 참고 목적, 깊이는 연결 유지용, 카메라 스크립트는 레포 내 `Camera/` 에 존재함.*
