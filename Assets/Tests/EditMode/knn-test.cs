using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class FindNearestTests
{
    [Test]
    public void FindNearest_ReturnsClosestKPoints()
    {
        // Arrange
        var go     = new GameObject("InterpTest");
        var interp = go.AddComponent<Interpolation>();

        // Build a dummy dataset with points at x = 0,1,2,3,4,5 (all y,z = 0)
        var dummy = new Dictionary<Vector3, float[][]>();
        for (int i = 0; i < 6; i++)
        {
            dummy[new Vector3(i, 0, 0)] = new[] { new float[]{ 0f, 0f, 0f } };
        }

        // Inject it into the private field 'dataset'
        var datasetField = typeof(Interpolation)
            .GetField("dataset", BindingFlags.NonPublic | BindingFlags.Instance);
        datasetField.SetValue(interp, dummy);

        // Pick a target at the origin, request the 3 nearest
        Vector3 target = Vector3.zero;
        int k = 3;

        // Act: invoke the private FindNearest method
        var method = typeof(Interpolation)
            .GetMethod("FindNearest", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (List<KeyValuePair<Vector3, float[][]>>)
            method.Invoke(interp, new object[]{ target, k });

        // Assert: we should get exactly 3 entries, at x = 0, 1, and 2
        Assert.AreEqual(k, result.Count, "Returned list length");

        var expected = new[] {
            new Vector3(0,0,0),
            new Vector3(1,0,0),
            new Vector3(2,0,0)
        };

        for (int i = 0; i < k; i++)
        {
            Assert.AreEqual(expected[i], result[i].Key,
                $"Point #{i} should be {expected[i]} but was {result[i].Key}");
        }

        // Cleanup
        Object.DestroyImmediate(go);
    }
}