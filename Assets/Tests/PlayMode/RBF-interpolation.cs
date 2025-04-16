using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class InterpolationIntegrationTests
{
    private GameObject      testRoot;
    private Interpolation   interp;
    private ArticulationBody childBody;

    [UnitySetUp]
    public IEnumerator Setup()
    {
     
        testRoot = new GameObject("InterpTestRoot");
        interp   = testRoot.AddComponent<Interpolation>();


        var rootGO   = new GameObject("RootArt");
        var rootBody = rootGO.AddComponent<ArticulationBody>();
        rootGO.transform.parent = testRoot.transform;
        interp.rootArticulation = rootBody;

        var jointGO = new GameObject("Joint0");
        jointGO.transform.parent = rootGO.transform;
        childBody = jointGO.AddComponent<ArticulationBody>();


        var artField = typeof(Interpolation)
            .GetField("articulationBodies", BindingFlags.NonPublic | BindingFlags.Instance);
        artField.SetValue(interp, new List<ArticulationBody> { childBody });


        var dict = new Dictionary<Vector3, float[][]>
        {
            { new Vector3(0f,0f,0f), new[]{ new float[]{  0f, 0f, 0f } } },
            { new Vector3(1f,1f,1f), new[]{ new float[]{ 90f, 0f, 0f } } }
        };
        var dataField = typeof(Interpolation)
            .GetField("dataset", BindingFlags.NonPublic | BindingFlags.Instance);
        dataField.SetValue(interp, dict);


        var finger = new GameObject("Finger");
        interp.fingerTipObject = finger;

        yield return null; // wait a frame
    }

    [UnityTest]
    public IEnumerator ApplyInterpolatedRotations_UpdatesJointRotation()
    {
        // Arrange: target is halfway between our two dataset keys
        Vector3 target = new Vector3(0.5f, 0.5f, 0.5f);

        // Act: perform the interpolation+application
        interp.ApplyInterpolatedRotations(target);
        yield return null; // allow one frame for the transform to update

        // Assert: childBody.localRotation should be ≈45° around X, and 0° on Y/Z
        var e = childBody.transform.localRotation.eulerAngles;
        Assert.That(e.x, Is.InRange(44f, 46f), $"X angle was {e.x:F2}, expected ~45°");
        Assert.That(e.y, Is.EqualTo(0f).Within(1e-3f),  $"Y angle was {e.y:F2}, expected 0°");
        Assert.That(e.z, Is.EqualTo(0f).Within(1e-3f),  $"Z angle was {e.z:F2}, expected 0°");
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        Object.DestroyImmediate(testRoot);
        yield return null;
    }
}