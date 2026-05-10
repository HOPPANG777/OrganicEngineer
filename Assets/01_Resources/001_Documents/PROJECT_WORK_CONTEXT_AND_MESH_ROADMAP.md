# OrganicEngineer — 작업 이어하기용 컨텍스트 문서

이 문서는 다른 세션·에이전트·인원이 맥락 없이 이어서 작업할 수 있도록 **기술 방향**, **확정 기획**, **코드 상태**, **다음 과제**, **기초지식 요약**을 한 파일에 묶었다. 상세 채팅은 Cursor 내보낸 MD(`cursor_3d_mesh_cutting_*.md` 등) 또는 `SETUP_TODO_AND_HIERARCHY.txt`, `CODING_CONVENTIONS.md`를 참고한다.

---

## 1. 프로젝트·게임 성격

- **타이틀**: OrganicEngineer.
- **확정된 허브 씬 플로우**(문서화됨): `BootScene` → `TitleScene` → `LobbyScene` → `GameBattle` / `GameAnalysis` / `GameDissection` / `GameAssembly` 후 로비 복귀. (`SCENE_FLOW.md`, `SETUP_TODO_AND_HIERARCHY.txt`).
- **핵심 컨셉**(기획·채팅 병합): **몬스터 분해 + 재조립** — 자르기 → 부위(파츠) 저장 → 조립하여 키메라. 프로토타입에서는 **자르기만** 우선 구현한다.
- **3D 확장**(기존 컨셉 기획서에서 3D 프로토 진행 중): 표면 따라 잘린 부위 노출·저장·조립까지를 목표로 하되 상세 규약은 미완료.

---

## 2. 기술 방향 요약

| 영역 | 방향 |
|------|------|
| **메시 컷(Core)** | **평면 1개** 기준 분할 알고리즘은 `MeshCutter.Cut(mesh, Plane local)`. **다중 평면**(자유 커브 근사)은 **평면을 순차 호출하는 상위 파이프라인**으로 두는 형태가 맞다. 현재 단면 캡은 **볼록 가정**(convex hull + fan); **오목**은 향후 ear-clipping 등. |
| **입력 형태(최종)** | **마우스 드래그**로 자름; **플레이어 손 떨림**이 단면에 **구불구불**하게 반영되어야 한다(고주파·다평면 근사·노이즈 등 후속 과제). |
| **평면 정의 규칙**(활성 카메라 기준) | **규칙 C**: 드래그로 얻은 표면 두 점 `A`,`B`(레이캐스트 히트), 절단면 법선 `n ∝ Normalize( (B−A) × 카메라.forward )`. **Q/E로 바뀐 측면 카메라**가 `ScreenPointToRay` 및 `forward` 의 기준이다. 두 점만 있으면 평면이 무수히 많으므로 **외적 보조축**(카메라 깊이 방향)이 필요하다. |
| **내부 표현**(근육·뼈·장기) | 모델러가 만들어 둔 **별도 메시 레이어**(한 캐릭터 **안에 고정**). 레이어링·아트 미완 단계에서는 **비워 둘 수 있음**. 단면에서는 **마스킹/클립 쉐이더**(URP Shader Graph)로 안쪽 레이어가 보이도록 할 예정. |
| **레이어 절단 정책**(설계 대화 확정) | **갈래 A**: 같은 컷 평면으로 **외피·내부 레이어를 한 번에** 자르는 방향(MD 복구문서 참고). 레이어가 빈 상태면 호출 순서만 맞춰 두면 됨. |
| **렌더 파이프라인** | **URP**, Shader Graph. |
| **메시 알고리즘** | 외부 슬라이서 에셋 없이 직접 구현(scratch). 학습 레퍼런스로 EzySlice 등은 읽기만. |

---

## 3. 카메라·컨트롤 확정 사항

### 카메라 자동 배치(설계)

- 카메라 위치는 손으로 맞추기보다 **`MonsterAnchor` 의 Renderer Bounds** 로 **탑 1 + 측면 4** 거리·`LookAt`·직교 카메라 `orthographicSize` 등을 계산하는 스크립트(**`CutCameraAutoLayout`** 개념)에서 맞추는 방향이다. 수동 트리거 또는 시작 시 한 번 호출 등.

### 입력 전용 분리

- **`CutCameraSwitchInput`**: Update + `GetKeyDown` 으로 Q/E 만 처리하고 **`CutCameraRig`** 에 위임(`CODING_CONVENTIONS.md`: 코루틴 미사용).

### 카메라 5대 구성

- **탑 카메라 1**: **탑뷰**(몬스터를 눕혀 놓았을 때 위에서 본 형태).
- **측면 카메라 4**: **서로 다른 4방향**. 동시 표시 영역에는 **탑 카메라 1 + 측면 중 선택된 1대**(예: 좌우/상하 `Camera.rect` 분할).
- **`Q` / `E`**: 표시되는 **측면 카메라만 전환**(활성 측면 1대, 나머지 `enabled=false` 패턴 등).

### 측면 카메라의 목적

- **도구 깊이·각도 확인용** 참고 뷰. 표시 시 **라면 컵을 수직으로 반으로 나눠 본 단면처럼**(클리핑 단면 형태로 내부 구조 확인). **네 대는 서로 다른 시점**으로 같은 종류의 단면 검증을 돕는다.

### 다른 입력 W/S · A/D (기획자 확인)

| 키 | 의미 |
|----|------|
| W/S | 측면 뷰에서 **도구 높낮이** |
| A/D | 도구 **각도**(예: 외전 시뮬레이터처럼 자르는 방향을 바꿀 때 사용) |
| 마우스 | **좌클릭 드래그**로 도구 접촉 지점 따라 절단(세부 규격은 추가 기획). |

### 깊이(Depth)

- 왕복 애니메이션을 위한 것이 **아니다**.
- 목적은 **연결된 덩어리를 남기기**(예: 특정 깊이에서 통과하지 않고 멈춤). 예: 오두막 지붕을 통째 한 조각으로 남기려면 **꼭짓점 부분까지만** 깊이 들어가고 **완전 관통하면 안 된다.**

### 플레이 초기 버전 단순화

- 도구 종류(펜/톱 등)는 프로토타입에서는 **“도구” 하나로 통합**. 펜 선 그리기 단계 없이 **컷 기능만**(드래그·평면).

---

## 4. 코드베이스 관련 현황

### 메시 커팅 (구현 존재)

- 경로: `Assets/02_Scripts/006_MeshCutting/`
  - `Core/MeshCutter.cs` — 로컬 `Plane`으로 절단, `MeshBuffer`, 볼록 캡 등.
  - `Core/CutResult.cs` — `CutPlaneRelation`, `aboveMesh` / `belowMesh`, 캡용 `CutEdge` 리스트 등.
  - `Core/MeshBuffer.cs`
  - `Sandbox/MeshCutSandboxController.cs` — Odin `[Button]` Cut/Reset, 월드→로컬 `Plane`, 조각 스폰.
- 알고리즘은 **평면 1조각** 단위 완결; 사용자는 **단일 컷 안정 검증 완료**했다고 확인함.

### 컨벤션

- `CODING_CONVENTIONS.md`(v2.2): `GameRoot` 단일 패턴/`Controller` 패턴, **`_camelCase`** private 필드, **`[SerializeField]`+`Tooltip`**, 코루틴 미사용(Update·상태), `for` 선호 핵심 블록 주석 형식 `//=================================================================` 등.
- 새 카메라 모듈 설계안(직접 구현 안 함, 문서로만 안내됨):
  - `CutCameraRig` — 활성 탑/측면 전달, Q/E 상태.
  - `CutCameraAutoLayout` — `MonsterAnchor` + Renderer Bounds로 자동 거리/`orthographicSize`.
  - `CutCameraSwitchInput` — Q/E 입력.

---

## 5. 지금 해야 하는 것 · 순서 체크리스트

다음 순서를 권장한다.

### A. 즉시(입력 레이어만)

1. **활성 카메라** 레퍼런스 하나로 `ScreenPointToRay`, `forward` 고정 (**Q/E로 바뀔 때마다 업데이트**).
2. **마우스 Down/Up** 레이캐스트 → `A`,`B`; 실패 시 컷 호출 안 함 (**Collider 필요**).
3. **규칙 C**로 월드 `Plane` 후 **타깃 로컬**로 변환(`InverseTransformDirection` / `InverseTransformPoint`) 후 `MeshCutter.Cut` — 기존 샌드박스 스폰 정책과 호환.
4. **외적 근처 0**(드래그가 카메라 깊이와 평행)일 때 무시 처리.

### B. 바로 다음

5. **연속 재컷** 시 **레이히트 등으로 다음에 자를 조각의 `MeshFilter`** 를 고르는 규칙을 한 줄로 정한 뒤 다시 `Cut`.
6. **Reset 범위** 정책(전량 vs 일부).

### C. 후순위

7. 마우스 궤적을 **다중 평면 근사**하고 **떨림(미세 진동)** 을 단면 형상에 반영.
8. 깊이(불완전 절단): 평행 두 평면 또는 두께 있는 슬립 등 기획 확정 후 알고리즘 연동.
9. W/S·A/D 로 조정한 높낮이·각도를 **컷 평면** 계산에 합치기.
10. 내부 레이어 메시가 생기면 **각 레이어 동일 평면으로 `Cut`** + **쉐이더 클립** 값 동기화.

---

## 6. 기초 지식 초간 레퍼런스

- **메시 최소 채널**: `vertices`, `triangles`(정점 인덱스 세 개씩 묶음), UV, normals(필요 시 tangents).
- **평면 앞뒤**: 법선 `n`, 평면 위 한 점 `p0`; 점 `p`와의 관계는 `Dot(n, p - p0)` 의 부호로 판별(Unity `Plane`). `MeshCutter`는 정점별 side 후 삼각형별 복사 또는 분할.
- **선분-평면 교차**: 교차 비율 `t` 로 교차점을 구하고 UV·법선도 같은 `t` 로 선형 보간(코드상 `EdgeIntersectEpsilon` 처리).
- **레이**: `Camera.ScreenPointToRay` 에 넘기는 `Camera` 는 **반드시 현재 활성 컷 카메라**(규칙 C 기준과 동일).
- **`Cross(u,v)`**: `u` 와 `v` 에 동시에 수직인 벡터(두 벡터가 펼치는 평면의 법선). 두 점만 있으면 평면이 정해지지 않아 **두 번째 방향**(여기서는 카메라 `forward`) 이 필요하다.
- **볼록 단면 캡**: 교차 에지 모음 -> 평면에 투영 후 볼록 껍질 -> fan. **오목 단면**에서는 근사 오차가 난다.
- **월드 -> 메시 로컬**: 법선은 `InverseTransformDirection`, 점은 `InverseTransformPoint`; **스케일**이 들어가면 혼동하지 말 것.

---

## 7. 중요 참고 파일

| 파일 | 역할 |
|------|------|
| `CODING_CONVENTIONS.md` | 코딩·주석·패턴 규칙 |
| `Assets/01_Resources/001_Documents/SETUP_TODO_AND_HIERARCHY.txt` | 할 일·히어라키·E-Step |
| `Assets/01_Resources/001_Documents/SCENE_FLOW.md` | 씬 순서표 |
| `Assets/02_Scripts/006_MeshCutting/**` | 메시 컷 구현본문 |
| 본 레포 바깥: `Downloads/cursor_3d_mesh_cutting*.md` | 알고리즘·튜토리얼 복구용 내보내기 |

---

## 8. 문서 유지 방법

기획·구현이 바뀌면 이 파일의 **§2·§3·§5·§6** 를 우선 갱신한다. 큰 부록이 필요하면 **§9 Appendix** 를 추가한다.

*최근 정렬: 단일 평면 컷 검증 완료 기준, 규칙 C + Q/E 활성 카메라, 측면 단면 뷰 목적, 깊이 조절 이유(연결 유지), 카메라 자동 레이아웃·`CutCameraRig` 설계 논의 반영.*
