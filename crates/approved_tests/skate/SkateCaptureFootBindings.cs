using System;

// This assembly replaces only engine/interop reads for the approved scenario.
// Identity-rotation transforms describe the controlled mesh, not a native avatar.
namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
    public sealed class Il2CppStructArray<T>
    {
        private readonly T[] values;
        public Il2CppStructArray(T[] source) { values = source; }
        public int Length { get { return values.Length; } }
        public T this[int index] { get { return values[index]; } }
    }

    public sealed class Il2CppReferenceArray<T>
    {
        private readonly T[] values;
        public Il2CppReferenceArray(T[] source) { values = source; }
        public int Length { get { return values.Length; } }
        public T this[int index] { get { return values[index]; } }
    }
}

namespace UnityEngine
{
    using Il2CppInterop.Runtime.InteropTypes.Arrays;

    public class Object
    {
        private static long next;
        public IntPtr Pointer { get; } = new IntPtr(++next);
        public static void Destroy(Object value) { }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float sqrMagnitude { get { return x * x + y * y + z * z; } }
        public Vector3 normalized
        {
            get { float length = (float)Math.Sqrt(sqrMagnitude); return length == 0 ? default(Vector3) : this / length; }
        }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator *(Vector3 a, float scale) { return new Vector3(a.x * scale, a.y * scale, a.z * scale); }
        public static Vector3 operator /(Vector3 a, float scale) { return new Vector3(a.x / scale, a.y / scale, a.z / scale); }
        public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
        public static Vector3 Cross(Vector3 a, Vector3 b)
        { return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x); }
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public static Quaternion identity { get { return new Quaternion { w = 1 }; } }
    }

    public struct Matrix4x4
    {
        public float m00, m01, m02, m03, m10, m11, m12, m13;
        public float m20, m21, m22, m23, m30, m31, m32, m33;
        public static Matrix4x4 identity { get { return new Matrix4x4 { m00 = 1, m11 = 1, m22 = 1, m33 = 1 }; } }
    }

    public sealed class Transform : Object
    {
        public Transform parent;
        public Vector3 localPosition, localScale = new Vector3(1, 1, 1);
        public Quaternion localRotation = Quaternion.identity;
        public Vector3 position { get { return parent == null ? localPosition : parent.position + localPosition; } }
        public Matrix4x4 localToWorldMatrix
        {
            get
            {
                var p = position;
                return new Matrix4x4 { m00 = localScale.x, m11 = localScale.y, m22 = localScale.z,
                    m03 = p.x, m13 = p.y, m23 = p.z, m33 = 1 };
            }
        }
    }

    public struct Bounds { public Vector3 min, max; }
    public struct BoneWeight
    {
        public int boneIndex0, boneIndex1, boneIndex2, boneIndex3;
        public float weight0, weight1, weight2, weight3;
    }
    public enum SkinWeights { OneBone = 1, TwoBones = 2, FourBones = 4 }

    public sealed class Mesh : Object
    {
        public Vector3[] SourceVertices;
        public int[] SourceTriangles;
        public BoneWeight[] SourceWeights;
        public Matrix4x4[] SourceBindposes;
        public bool isReadable = true;
        public int DeclaredCount = -1, blendShapeCount;
        public SkinWeights skinWeightBufferLayout = SkinWeights.FourBones;
        public Bounds bounds = new Bounds { min = new Vector3(-1, -1, -1), max = new Vector3(1, 1, 1) };
        public int vertexCount { get { return DeclaredCount < 0 ? SourceVertices.Length : DeclaredCount; } }
        public Il2CppStructArray<Vector3> vertices { get { return SourceVertices == null ? null : new Il2CppStructArray<Vector3>(SourceVertices); } }
        public Il2CppStructArray<int> triangles { get { return SourceTriangles == null ? null : new Il2CppStructArray<int>(SourceTriangles); } }
        public Il2CppStructArray<BoneWeight> boneWeights { get { return SourceWeights == null ? null : new Il2CppStructArray<BoneWeight>(SourceWeights); } }
        public Il2CppStructArray<Matrix4x4> bindposes { get { return SourceBindposes == null ? null : new Il2CppStructArray<Matrix4x4>(SourceBindposes); } }
    }

    public sealed class SkinnedMeshRenderer : Object
    {
        public Mesh Mesh;
        public int SharedReads, Bakes;
        public float[] BakeOffsets = new[] { 0f };
        public Exception ReadFailure;
        public Transform transform = new Transform();
        public int quality = 4;
        public Il2CppReferenceArray<Transform> bones;
        public Mesh sharedMesh
        {
            get { SharedReads++; if (ReadFailure != null) throw ReadFailure; return Mesh; }
        }
        public void BakeMesh(Mesh output, bool useScale)
        {
            float offset = BakeOffsets[Math.Min(Bakes++, BakeOffsets.Length - 1)];
            output.SourceVertices = (Vector3[])Mesh.SourceVertices.Clone();
            for (int n = 0; n < output.SourceVertices.Length; n++) output.SourceVertices[n].z += offset;
            output.SourceTriangles = (int[])Mesh.SourceTriangles.Clone();
            output.DeclaredCount = Mesh.DeclaredCount;
        }
    }

    public static class Time { public static float realtimeSinceStartup; }
}
