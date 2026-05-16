using UnityEngine;

namespace OrganicEngineer.MeshCutting.Sandbox
{
    //==============================================================================
    // 기능: MeshCutPiece
    // 설명: 절단으로 생성된 조각의 이동 대상 정보를 보관하고 월드 위치 이동을 수행한다.
    //==============================================================================
    // TODO: 물리 이동 모드가 필요해지면 kinematic 전환 정책 추가
    //
    //==============================================================================
    public class MeshCutPiece : MonoBehaviour
    {
        //-------------------- 맴버 변수 --------------------//

        // 조각 이동에 사용할 Rigidbody 캐시
        private Rigidbody _rigidbody;

        // 이동 가능한 상태인지 여부
        private bool _canMove = true;

        //-------------------- Get 함수 --------------------//

        // 조각 이동 가능 여부
        public bool CanMove { get => _canMove; }

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutPiece::Construct
        // 설명: 조각 생성 시 외부에서 Rigidbody 의존성을 주입한다.
        //==============================================================================
        public void Construct(Rigidbody body)
        {
            // 주입받은 Rigidbody 를 이동 처리에 사용한다.
            _rigidbody = body;
        }

        //==============================================================================
        // 기능: MeshCutPiece::MoveTo
        // 설명: 조각을 지정한 월드 위치로 이동한다.
        //==============================================================================
        public void MoveTo(Vector3 worldPosition)
        {
            if (!_canMove) return;

            // Rigidbody 가 있으면 물리 시스템과 위치 동기화를 유지한다.
            if (_rigidbody != null)
            {
                _rigidbody.MovePosition(worldPosition);
                return;
            }

            // Rigidbody 가 없는 예외 상황에서는 Transform 으로 직접 이동한다.
            transform.position = worldPosition;
        }

        //==============================================================================
        // 기능: MeshCutPiece::SetCanMove
        // 설명: 외부에서 조각 이동 가능 상태를 변경한다.
        //==============================================================================
        public void SetCanMove(bool canMove)
        {
            _canMove = canMove;
        }
    }
}
