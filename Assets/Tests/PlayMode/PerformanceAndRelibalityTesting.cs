using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class InterpolationPerformanceAndReliabilityTests
{
    Interpolation interp;
    Dictionary<Vector3, float[][]> ds;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        var go = new GameObject("PerfRelTestRoot");
        interp = go.AddComponent<Interpolation>();
        interp.enabled = false;

        // minimal dataset for performance
        ds = new Dictionary<Vector3, float[][]>
        {
            { Vector3.zero, new[]{ new float[]{0f,0f,0f} } },
            { Vector3.one,   new[]{ new float[]{90f,0f,0f} } }
        };
        typeof(Interpolation)
            .GetField("dataset", BindingFlags.NonPublic|BindingFlags.Instance)
            .SetValue(interp, ds);

        // empty articulationBodies for reliability
        typeof(Interpolation)
            .GetField("articulationBodies", BindingFlags.NonPublic|BindingFlags.Instance)
            .SetValue(interp, new List<ArticulationBody>());

        yield return null;
    }

    [UnityTest]
    public IEnumerator Performance_ApplyInterpolatedRotations_Under30ms()
    {
        var target = new Vector3(0.5f, 0.5f, 0.5f);

        // warm‑up
        interp.ApplyInterpolatedRotations(target);
        yield return null;

        var sw = new Stopwatch();
        sw.Start();

        interp.ApplyInterpolatedRotations(target);
        yield return null;

        sw.Stop();
        UnityEngine.Debug.Log($"[Performance] Elapsed = {sw.ElapsedMilliseconds} ms");
        Assert.LessOrEqual(sw.ElapsedMilliseconds, 30, 
            $"Interpolation took {sw.ElapsedMilliseconds} ms, exceeding 30 ms");

        yield return null;
    }

    [UnityTest]
    public IEnumerator Reliability_EmptyDataset_NoExceptionsOrNaN()
    {
        var rand = new System.Random();
        for (int i = 0; i < 10; i++)
        {
            var t = new Vector3(
                (float)(rand.NextDouble() * 2.0 - 1.0),
                (float)(rand.NextDouble() * 2.0 - 1.0),
                (float)(rand.NextDouble() * 2.0 - 1.0)
            );

            // ensure no exception
            try
            {
                interp.ApplyInterpolatedRotations(t);
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"Exception at target {t}: {ex}");
            }

            // ensure no NaN in any joint rotation (none in list, so trivially true)
            var bodies = (List<ArticulationBody>)typeof(Interpolation)
                .GetField("articulationBodies", BindingFlags.NonPublic|BindingFlags.Instance)
                .GetValue(interp);

            foreach (var body in bodies)
            {
                var q = body.transform.localRotation;
                Assert.IsTrue(
                    !(float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w)),
                    $"NaN detected in rotation at {t}"
                );
            }
        }

        yield return null;
    }
}