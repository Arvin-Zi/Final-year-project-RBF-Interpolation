using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WeightedQuaternionAverage
{
    // A Test behaves as an ordinary method
    [Test]
    public void WeightedQuaternionAverageSimplePasses()
    {
        // Arrange
        var interp = new GameObject().AddComponent<Interpolation>(); // Instantiates your script
        Quaternion[] quaternions =
        {
            Quaternion.Euler(0, 0, 0),
            Quaternion.Euler(0, 90, 0)
        };
        double[] weights = { 0.5, 0.5 };

        // Act
        Quaternion result = interp.WeightedQuaternionAverage(quaternions, weights);

        // Assert
        // Check if quaternion is normalized and is roughly in between the two rotations
        float magnitude = Mathf.Sqrt(result.x * result.x + result.y * result.y + result.z * result.z + result.w * result.w);
        Assert.AreEqual(1f, magnitude, 1e-5f, "Resulting quaternion is not normalized.");

        // Optionally check that the result is about halfway between the two angles (approx 45° on Y)
        Assert.That(result.eulerAngles.y, Is.InRange(44.0f, 46.0f),
            $"Expected Y angle around 45°, got {result.eulerAngles.y}");
    }
}


