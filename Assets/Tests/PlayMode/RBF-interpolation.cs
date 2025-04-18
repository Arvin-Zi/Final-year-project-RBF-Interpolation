using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class InterpolationIntegrationTests
{
    Interpolation interp;
    Dictionary<Vector3, float[][]> ds;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        var go = new GameObject("TestRoot");
        interp = go.AddComponent<Interpolation>();
        interp.enabled = false;             

        ds = new Dictionary<Vector3, float[][]>
        {
            { Vector3.zero, new[]{ new float[]{0f,0f,0f} } },
            { Vector3.one,   new[]{ new float[]{90f,0f,0f} } }
        };
        typeof(Interpolation)
            .GetField("dataset", BindingFlags.NonPublic|BindingFlags.Instance)
            .SetValue(interp, ds);

        yield return null;
    }

    [UnityTest]
    public IEnumerator RBFInterpolateRotation_HalfwayTarget_Returns45DegreesAboutX()
    {
        var target = new Vector3(0.5f, 0.5f, 0.5f);

        var findNearest = typeof(Interpolation)
            .GetMethod("FindNearest", BindingFlags.NonPublic|BindingFlags.Instance);
        var nearest = (List<KeyValuePair<Vector3, float[][]>>)
            findNearest.Invoke(interp, new object[]{ target, 5 });

        Assert.That(nearest.Count, Is.EqualTo(2), "Nearest-points count");

        var rbf = typeof(Interpolation)
            .GetMethod("RBFInterpolateRotation", BindingFlags.Public|BindingFlags.Instance);
        var resultQ = (Quaternion)rbf.Invoke(
            interp,
            new object[]{ target, nearest, 0 }
        );

        var euler = resultQ.eulerAngles;
        Debug.Log($"[Test] RBF result Euler = ({euler.x:F2},{euler.y:F2},{euler.z:F2})");

        Assert.That(euler.x, Is.InRange(44f, 46f),   $"X rotation was {euler.x:F2}");
        Assert.That(euler.y, Is.EqualTo(0f).Within(1e-3f), "Y should stay at 0");
        Assert.That(euler.z, Is.EqualTo(0f).Within(1e-3f), "Z should stay at 0");

        yield return null;
    }
}