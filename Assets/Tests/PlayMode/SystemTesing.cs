using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class InterpolationSystemTests
{
    Interpolation interp;
    Dictionary<Vector3, float[][]> ds;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        var go = new GameObject("SysTestRoot");
        interp = go.AddComponent<Interpolation>();
        interp.enabled = false; // disable Update()

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
    public IEnumerator RBFInterpolateRotation_AgreementWithSlerp_MeetsErrorThreshold()
    {
        var target = new Vector3(0.5f, 0.5f, 0.5f);
        // find nearest
        var findNearest = typeof(Interpolation)
            .GetMethod("FindNearest", BindingFlags.NonPublic|BindingFlags.Instance);
        var nearest = (List<KeyValuePair<Vector3, float[][]>>)
            findNearest.Invoke(interp, new object[]{ target, 5 });


        var rbf = typeof(Interpolation)
            .GetMethod("RBFInterpolateRotation", BindingFlags.Public|BindingFlags.Instance);
        var actualQ = (Quaternion)rbf.Invoke(
            interp,
            new object[]{ target, nearest, 0 }
        );

        var q0 = Quaternion.Euler(0f, 0f, 0f);
        var q1 = Quaternion.Euler(90f, 0f, 0f);
        var expectedQ = Quaternion.Slerp(q0, q1, 0.5f);


        float angleDiff = Quaternion.Angle(expectedQ, actualQ);
        Debug.Log($"[SystemTest] Quaternion angle diff = {angleDiff:F3}°");


        float posError = angleDiff * Mathf.Deg2Rad;
        Debug.Log($"[SystemTest] Implied position error ≈ {posError:F3} units");

        Assert.LessOrEqual(posError, 0.1f, 
            $"Position error {posError:F3} exceeds 0.1 units");

        yield return null;
    }
}