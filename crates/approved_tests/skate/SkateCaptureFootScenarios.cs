using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Shortcut.RustMod;
using UnityEngine;

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
        model.Renderer.Mesh.isReadable = false;
        for (int n = 0; n < 4; n++) model.Tick();
        Unavailable("Unreadable mesh was accepted");
        Require(model.Renderer.SharedReads == 3 && SkateFootContacts.Status.Contains("budget exhausted"), "Acquisition was not bounded");
    }

    private static void FailureIsolation(bool firstIsThird)
    {
        var broken = new Model();
        SkateFootContacts.UseView(firstIsThird, 11);
        broken.Renderer.Mesh.isReadable = false;
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
            model.Renderer.Mesh.isReadable = false; model.Tick();
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
        InvalidGeometry("unreadable", model => model.Renderer.Mesh.isReadable = false);
        InvalidGeometry("oversized", model => model.Renderer.Mesh.DeclaredCount = 262145);
        InvalidGeometry("nonfinite vertex", model => model.Renderer.Mesh.SourceVertices[0].x = float.NaN);
        InvalidGeometry("shin blend", model => model.Renderer.Mesh.SourceWeights[0] = new BoneWeight { boneIndex0 = 4, weight0 = .1f, boneIndex1 = 0, weight1 = .9f });
        InvalidGeometry("cross-foot influence", model => model.Renderer.Mesh.SourceWeights[0] = new BoneWeight { boneIndex0 = 2, weight0 = 1 });
        InvalidGeometry("foot morph", model => model.Renderer.Mesh.blendShapeCount = 1);
        InvalidGeometry("unnormalized weights", model => model.Renderer.Mesh.SourceWeights[0] = new BoneWeight { boneIndex0 = 0, weight0 = .5f });
        InvalidGeometry("invalid index", model => model.Renderer.Mesh.SourceTriangles[0] = 6);
        InvalidGeometry("nonuniform scale", model => model.Renderer.transform.localScale = new Vector3(2, 1, 1));
        Console.WriteLine("skate_capture_foot_acquisition: " + passed + " passed, " + failed + " failed");
        return failed == 0 ? 0 : 1;
    }
}
