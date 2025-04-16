using System.Collections;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.TestTools;

public class RBF_inter
{
  [Test]
public void RBFInterpolateRotation_BasicCheck()
{
    // Arrange
    var interp = new GameObject().AddComponent<Interpolation>();

    // Create a simplified nearestPoints structure
    List<KeyValuePair<Vector3, float[][]>> nearestPoints = new List<KeyValuePair<Vector3, float[][]>>
    {
        new KeyValuePair<Vector3, float[][]>(
            new Vector3(0f,0f,0f),
            new float[][]
            {
                new float[]{ 0f, 0f, 0f } // rotation for joint 0
            }
        ),
        new KeyValuePair<Vector3, float[][]>(
            new Vector3(1f,1f,1f),
            new float[][]
            {
                new float[]{ 90f, 0f, 0f } // rotation for joint 0
            }
        )
    };

    Vector3 target = new Vector3(0.5f, 0.5f, 0.5f);

    // Act
    Quaternion result = interp.RBFInterpolateRotation(target, nearestPoints, 0);

    // Assert
    // Expect a rotation between 0° and 90° on the X axis. We just do a rough check.
    Assert.That(result.eulerAngles.x, Is.GreaterThan(0f));
    Assert.That(result.eulerAngles.x, Is.LessThan(90f));
}
}