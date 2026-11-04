using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Shortcut.RustMod;
using UnityEngine;
using UnityEngine.Rendering;

public static class SkateCaptureFootScenarios
{
    private static int passed, failed;

    private sealed class Model
    {
        internal readonly Transform LeftFoot = new Transform { localPosition = new Vector3(-.2f, 0, 0) };
        internal readonly Transform RightFoot = new Transform { localPosition = new Vector3(.2f, 0, 0) };
        internal readonly Transform LeftToe, RightToe;
        internal readonly SkinnedMeshRenderer Renderer;

        internal Model()
        {
            LeftToe = new Transform { parent = LeftFoot, localPosition = new Vector3(0, 0, .12f) };
            RightToe = new Transform { parent = RightFoot, localPosition = new Vector3(0, 0, .12f) };
            var weights = new BoneWeight[6];
            for (int n = 0; n < weights.Length; n++) weights[n] = new BoneWeight { boneIndex0 = n < 3 ? 0 : 2, weight0 = 1 };
            var bind = new Matrix4x4[5];
            for (int n = 0; n < bind.Length; n++) bind[n] = Matrix4x4.identity;
            Renderer = new SkinnedMeshRenderer
            {
                Mesh = new Mesh
                {
                    SourceVertices = new[] { new Vector3(-.24f, -.04f, 0), new Vector3(-.16f, -.04f, 0),
                        new Vector3(-.20f, -.04f, .10f), new Vector3(.16f, -.04f, 0),
                        new Vector3(.24f, -.04f, 0), new Vector3(.20f, -.04f, .10f) },
                    SourceTriangles = new[] { 0, 1, 2, 3, 4, 5 }, SourceWeights = weights, SourceBindposes = bind
                },
                bones = new Il2CppReferenceArray<Transform>(new[] { LeftFoot, LeftToe, RightFoot, RightToe, new Transform() })
            };
        }

        internal Transform WeightedLeg(bool opposite = false, bool common = false)
        {
            var root = new Transform(); var leftAnkle = new Transform { parent = root }; var rightAnkle = new Transform { parent = root };
            LeftFoot.parent = leftAnkle; RightFoot.parent = rightAnkle; Renderer.transform.parent = root;
            Transform weighted = common ? root : opposite ? rightAnkle : leftAnkle;
            Renderer.bones = new Il2CppReferenceArray<Transform>(new[] { LeftFoot, LeftToe, RightFoot, RightToe, weighted });
            // Keep the complete incident triangle: one ankle-only corner, one
            // smooth ankle/foot blend and one Foot corner. No synthetic sole.
            Renderer.Mesh.SourceWeights[0] = new BoneWeight { boneIndex0 = 4, weight0 = 1 };
            Renderer.Mesh.SourceWeights[1] = new BoneWeight { boneIndex0 = 0, weight0 = .75f, boneIndex1 = 4, weight1 = .25f };
            Renderer.BakeOffsets = new[] { .025f };
            return root;
        }

        internal void Gpu(bool wideIndices = false)
        {
            var mesh = Renderer.Mesh; mesh.isReadable = false;
            mesh.Attributes = new[] { new VertexAttributeDescriptor { attribute = VertexAttribute.Position,
                format = VertexAttributeFormat.Float32, dimension = 3, stream = 1 } };
            mesh.VertexBufferCount = mesh.SourceVertices.Length;
            mesh.VertexBytes = new byte[mesh.VertexBufferCount * 20];
            for (int i = 0; i < mesh.SourceVertices.Length; i++)
            {
                Vector3 p = mesh.SourceVertices[i]; int at = i * 20 + 4;
                BitConverter.GetBytes(p.x).CopyTo(mesh.VertexBytes, at);
                BitConverter.GetBytes(p.y).CopyTo(mesh.VertexBytes, at + 4);
                BitConverter.GetBytes(p.z).CopyTo(mesh.VertexBytes, at + 8);
            }
            mesh.indexFormat = wideIndices ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.IndexBufferStride = wideIndices ? 4 : 2; mesh.IndexBufferCount = 6;
            mesh.IndexBytes = new byte[6 * mesh.IndexBufferStride];
            for (int i = 0; i < 6; i++)
            {
                byte[] bytes = wideIndices ? BitConverter.GetBytes((uint)i) : BitConverter.GetBytes((ushort)i);
                bytes.CopyTo(mesh.IndexBytes, i * mesh.IndexBufferStride);
            }
            // Source triangles concatenate SUBMESH order after baseVertex, not raw-buffer order.
            mesh.Submeshes = new[] {
                new SubMeshDescriptor { topology = MeshTopology.Triangles, indexStart = 3, indexCount = 3, baseVertex = -3 },
                new SubMeshDescriptor { topology = MeshTopology.Triangles, indexStart = 0, indexCount = 3, baseVertex = 3 } };
        }
        internal void UnsupportedGpu()
        {
            Gpu(); var descriptor = Renderer.Mesh.Attributes[0]; descriptor.format = VertexAttributeFormat.Float16;
            Renderer.Mesh.Attributes[0] = descriptor;
        }

        internal void Observe(bool eligible = true)
        {
            SkateFootContacts.ObserveAfterApply(new[] { Renderer }, LeftFoot, LeftToe, RightFoot, RightToe,
                Vector3.up, Vector3.up, eligible);
        }
        internal void Tick(bool eligible = true) { Time.realtimeSinceStartup += 1.1f; Observe(eligible); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message + " | " + SkateFootContacts.Status);
    }

    private static void Unavailable(string message)
    {
        Require(!SkateFootContacts.Ready && SkateFootContacts.LeftFitter == null && SkateFootContacts.RightFitter == null, message);
    }

    private static void Scenario(string name, Action run)
    {
        SkateFootContacts.Reset();
        Time.realtimeSinceStartup = 10;
        SkateFootContacts.SetBoard(SkateBoardMesh.CreateDetailed(), 0);
        try { run(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.Error.WriteLine("FAIL " + name + ": " + error.Message); }
    }

    private static void Exhaust(Model model)
    {
        model.UnsupportedGpu();
        for (int n = 0; n < 4; n++) model.Tick();
        Unavailable("Unreadable mesh was accepted");
        Require(model.Renderer.SharedReads == 3 && SkateFootContacts.Status.Contains("budget exhausted"), "Acquisition was not bounded");
    }

    private static void FailureIsolation(bool firstIsThird)
    {
        var broken = new Model();
        SkateFootContacts.UseView(firstIsThird, 11);
        broken.UnsupportedGpu();
        broken.Tick();
        int reads = broken.Renderer.SharedReads;
        broken.Observe();
        Require(broken.Renderer.SharedReads == reads, "Acquisition ignored retry backoff");
        broken.Renderer.Mesh.isReadable = true;
        broken.Renderer.Mesh.skinWeightBufferLayout = (SkinWeights)99;
        for (int n = 0; n < 3; n++) broken.Tick();
        Require(broken.Renderer.SharedReads == 3, "Unchanged view exceeded its failure budget");
        var healthy = new Model();
        SkateFootContacts.UseView(!firstIsThird, 11);
        healthy.Tick(); healthy.Tick();
        Require(SkateFootContacts.Ready && healthy.Renderer.Bakes == 2, "One view's failures poisoned the other view");
        SkateFootContacts.UseView(firstIsThird, 11);
        broken.Tick();
        Unavailable("View toggle restored an exhausted epoch");
        Require(broken.Renderer.SharedReads == 3, "View toggle granted extra attempts");
    }

    private static void SkinReplacement()
    {
        var first = new Model();
        SkateFootContacts.UseView(false, 21); Exhaust(first);
        var third = new Model();
        SkateFootContacts.UseView(true, 21); third.Tick(); third.Tick();
        object left = SkateFootContacts.LeftFitter;
        SkateFootContacts.UseView(false, 22);
        first.Renderer.Mesh.isReadable = true;
        first.Tick(); first.Tick();
        Require(SkateFootContacts.Ready && !SkateFootContacts.Status.Contains("First refusal:"), "Replacement skin retained stale failure state");
        SkateFootContacts.UseView(true, 21); third.Tick();
        Require(SkateFootContacts.Ready && third.Renderer.Bakes == 2 && ReferenceEquals(left, SkateFootContacts.LeftFitter), "Replacement reset the other view's cached profile");
    }

    private static void SnapshotIsolation()
    {
        var first = new Model();
        first.Renderer.BakeOffsets = new[] { 0f, .001f, .002f, .002f };
        SkateFootContacts.UseView(false, 31);
        for (int n = 0; n < 5; n++) first.Tick();
        Require(SkateFootContacts.Ready && first.Renderer.Bakes == 4, "Bounded calibration did not reuse its stable profile");
        var third = new Model();
        SkateFootContacts.UseView(true, 31); third.Tick(); third.Tick();
        Require(SkateFootContacts.Ready && third.Renderer.Bakes == 2, "Snapshot cap leaked between views");
        SkateFootContacts.UseView(false, 31);
        first.Renderer.Mesh.SourceVertices[0].z += .003f;
        first.Tick();
        Unavailable("Changed geometry reused a capped old profile");
        Require(SkateFootContacts.Status.Contains("snapshot budget exhausted"), "Same skin silently received a fresh snapshot budget");
        SkateFootContacts.UseView(true, 31); third.Tick();
        Require(SkateFootContacts.Ready, "Exhausted snapshots invalidated the other view");
        SkateFootContacts.UseView(false, 32); first.Tick(); first.Tick();
        Require(SkateFootContacts.Ready, "Replacement skin did not get its own snapshots");
    }

    private static void RideReset()
    {
        var first = new Model(); var third = new Model();
        SkateFootContacts.UseView(false, 41); Exhaust(first);
        SkateFootContacts.UseView(true, 41); Exhaust(third);
        SkateFootContacts.Reset();
        Unavailable("Ride reset left contacts ready");
        first.Renderer.Mesh.isReadable = third.Renderer.Mesh.isReadable = true;
        SkateFootContacts.UseView(false, 41); first.Tick(); first.Tick();
        Require(SkateFootContacts.Ready, "Ride reset failed to restore first-view acquisition");
        SkateFootContacts.UseView(true, 41); third.Tick(); third.Tick();
        Require(SkateFootContacts.Ready, "Ride reset failed to restore third-view acquisition");
    }

    private static void InvalidGeometry(string name, Action<Model> change)
    {
        Scenario("strict refusal: " + name, () =>
        {
            var model = new Model(); change(model);
            SkateFootContacts.UseView(false, 51); model.Tick();
            Unavailable("Invalid geometry was accepted");
        });
    }

    private static void WeightedSources()
    {
        foreach (bool unreadable in new[] { false, true })
        Scenario("measured single-side ankle/foot blend " + (unreadable ? "GPU source" : "CPU source"), () =>
        {
            var model = new Model(); Transform root = model.WeightedLeg();
            if (unreadable) model.Gpu();
            SkateFootContacts.UseView(false, 101); model.Tick(); model.Tick();
            Require(SkateFootContacts.Ready && model.Renderer.Bakes == 2, "Valid weighted leg did not calibrate");
            var profile = SkateFootContacts.LeftFitter.Profile;
            Require(profile.VertexCount == 3 && profile.TriangleCount == 1, "Ankle corner was cut out of the drawn triangle");
            Require(Math.Abs(profile.Vertex(0).Z - .025) < .000001, "Contact profile did not use measured posed geometry");
            object fit = SkateFootContacts.LeftFitter;
            root.localPosition = new Vector3(1024, 0, 2048); model.Tick();
            Require(model.Renderer.Bakes == 2 && ReferenceEquals(fit, SkateFootContacts.LeftFitter), "World travel invalidated the local weighted cache");
            // Controlled world-basis getter intentionally stays fixed: this must
            // detect the additional relative skin dependency, not a Foot matrix.
            model.LeftFoot.localRotation = new Quaternion { y = (float)Math.Sin(.1), w = (float)Math.Cos(.1) };
            model.Tick();
            Require(SkateFootContacts.Ready && model.Renderer.Bakes == 3 && !ReferenceEquals(fit, SkateFootContacts.LeftFitter),
                "Changed weighted local chain reused a stale contact profile");
            Require(model.Renderer.Mesh.BufferAcquires == model.Renderer.Mesh.BufferDisposals, "Weighted source leaked GPU wrappers");
        });
        Scenario("opposite-leg ankle blend remains cross-foot refusal", () =>
        {
            var model = new Model(); model.WeightedLeg(true); SkateFootContacts.UseView(false, 102); model.Tick();
            Unavailable("Opposite leg was accepted as left sole geometry");
            Require(model.Renderer.Bakes == 0, "Cross-foot source reached a posed bake");
        });
        Scenario("shared pelvis cannot own a foot patch", () =>
        {
            var model = new Model(); model.WeightedLeg(false, true); SkateFootContacts.UseView(false, 103); model.Tick();
            Unavailable("Shared body influence was accepted as foot ownership");
        });
        Scenario("unproved leg hierarchy cannot grant ancestor ownership", () =>
        {
            var model = new Model(); model.WeightedLeg(); model.RightFoot.parent = null;
            SkateFootContacts.UseView(false, 105); model.Tick(); Unavailable("Disconnected hierarchy claimed an entire model root");
        });
        Scenario("weighted local scale guard remains strict", () =>
        {
            var model = new Model(); model.WeightedLeg();
            var helper = new Transform { parent = model.LeftFoot.parent, localScale = new Vector3(2, 1, 1) };
            model.Renderer.bones = new Il2CppReferenceArray<Transform>(new[] { model.LeftFoot, model.LeftToe, model.RightFoot, model.RightToe, helper });
            SkateFootContacts.UseView(false, 104); model.Tick(); Unavailable("Nonuniform weighted geometry was accepted");
        });
    }

    private static void GeometryConfirmation()
    {
        foreach (bool unreadable in new[] { false, true })
        Scenario("stable measured geometry across weighted pose signatures " + (unreadable ? "GPU source" : "CPU source"), () =>
        {
            var model = new Model(); model.WeightedLeg(); if (unreadable) model.Gpu();
            SkateFootContacts.UseView(false, 111); model.Tick();
            model.LeftFoot.localRotation = new Quaternion { y = (float)Math.Sin(.1), w = (float)Math.Cos(.1) };
            model.Tick();
            Require(SkateFootContacts.Ready && model.Renderer.Bakes == 2 && !SkateFootContacts.Status.Contains("pending"),
                "Identical measured geometry could not confirm after a pose-signature change");
            object fit = SkateFootContacts.LeftFitter; model.Tick();
            Require(model.Renderer.Bakes == 2 && ReferenceEquals(fit, SkateFootContacts.LeftFitter), "Confirmed current pose was not cached");
            for (int n = 2; n <= 4; n++)
            {
                model.LeftFoot.localRotation = new Quaternion { y = (float)Math.Sin(n * .1), w = (float)Math.Cos(n * .1) };
                model.Tick();
            }
            Unavailable("Pose changes exceeded the fixed calibration snapshot cap");
            Require(model.Renderer.Bakes == 4 && SkateFootContacts.Status.Contains("snapshot budget exhausted"), "Pose changes received extra snapshots");
        });
        Scenario("changed source cannot reuse an old geometry confirmation", () =>
        {
            var model = new Model(); model.WeightedLeg(); SkateFootContacts.UseView(false, 112); model.Tick();
            model.Renderer.Mesh.SourceWeights[1] = new BoneWeight { boneIndex0 = 0, weight0 = .5f, boneIndex1 = 4, weight1 = .5f };
            model.Tick();
            Require(SkateFootContacts.Ready && SkateFootContacts.Status.Contains("pending"), "A new source key inherited the old confirmation");
            model.Tick();
            Require(model.Renderer.Bakes == 3 && !SkateFootContacts.Status.Contains("pending"), "New source failed its own measured confirmation");
        });
        Scenario("changing measured geometry still exhausts four snapshots", () =>
        {
            var model = new Model(); model.WeightedLeg(); model.Renderer.BakeOffsets = new[] { 0f, .001f, .002f, .003f };
            SkateFootContacts.UseView(false, 113);
            for (int n = 0; n < 4; n++)
            {
                model.LeftFoot.localRotation = new Quaternion { y = (float)Math.Sin(n * .1), w = (float)Math.Cos(n * .1) };
                model.Tick();
            }
            Unavailable("Genuinely moving weighted geometry was marked stable");
            Require(model.Renderer.Bakes == 4 && SkateFootContacts.Status.Contains("did not stabilize"), "Geometry stability or snapshot bound weakened");
            model.Tick(); Require(model.Renderer.Bakes == 4, "Unstable geometry got a fifth snapshot");
        });
    }

    private static void GpuRefusal(string name, Action<Mesh> change, bool wideIndices = false)
    {
        Scenario("unreadable source refusal: " + name, () =>
        {
            var model = new Model(); model.Gpu(wideIndices); change(model.Renderer.Mesh);
            SkateFootContacts.UseView(false, 91); model.Tick();
            Unavailable("Unsupported source readback was accepted");
            Require(model.Renderer.Bakes == 0, "Rejected source reached the posed bake");
            Require(model.Renderer.Mesh.BufferAcquires == model.Renderer.Mesh.BufferDisposals, "Borrowed GPU wrapper leaked");
        });
    }
    private static void GpuSources()
    {
        foreach (bool wide in new[] { false, true })
        Scenario("unreadable Float32 rest stream and " + (wide ? "UInt32" : "UInt16") + " submesh order", () =>
        {
            var model = new Model(); model.Gpu(wide); SkateFootContacts.UseView(false, 90);
            model.Tick(); model.Tick();
            Require(SkateFootContacts.Ready && model.Renderer.Bakes == 2, "Supported unreadable source did not calibrate");
            var mesh = model.Renderer.Mesh; object old = SkateFootContacts.LeftFitter;
            model.Tick();
            Require(model.Renderer.Bakes == 2 && ReferenceEquals(old, SkateFootContacts.LeftFitter), "Stable unreadable source was rebaked");
            Require(mesh.VertexReads == 3 && mesh.IndexReads == 3 && mesh.BufferDisposals == 6, "Source buffers were not read and disposed each check");
            BitConverter.GetBytes(mesh.SourceVertices[0].x + .001f).CopyTo(mesh.VertexBytes, 4);
            model.Tick();
            Require(SkateFootContacts.Ready && model.Renderer.Bakes == 3 && !ReferenceEquals(old, SkateFootContacts.LeftFitter),
                "Rest-byte change reused a key built from posed or stale vertices");
            Require(mesh.BufferAcquires == mesh.BufferDisposals, "Successful source readback leaked wrappers");
        });
        Scenario("unreadable aggregate byte budget precedes oversized readback", () =>
        {
            var model = new Model(); var renderers = new SkinnedMeshRenderer[6];
            for (int i = 0; i < renderers.Length; i++)
            {
                var part = i == 0 ? model : new Model(); part.Gpu(true);
                var mesh = part.Renderer.Mesh; var bytes = new byte[1572864 * 4];
                mesh.IndexBytes.CopyTo(bytes, 0); mesh.IndexBytes = bytes; mesh.IndexBufferCount = 1572864;
                part.Renderer.bones = model.Renderer.bones; renderers[i] = part.Renderer;
            }
            SkateFootContacts.UseView(false, 92);
            SkateFootContacts.ObserveAfterApply(renderers, model.LeftFoot, model.LeftToe, model.RightFoot, model.RightToe,
                Vector3.up, Vector3.up, true);
            Unavailable("Aggregate GPU readback budget was ignored");
            Require(SkateFootContacts.Status.Contains("aggregate readback"), "Wrong aggregate refusal");
            Require(renderers[5].Mesh.IndexReads == 0, "Over-budget readback was executed");
            foreach (var renderer in renderers)
                Require(renderer.Bakes == 0 && renderer.Mesh.BufferAcquires == renderer.Mesh.BufferDisposals, "Rejected aggregate leaked a buffer or baked");
        });
        GpuRefusal("missing position", m => m.Attributes = new VertexAttributeDescriptor[0]);
        GpuRefusal("duplicate position", m => m.Attributes = new[] { m.Attributes[0], m.Attributes[0] });
        GpuRefusal("compressed position", m => m.Attributes[0].format = VertexAttributeFormat.Float16);
        GpuRefusal("position dimension", m => m.Attributes[0].dimension = 2);
        GpuRefusal("position stream", m => m.Attributes[0].stream = 4);
        GpuRefusal("position stream mismatch", m => m.PositionStream = 2);
        GpuRefusal("negative offset", m => m.PositionOffset = -4);
        GpuRefusal("offset beyond record", m => m.PositionOffset = 12);
        GpuRefusal("oversized stride", m => m.PositionStride = 512);
        GpuRefusal("vertex stride mismatch", m => m.VertexBufferStride = 24);
        GpuRefusal("vertex count mismatch", m => m.VertexBufferCount++);
        GpuRefusal("truncated vertices", m => m.VertexBytes = new byte[12]);
        GpuRefusal("unsupported index format", m => m.indexFormat = (IndexFormat)99);
        GpuRefusal("too many submeshes", m => m.Submeshes = new SubMeshDescriptor[257]);
        GpuRefusal("line topology", m => m.Submeshes[0].topology = MeshTopology.Lines);
        GpuRefusal("negative submesh start", m => m.Submeshes[0].indexStart = -1);
        GpuRefusal("partial triangle", m => m.Submeshes[0].indexCount = 4);
        GpuRefusal("index range overflow", m => m.Submeshes[0].indexStart = int.MaxValue);
        GpuRefusal("aggregate index bound", m => m.Submeshes[0].indexCount = 1572867);
        GpuRefusal("base vertex outside source", m => m.Submeshes[0].baseVertex = int.MaxValue);
        GpuRefusal("UInt32 unsigned overflow", m => BitConverter.GetBytes(uint.MaxValue).CopyTo(m.IndexBytes, 12), true);
        GpuRefusal("truncated indices", m => m.IndexBytes = new byte[2]);
        GpuRefusal("index stride mismatch", m => m.IndexBufferStride = 4);
        GpuRefusal("index buffer bound", m => m.IndexBufferCount = int.MaxValue);
        GpuRefusal("position read exception", m => m.VertexReadFailure = new NotSupportedException("controlled GPU read"));
        GpuRefusal("index read exception", m => m.IndexReadFailure = new NotSupportedException("controlled GPU read"));
        GpuRefusal("nonfinite rest vertex", m => BitConverter.GetBytes(float.NaN).CopyTo(m.VertexBytes, 4));
        GpuRefusal("source weights unavailable", m => m.WeightReadFailure = new NotSupportedException("controlled weights"));
        GpuRefusal("source bind poses unavailable", m => m.BindReadFailure = new NotSupportedException("controlled bind poses"));
    }

    public static int Main()
    {
        Console.WriteLine("Production SkateFootContacts + shared sources; controlled Unity/interop boundary. No native avatar or gameplay test.");
        Scenario("valid measured pair and cached reuse", () =>
        {
            var model = new Model(); SkateFootContacts.UseView(false, 1);
            model.Tick(); model.Tick();
            Require(SkateFootContacts.Ready && SkateFootContacts.LeftFitter != null && SkateFootContacts.RightFitter != null, "Valid pair did not calibrate");
            object left = SkateFootContacts.LeftFitter; object right = SkateFootContacts.RightFitter;
            model.Tick();
            Require(model.Renderer.Bakes == 2 && ReferenceEquals(left, SkateFootContacts.LeftFitter) && ReferenceEquals(right, SkateFootContacts.RightFitter), "Unchanged geometry was rebuilt");
        });
        Scenario("first-person failure does not poison third-person", () => FailureIsolation(false));
        Scenario("third-person failure does not poison first-person", () => FailureIsolation(true));
        Scenario("terminal failure retains the first diagnostic", () =>
        {
            var model = new Model(); SkateFootContacts.UseView(false, 12);
            model.UnsupportedGpu(); model.Tick();
            model.Renderer.Mesh.isReadable = true;
            model.Renderer.Mesh.skinWeightBufferLayout = (SkinWeights)99;
            for (int n = 0; n < 3; n++) model.Tick();
            Unavailable("Failed mesh became ready");
            Require(SkateFootContacts.Status.Contains("First refusal:") && SkateFootContacts.Status.Contains("unreadable"), "Terminal message lost the original failure");
        });
        Scenario("replacement skin resets only its own acquisition epoch", SkinReplacement);
        Scenario("snapshot bounds, stale geometry and view isolation", SnapshotIsolation);
        Scenario("new ride resets both bounded views", RideReset);
        Scenario("ineligible pose cannot start calibration", () =>
        {
            var model = new Model(); SkateFootContacts.UseView(false, 61); model.Tick(false);
            Unavailable("Ineligible pose calibrated");
            Require(model.Renderer.Bakes == 0, "Ineligible pose baked a mesh");
            model.Tick(); model.Tick();
            Require(SkateFootContacts.Ready, "Later eligible pose did not calibrate");
        });
        Scenario("native read exception invalidates both fitters", () =>
        {
            var model = new Model(); SkateFootContacts.UseView(false, 71); model.Tick(); model.Tick();
            model.Renderer.ReadFailure = new NotSupportedException("controlled engine boundary failure");
            model.Tick();
            Unavailable("Read failure retained stale fitters");
            Require(SkateFootContacts.Status.Contains("NotSupportedException"), "Native exception type was lost");
        });
        Scenario("invalid board invalidates contacts and valid replacement recovers", () =>
        {
            var model = new Model(); SkateFootContacts.UseView(false, 81); model.Tick(); model.Tick();
            SkateFootContacts.SetBoard(null, 0); Unavailable("Missing board retained fitters");
            model.Tick(); Unavailable("Missing board allowed acquisition");
            SkateFootContacts.SetBoard(SkateBoardMesh.CreateDetailed(), 0); model.Tick(); model.Tick();
            Require(SkateFootContacts.Ready, "Valid replacement board did not recover");
        });
        InvalidGeometry("unsupported unreadable", model => model.UnsupportedGpu());
        InvalidGeometry("oversized", model => model.Renderer.Mesh.DeclaredCount = 262145);
        InvalidGeometry("nonfinite vertex", model => model.Renderer.Mesh.SourceVertices[0].x = float.NaN);
        InvalidGeometry("unowned bone blend", model => model.Renderer.Mesh.SourceWeights[0] = new BoneWeight { boneIndex0 = 4, weight0 = .1f, boneIndex1 = 0, weight1 = .9f });
        InvalidGeometry("cross-foot influence", model => model.Renderer.Mesh.SourceWeights[0] = new BoneWeight { boneIndex0 = 2, weight0 = 1 });
        InvalidGeometry("foot morph", model => model.Renderer.Mesh.blendShapeCount = 1);
        InvalidGeometry("unnormalized weights", model => model.Renderer.Mesh.SourceWeights[0] = new BoneWeight { boneIndex0 = 0, weight0 = .5f });
        InvalidGeometry("invalid index", model => model.Renderer.Mesh.SourceTriangles[0] = 6);
        InvalidGeometry("nonuniform scale", model => model.Renderer.transform.localScale = new Vector3(2, 1, 1));
        GpuSources(); WeightedSources(); GeometryConfirmation();
        Console.WriteLine("skate_capture_foot_acquisition: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
