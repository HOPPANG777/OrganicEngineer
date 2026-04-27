***

# 📝 Unity Game Development Coding Conventions (v2.1)

본 프로젝트의 코드 일관성, 런타임 성능, 그리고 유지보수 효율을 극대화하기 위해 정의된 가이드라인입니다. 모든 팀원은 코드를 작성하거나 리뷰할 때 반드시 이 문서를 기준으로 합니다.

## 1. 핵심 설계 원칙 (Core Architecture)

### 1.1 의존성 및 결합도 (DI & Low Coupling)
* **의존성 주입(DI) 권장**: 클래스 내부에서 직접 `new`를 통해 객체를 생성하거나 `GameObject.Find` 계열 함수를 사용하는 것을 지양합니다.
* **주입 설계**: 생성자나 `Construct` 메서드를 통해 외부에서 주입받도록 설계하세요.
* **낮은 결합도**: 각 클래스는 자신의 역할에만 집중하며, 다른 클래스의 세부 구현을 알지 못하도록 인터페이스(Interface)를 적극 활용합니다.

### 1.2 상속 구조 (Inheritance Limit)
* **1단계 상속 원칙**: 상속은 최대 1단계까지만 허용합니다 (예: `UnitBase` → `Player`).
* **구성(Composition) 중심**: 2단계 이상의 깊은 상속 구조 대신, 기능을 작은 단위의 클래스로 분리하여 필요한 기능을 조립하는 방식을 사용합니다. 이는 '클래스 폭발' 현상을 방지합니다.

---

## 2. 프로그래밍 가이드 및 최적화

### 2.1 비동기 처리 (UniTask)
* **No Coroutine**: 유니티 기본 코루틴은 사용하지 않습니다.
* **UniTask**: 모든 비동기 로직, 대기 시간 처리, 프레임 대기는 UniTask를 사용하여 메모리 효율과 가독성을 확보합니다.

### 2.2 루프 최적화 (Loops)
* **for문 우선**: 리스트나 배열을 순회할 때 가비지 컬렉션(GC) 발생을 방지하기 위해 `foreach` 대신 `for`문 사용을 원칙으로 합니다.

### 2.3 주석 규칙 (Comments & Documentation)
모든 **기능(Class), 메서드(Function), 변수(Field)**에는 역할을 설명하는 주석을 최소 한 줄 이상 작성합니다. 목적에 따라 아래의 규격을 준수합니다.

* **기본 한 줄 주석**: 기능, 변수, 짧은 로직 설명 시 사용합니다.
* **TODO / FIXME 주석**: 추가 작업이나 버그 수정이 필요한 곳에 명시합니다.
  ```csharp
  // TODO: 예외 처리 추가 필요
  // FIXME: 메모리 누수 발생 가능성 있음
  ```
* **문서화 주석**: 클래스나 핵심 메서드의 API 문서를 작성할 때 사용합니다.
  ```csharp
  /*
   * @brief 사용자 이름을 반환한다
   * @param id 사용자 ID
   * @return 사용자 이름
   */
  string getUserName(int id);
  ```
* **구분선 주석**: 파일 내에서 큰 기능 단위나 영역을 나눌 때 사용합니다.
  ```csharp
  //-------------------- 데이터 초기화 --------------------
  ```
* **상세/블록 주석**: 핵심 기능에 대한 상세한 설명이나 세부 구현 사항을 나열할 때 사용합니다.
  ```csharp
  //==============================================================================
  // 기능: MyNameClass::Create
  // 설명: 객체를 생성하는 함수
  //==============================================================================
  // TODO: 세부 구현 사항
  //    
  //==============================================================================
  ```

---

## 3. 네이밍 규칙 (Naming Conventions)

| 분류 | 표기법 (Case) | 예시 | 비고 |
| :--- | :--- | :--- | :--- |
| **클래스 / 구조체** | `PascalCase` | `PlayerController` | 명사형 사용, 역할 명시 |
| **인터페이스** | `IPascalCase` | `IMoveable` | `I` 접두사 필수 |
| **메서드** | `PascalCase` | `TakeDamage()` | 동사형 사용 |
| **열거형(Enum) 상수** | `SCREAMING_SNAKE` | `TILE_WALL` | 타입_태그 형식 |
| **멤버 변수(Private)** | `_camelCase` | `_moveSpeed` | `_` 접두사 사용 |
| **지역/매개 변수** | `camelCase` | `targetPos` | - |

---

## 4. 인스펙터 및 필드 규칙

### 4.1 멤버 변수 및 프로퍼티
* **Private 필드**: `_` 접두사를 사용하며, 외부 노출이 필요하면 프로퍼티로 감쌉니다.
* **프로퍼티**: `get; set;` 로직을 명시하고 대소문자 규칙(`PascalCase`)을 적용하여 캡슐화를 강화합니다.

### 4.2 인스펙터 속성 (Attributes)
* **[SerializeField]**: Private 필드 바로 윗줄에 작성합니다.
* **[Tooltip]**: 모든 직렬화 필드에 필수 작성하여 역할을 명시합니다.
* **그룹화**[cite: 31]:
  * Odin Inspector 사용 시: `[TabGroup("GroupName")]` 사용.
  * Odin 미사용 시: `[Header("GroupName")]` 사용.

---

## 5. 표준 코드 예시 (Best Practice)

아래는 위에서 정의된 규칙들을 종합적으로 적용한 표준 코드 예시입니다.

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using Sirenix.OdinInspector;

// 이동 기능을 정의하는 인터페이스
public interface IMoveable
{
    void Move();
}

// 1단계 상속: 모든 유닛의 베이스 클래스
public abstract class UnitBase : MonoBehaviour
{
    // 유닛 식별자
    protected string _unitId;
}

// 플레이어 컨트롤러: UnitBase를 상속하며 기능별 분리 준수
public class PlayerController : UnitBase
{
    // 이동 관련 설정을 모은 탭 그룹
    [TabGroup("Settings")]
    [Tooltip("캐릭터의 이동 속도")]
    [SerializeField]
    private float _moveSpeed;

    // 프로퍼티를 활용한 캡슐화 (get; set; 로직 명시)
    public float MoveSpeed 
    { 
        get => _moveSpeed; 
        private set => _moveSpeed = value; 
    }

    // 이동 로직 (의존성 주입 대상)
    private IMoveable _moveLogic;

    // 의존성 주입을 위한 생성 메서드
    public void Construct(IMoveable moveLogic)
    {
        _moveLogic = moveLogic;
    }

    // 컴포넌트 캐싱 및 초기화
    private void Awake()
    {
        _unitId = "Player_01";
    }

    // 비동기 시작 로직 (UniTask 활용)
    public async UniTaskVoid InitializeAsync()
    {
        // 1초 비동기 대기
        await UniTask.Delay(1000);
        Debug.Log("플레이어 초기화 완료");
    }

    // 데이터 처리 로직 (for문 활용)
    public void UpdateScore(int[] scores)
    {
        // 최적화를 위해 for문 사용
        for (int i = 0; i < scores.Length; i++)
        {
            Debug.Log($"점수 확인: {scores[i]}");
        }
    }
}
```

> **참고**: 본 문서는 팀 협의를 통해 업데이트될 수 있으며, 수정 시 팀원 전체 공유가 필요합니다.