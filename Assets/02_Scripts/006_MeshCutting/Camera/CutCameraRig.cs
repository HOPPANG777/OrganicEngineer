using Sirenix.OdinInspector;
using UnityEngine;

namespace OrganicEngineer.MeshCutting.CutCamera
{
    //==============================================================================
    // 기능: CutCameraRig
    // 설명: 탑 카메라 1·측면 카메라 4대를 보유하며 활성 측면 인덱스를 관리한다.
    //==============================================================================
    // TODO: enum 라벨(Front/Right/Back/Left) 도입, OnSideCameraChanged 활용처 확장
    //
    //==============================================================================
    public class CutCameraRig : MonoBehaviour
    {
        //-------------------- 맴버 변수 --------------------//

        [TabGroup("Camera")]
        [Tooltip("탑뷰 카메라(항상 켜짐)")]
        [SerializeField] private Camera _topCamera;

        [TabGroup("Camera")]
        [Tooltip("측면 4대(Front/Right/Back/Left 순)")]
        [SerializeField] private Camera[] _sideCamera;

        [TabGroup("Camera")]
        [Tooltip("시작 시 활성화 할 측면 인덱스")]
        [SerializeField] private int _initialSideIndex;

        //-------------------- 내부 변수 --------------------//

        private int _activeSideIndex;                               // 현재 활성화 되어 있는 측면 카메라
        private CutCameraMode _mode = CutCameraMode.TOP_PLUS_SIDE;  // 현재 모드

        //-------------------- Get 함수 --------------------//

        // 탑뷰 카메라를 반환하는 getter
        public Camera TopCamera { get => _topCamera; }

        // 활성화된 측면 카메라의 인덱스를 반환하는 getter
        public int ActiveSideIndex { get => _activeSideIndex; }

        // 카메라 모드
        public CutCameraMode Mode { get => _mode; }

        // 현재 활성화된 측면 카메라를 반환하는 getter
        // 탑뷰만 모드일 때는 활성 측면이 없음 → null 반환
        public Camera ActiveSideCamera
        {
            get
            {
                if (_mode == CutCameraMode.TOP_ONLY) return null;
                if (_sideCamera == null) return null;
                if (_activeSideIndex < 0 || _activeSideIndex >= _sideCamera.Length) return null;
                return _sideCamera[_activeSideIndex];
            }
        }

        // 측면 카메라 배열의 길이(없거나 비어 있으면 0)
        public int SideCameraCount { get => _sideCamera == null ? 0 : _sideCamera.Length; }

        // 슬롯 인덱스로 측면 카메라 한 대를 반환(범위 밖이거나 null 이면 null)
        public Camera GetSideCamera(int index)
        {
            if (_sideCamera == null) return null;
            if (index < 0 || index >= _sideCamera.Length) return null;
            return _sideCamera[index];
        }

        //-------------------- 기능 함수 --------------------//

        private void Awake()
        {
            if (_sideCamera == null || _sideCamera.Length == 0)
            {
                Debug.LogWarning("[CutCameraRig] _sideCamera 비어 있음.");
                enabled = false;
                return;
            }
            _activeSideIndex = Mathf.Clamp(_initialSideIndex, 0, _sideCamera.Length - 1);
        }

        private void Start()
        {
            ApplyActive();
        }

        private void OnEnable()
        {
            // Awake가 아직 안 돌았거나 가드된 경우 무시
            if (_sideCamera == null || _sideCamera.Length == 0) return;
            ApplyActive();
        }

        //==============================================================================
        // 기능: CutCameraRig::SetActive
        // 설명: 측면 인덱스를 검증·갱신하고 모드를 TopPlusSide 로 강제한 뒤 동기화·이벤트 발행.
        //==============================================================================
        // TODO: 잘못된 입력에 대한 에러 정책 정리
        //
        //==============================================================================
        // index: 활성화할 측면 카메라 배열 인덱스(0~Length-1)
        public void SetActive(int index)
        {
            // 측면 카메라 배열이 없거나 비어 있으면 처리 불가
            if (_sideCamera == null || _sideCamera.Length == 0) return;

            // 요청 인덱스가 배열 범위를 벗어나면 경고 후 종료
            if (index < 0 || index >= _sideCamera.Length)
            {
                // 잘못된 인덱스 값을 콘솔에 출력
                Debug.LogWarning($"[CutCameraRig] index 범위 밖: {index}");
                return;
            }

            // 활성 측면 인덱스가 바뀌는지 여부(같은 인덱스면 측면 변경 이벤트 생략)
            bool indexChanged = (_activeSideIndex != index);
            // TopOnly 등에서 TopPlusSide 로 돌아오는지 여부
            bool modeChanged = (_mode != CutCameraMode.TOP_PLUS_SIDE);

            // 인덱스도 같고 이미 TopPlusSide 이면 할 일 없음
            if (indexChanged == false && modeChanged == false) return;

            // 현재 활성 측면 인덱스를 요청값으로 갱신
            _activeSideIndex = index;

            // 측면을 켜는 모드로 전환(탑뷰만 모드 해제)
            _mode = CutCameraMode.TOP_PLUS_SIDE;

            // 탑·측면 카메라 enabled 를 모드·인덱스에 맞게 동기화
            ApplyActive();
        }

        //==============================================================================
        // 기능: CutCameraRig::SwitchNext
        // 설명: 활성 측면 카메라 인덱스를 다음 칸(0→1→2→3→0)으로 이동한다.
        //==============================================================================
        // TODO: 입력 잠금/쿨타임 옵션
        //
        //==============================================================================
        public void SwitchNext()
        {
            /*
            // 다음 카메라로 가려면 어떻게 해야할까?
            // 일단 다음으로 이동할 카메라의 인덱스를 구한 뒤에
            // 이전 카메라를 끄고 다음 인덱스의 카메라를 활성화하면 되겠지요?

            1. 다음 카메라의 인덱스를 구한다.
            2. nextIndex = _activeSideIndex = (_activeSideIndex + 1) % 4
            3. SetIndex로 nextIndex를 전해주면 된다.
            */

            // 다음 카메라의 인덱스를 구한다.
            // -> 현재 카메라 인덱스에 1을 더하고,
            // 측면 카메라 갯수로 나머지를 구하면 다음 인덱스가 나온다.
            // 2 + 1 = 3, 3 % 4 = 3;
            int nextIndex = (_activeSideIndex + 1) % _sideCamera.Length;

            // 다음 카메라 활성화
            SetActive(nextIndex);
        }

        //==============================================================================
        // 기능: CutCameraRig::SwitchPrev
        // 설명: 활성 측면 카메라 인덱스를 이전 칸(0→3→2→1→0)으로 이동한다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        public void SwitchPrev()
        {
            // 이전 카메라의 인덱스를 구한다.
            // -> 현재 카메라 인덱스에 측면 카메라 갯수에 -1을 하고
            // 거기에 4으로 나머지를 구하면 이전 엔덱스가 구해진다.
            // 2 + 3 = 5, 5 % 4 = 1;
            int len = _sideCamera.Length;
            int prevIndex = (_activeSideIndex + len - 1) % len;

            // 이전 카메라 활성화
            SetActive(prevIndex);
        }

        //==============================================================================
        // 기능: CutCameraRig::ApplyActive
        // 설명: _mode 와 _activeSideIndex 를 기준으로 탑뷰·측면 카메라 enabled 를 강제 동기화한다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        private void ApplyActive()
        {
            // 탑뷰는 어떤 모드에서도 항상 ON
            if (_topCamera != null)
            {
                _topCamera.enabled = true;
            }

            if (_mode == CutCameraMode.TOP_ONLY)
            {
                // 측면 전부 OFF
                for (int i = 0; i < _sideCamera.Length; i++)
                {
                    if (_sideCamera[i] == null) continue;
                    _sideCamera[i].enabled = false;
                }
            }
            else // TopPlusSide
            {
                // 측면은 활성 인덱스 1대만 ON
                for (int i = 0; i < _sideCamera.Length; i++)
                {
                    if (_sideCamera[i] == null) continue;
                    _sideCamera[i].enabled = (i == _activeSideIndex);
                }
            }
        }

        //==============================================================================
        // 기능: CutCameraRig::EnterTopOnly
        // 설명: 모드를 TopOnly 로 전환하고 측면 카메라를 모두 끈다.
        //==============================================================================
        // TODO: 토글식(R 다시 누르면 마지막 측면 복귀) 옵션
        //
        //==============================================================================
        public void EnterTopOnly()
        {
            if (_mode == CutCameraMode.TOP_ONLY) return;

            _mode = CutCameraMode.TOP_ONLY;
            ApplyActive();
        }


    }   
}
