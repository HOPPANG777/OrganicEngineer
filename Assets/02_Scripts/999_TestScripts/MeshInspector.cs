using UnityEngine;

//==============================================================================
// 기능: MeshInspector
// 설명: 테스트용 MeshFilter 의 기본 메시 정보를 콘솔에 출력한다.
//==============================================================================
// TODO: 런타임 디버그 UI 또는 에디터 전용 도구로 분리
//
//==============================================================================
public class MeshInspector : MonoBehaviour
{
    //-------------------- 맴버 변수 --------------------//

    // 정보를 확인할 테스트 대상 MeshFilter
    public MeshFilter targetCube;

    //==============================================================================
    // 기능: MeshInspector::Start
    // 설명: 시작 시 메시 이름, 정점/삼각형/UV/노멀 수와 일부 샘플 데이터를 로그로 출력한다.
    //==============================================================================
    // TODO: targetCube null 가드와 샘플 개수 옵션
    //
    //==============================================================================
    void Start()
    {
        // 테스트 대상의 공유 Mesh 참조
        Mesh mesh = targetCube.sharedMesh;

        // 메시 기본 정보 출력
        Debug.Log($"메시 이름: {mesh.name}");
        Debug.Log($"정점 수: {mesh.vertexCount}");
        Debug.Log($"삼각형 인덱스 수: {mesh.triangles.Length} (= 삼각형 {mesh.triangles.Length / 3}개)");
        Debug.Log($"UV 수: {mesh.uv.Length}");
        Debug.Log($"법선 수: {mesh.normals.Length}");

        // 앞쪽 정점 샘플 출력
        Debug.Log("=== 처음 4개 정점의 위치 ===");
        for (int i = 0; i < 4; i++)
        {
            Debug.Log($"v[{i}] = {mesh.vertices[i]}, uv = {mesh.uv[i]}, normal = {mesh.normals[i]}");
        }

        // 앞쪽 삼각형 인덱스 샘플 출력
        Debug.Log("=== 처음 2개 삼각형의 인덱스 ===");
        for (int i = 0; i < 6; i += 3)
        {
            Debug.Log($"tri[{i / 3}] = ({mesh.triangles[i]}, {mesh.triangles[i + 1]}, {mesh.triangles[i + 2]})");
        }
    }
}