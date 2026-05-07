using UnityEngine;

public class MeshInspector : MonoBehaviour
{
    public MeshFilter targetCube;

    void Start()
    {
        Mesh mesh = targetCube.sharedMesh;

        Debug.Log($"메시 이름: {mesh.name}");
        Debug.Log($"정점 수: {mesh.vertexCount}");
        Debug.Log($"삼각형 인덱스 수: {mesh.triangles.Length} (= 삼각형 {mesh.triangles.Length / 3}개)");
        Debug.Log($"UV 수: {mesh.uv.Length}");
        Debug.Log($"법선 수: {mesh.normals.Length}");

        Debug.Log("=== 처음 4개 정점의 위치 ===");
        for (int i = 0; i < 4; i++)
        {
            Debug.Log($"v[{i}] = {mesh.vertices[i]}, uv = {mesh.uv[i]}, normal = {mesh.normals[i]}");
        }

        Debug.Log("=== 처음 2개 삼각형의 인덱스 ===");
        for (int i = 0; i < 6; i += 3)
        {
            Debug.Log($"tri[{i / 3}] = ({mesh.triangles[i]}, {mesh.triangles[i + 1]}, {mesh.triangles[i + 2]})");
        }
    }
}