using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    public readonly struct BoundedCutPlane
    {
        private const float MinLengthSqr = 1e-10f;

        public Plane PlaneLocal { get; }
        public Vector3 SegmentStartLocal { get; }
        public Vector3 SegmentEndLocal { get; }
        public float SpanPadding { get; }
        public float BandPadding { get; }

        private readonly Vector3 _axisLocal;
        private readonly float _length;

        public bool IsValid => _length * _length > MinLengthSqr;

        public BoundedCutPlane(Plane planeLocal, Vector3 segmentStartLocal, Vector3 segmentEndLocal, float spanPadding)
            : this(planeLocal, segmentStartLocal, segmentEndLocal, spanPadding, spanPadding)
        {
        }

        public BoundedCutPlane
        (
            Plane planeLocal,
            Vector3 segmentStartLocal,
            Vector3 segmentEndLocal,
            float spanPadding,
            float bandPadding
        )
        {
            PlaneLocal = planeLocal;
            SegmentStartLocal = segmentStartLocal;
            SegmentEndLocal = segmentEndLocal;
            SpanPadding = Mathf.Max(0f, spanPadding);
            BandPadding = Mathf.Max(0f, bandPadding);

            Vector3 delta = segmentEndLocal - segmentStartLocal;
            _length = delta.magnitude;
            _axisLocal = _length > Mathf.Sqrt(MinLengthSqr) ? delta / _length : Vector3.right;
        }

        public bool TriangleOverlapsSpan(Vector3 a, Vector3 b, Vector3 c)
        {
            float pa = ProjectOnSegmentAxis(a);
            float pb = ProjectOnSegmentAxis(b);
            float pc = ProjectOnSegmentAxis(c);

            float min = Mathf.Min(pa, Mathf.Min(pb, pc));
            float max = Mathf.Max(pa, Mathf.Max(pb, pc));

            return max >= -SpanPadding && min <= _length + SpanPadding;
        }

        public bool CutEdgeOverlapsSpan(Vector3 a, Vector3 b)
        {
            float pa = ProjectOnSegmentAxis(a);
            float pb = ProjectOnSegmentAxis(b);

            float min = Mathf.Min(pa, pb);
            float max = Mathf.Max(pa, pb);
            if (max < -SpanPadding || min > _length + SpanPadding) return false;

            float midpoint = (pa + pb) * 0.5f;
            return midpoint >= -SpanPadding && midpoint <= _length + SpanPadding;
        }

        public bool TriangleTouchesSegmentBand(Vector3 a, Vector3 b, Vector3 c)
        {
            if (PointTouchesSegmentBand(a)) return true;
            if (PointTouchesSegmentBand(b)) return true;
            if (PointTouchesSegmentBand(c)) return true;

            Vector3 center = (a + b + c) / 3f;
            return PointTouchesSegmentBand(center);
        }

        public bool PointTouchesSegmentBand(Vector3 point)
        {
            float projection = ProjectOnSegmentAxis(point);
            if (projection < -SpanPadding || projection > _length + SpanPadding) return false;

            float clampedProjection = Mathf.Clamp(projection, 0f, _length);
            Vector3 closest = SegmentStartLocal + _axisLocal * clampedProjection;
            Vector3 offset = point - closest;
            return offset.sqrMagnitude <= BandPadding * BandPadding;
        }

        private float ProjectOnSegmentAxis(Vector3 point)
        {
            return Vector3.Dot(point - SegmentStartLocal, _axisLocal);
        }
    }

    static class MeshCutIslandUtility
    {
        private const float WeldScale = 1e4f;

        public static void KeepSmallestIslandTouchingSegment(CutResult result, BoundedCutPlane boundedPlaneLocal)
        {
            if (result == null || result.Relation != CutPlaneRelation.STRADDLING) return;

            List<MeshIsland> islands = new List<MeshIsland>();
            CollectIslands(result.AboveMesh, boundedPlaneLocal, islands);
            CollectIslands(result.BelowMesh, boundedPlaneLocal, islands);

            MeshIsland cutIsland = null;
            for (int i = 0; i < islands.Count; i++)
            {
                MeshIsland island = islands[i];
                if (!island.TouchesSegment) continue;
                if (cutIsland == null || island.Area < cutIsland.Area)
                    cutIsland = island;
            }

            if (cutIsland == null) return;

            List<MeshIsland> cutIslands = new List<MeshIsland> { cutIsland };
            List<MeshIsland> restIslands = new List<MeshIsland>();
            for (int i = 0; i < islands.Count; i++)
            {
                if (!ReferenceEquals(islands[i], cutIsland))
                    restIslands.Add(islands[i]);
            }

            result.AboveMesh = BuildMesh(cutIslands);
            result.BelowMesh = BuildMesh(restIslands);
            result.Relation = result.AboveMesh != null && result.BelowMesh != null
                ? CutPlaneRelation.STRADDLING
                : MeshCutUtility.ResolveRelation(result.AboveMesh != null, result.BelowMesh != null);
        }

        private static void CollectIslands(Mesh mesh, BoundedCutPlane boundedPlaneLocal, List<MeshIsland> output)
        {
            if (mesh == null) return;

            Vector3[] verts = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            Vector3[] normals = mesh.normals;
            List<TriangleRef> triangles = new List<TriangleRef>();
            int subMeshCount = mesh.subMeshCount;

            for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
            {
                int[] indices = mesh.GetTriangles(subMesh);
                for (int i = 0; i < indices.Length; i += 3)
                    triangles.Add(new TriangleRef(subMesh, indices[i], indices[i + 1], indices[i + 2]));
            }

            if (triangles.Count == 0) return;

            DisjointSet set = new DisjointSet(triangles.Count);
            Dictionary<Vector3Int, int> firstTriangleByPoint = new Dictionary<Vector3Int, int>();
            for (int triIndex = 0; triIndex < triangles.Count; triIndex++)
            {
                TriangleRef tri = triangles[triIndex];
                UnionPoint(firstTriangleByPoint, set, verts[tri.A], triIndex);
                UnionPoint(firstTriangleByPoint, set, verts[tri.B], triIndex);
                UnionPoint(firstTriangleByPoint, set, verts[tri.C], triIndex);
            }

            Dictionary<int, MeshIsland> islandByRoot = new Dictionary<int, MeshIsland>();
            int capSubMeshIndex = subMeshCount - 1;
            for (int triIndex = 0; triIndex < triangles.Count; triIndex++)
            {
                int root = set.Find(triIndex);
                if (!islandByRoot.TryGetValue(root, out MeshIsland island))
                {
                    island = new MeshIsland(mesh, subMeshCount);
                    islandByRoot[root] = island;
                    output.Add(island);
                }

                TriangleRef tri = triangles[triIndex];
                island.Triangles.Add(tri);

                Vector3 a = verts[tri.A];
                Vector3 b = verts[tri.B];
                Vector3 c = verts[tri.C];
                island.Area += Vector3.Cross(b - a, c - a).magnitude * 0.5f;

                if (tri.SubMesh == capSubMeshIndex && boundedPlaneLocal.TriangleTouchesSegmentBand(a, b, c))
                    island.TouchesSegment = true;
            }
        }

        private static Mesh BuildMesh(List<MeshIsland> islands)
        {
            if (islands == null || islands.Count == 0) return null;

            int subMeshCount = 1;
            for (int i = 0; i < islands.Count; i++)
                subMeshCount = Mathf.Max(subMeshCount, islands[i].SubMeshCount);

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<List<int>> trisBySubMesh = new List<List<int>>();
            for (int i = 0; i < subMeshCount; i++)
                trisBySubMesh.Add(new List<int>());

            for (int islandIndex = 0; islandIndex < islands.Count; islandIndex++)
            {
                MeshIsland island = islands[islandIndex];
                Vector3[] srcVerts = island.Source.vertices;
                Vector2[] srcUVs = island.Source.uv;
                Vector3[] srcNormals = island.Source.normals;

                for (int triIndex = 0; triIndex < island.Triangles.Count; triIndex++)
                {
                    TriangleRef tri = island.Triangles[triIndex];
                    int a = AddVertex(srcVerts, srcUVs, srcNormals, tri.A, verts, uvs, normals);
                    int b = AddVertex(srcVerts, srcUVs, srcNormals, tri.B, verts, uvs, normals);
                    int c = AddVertex(srcVerts, srcUVs, srcNormals, tri.C, verts, uvs, normals);
                    trisBySubMesh[tri.SubMesh].Add(a);
                    trisBySubMesh[tri.SubMesh].Add(b);
                    trisBySubMesh[tri.SubMesh].Add(c);
                }
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.subMeshCount = subMeshCount;
            for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                mesh.SetTriangles(trisBySubMesh[subMesh], subMesh);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static int AddVertex
        (
            Vector3[] srcVerts,
            Vector2[] srcUVs,
            Vector3[] srcNormals,
            int sourceIndex,
            List<Vector3> verts,
            List<Vector2> uvs,
            List<Vector3> normals
        )
        {
            int index = verts.Count;
            verts.Add(srcVerts[sourceIndex]);
            uvs.Add(sourceIndex < srcUVs.Length ? srcUVs[sourceIndex] : Vector2.zero);
            normals.Add(sourceIndex < srcNormals.Length ? srcNormals[sourceIndex] : Vector3.up);
            return index;
        }

        private static void UnionPoint(Dictionary<Vector3Int, int> firstTriangleByPoint, DisjointSet set, Vector3 point, int triIndex)
        {
            Vector3Int key = Quantize(point);
            if (firstTriangleByPoint.TryGetValue(key, out int otherTriIndex))
            {
                set.Union(triIndex, otherTriIndex);
                return;
            }

            firstTriangleByPoint[key] = triIndex;
        }

        private static Vector3Int Quantize(Vector3 value)
        {
            return new Vector3Int(
                Mathf.RoundToInt(value.x * WeldScale),
                Mathf.RoundToInt(value.y * WeldScale),
                Mathf.RoundToInt(value.z * WeldScale)
            );
        }

        private sealed class MeshIsland
        {
            public Mesh Source { get; }
            public int SubMeshCount { get; }
            public List<TriangleRef> Triangles { get; } = new List<TriangleRef>();
            public bool TouchesSegment { get; set; }
            public float Area { get; set; }

            public MeshIsland(Mesh source, int subMeshCount)
            {
                Source = source;
                SubMeshCount = subMeshCount;
            }
        }

        private readonly struct TriangleRef
        {
            public int SubMesh { get; }
            public int A { get; }
            public int B { get; }
            public int C { get; }

            public TriangleRef(int subMesh, int a, int b, int c)
            {
                SubMesh = subMesh;
                A = a;
                B = b;
                C = c;
            }
        }

        private sealed class DisjointSet
        {
            private readonly int[] _parent;

            public DisjointSet(int count)
            {
                _parent = new int[count];
                for (int i = 0; i < count; i++)
                    _parent[i] = i;
            }

            public int Find(int value)
            {
                while (_parent[value] != value)
                {
                    _parent[value] = _parent[_parent[value]];
                    value = _parent[value];
                }

                return value;
            }

            public void Union(int a, int b)
            {
                int rootA = Find(a);
                int rootB = Find(b);
                if (rootA == rootB) return;
                _parent[rootB] = rootA;
            }
        }
    }
}
