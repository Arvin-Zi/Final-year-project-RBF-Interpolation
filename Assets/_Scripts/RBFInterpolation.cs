using MathNet.Numerics.LinearAlgebra;
using UnityEngine;
using System;

public static class RBFInterpolation
{
    /// <summary>
    /// Gaussian RBF function: φ(r) = exp(-epsilon * r^2).
    /// </summary>
    public static double Gaussian(double r, double epsilon)
    {
        return Math.Exp(-epsilon * r * r);
    }

    /// <summary>
    /// Given n data points in 3D (positions[i]) and their associated quaternion values[i],
    /// solves for RBF coefficients, then interpolates the quaternion at a new target position.
    /// </summary>
    public static Quaternion Interpolate(
        Vector3   target,      // The position we want to interpolate at
        Vector3[] positions,   // The positions of the n known data points
        Quaternion[] values,   // The quaternions at those n known data points
        double    epsilon      // The Gaussian RBF "shape" parameter
    )
    {
        int n = positions.Length;
        if (n == 0)
            return Quaternion.identity;

        // ------------------------------------------------------------------
        // 1) Build the NxN RBF matrix K, with K_ij = Gaussian(||p_i - p_j||).
        // ------------------------------------------------------------------
        double[,] K = new double[n,n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                double r = Vector3.Distance(positions[i], positions[j]);
                K[i, j] = Gaussian(r, epsilon);
            }
        }

        // Convert to MathNet matrix
        var Kmat = Matrix<double>.Build.DenseOfArray(K);

        // ------------------------------------------------------------------
        // 2) Decompose neighbor quaternions into separate arrays for x,y,z,w.
        // ------------------------------------------------------------------
        double[] bx = new double[n];
        double[] by = new double[n];
        double[] bz = new double[n];
        double[] bw = new double[n];

        // Optional sign flipping to ensure quaternions are oriented consistently:
        // Compare each neighbor's quaternion to the first neighbor's quaternion
        // and flip if dot < 0. This helps avoid ± issues. 
        Quaternion qRef = values[0];
        for (int i = 0; i < n; i++)
        {
            // Flip sign if needed
            if (Quaternion.Dot(qRef, values[i]) < 0f)
            {
                values[i] = new Quaternion(-values[i].x, -values[i].y, -values[i].z, -values[i].w);
            }

            bx[i] = values[i].x;
            by[i] = values[i].y;
            bz[i] = values[i].z;
            bw[i] = values[i].w;
        }

        // ------------------------------------------------------------------
        // 3) Solve the linear system K * alpha = q_component for each of x,y,z,w.
        // ------------------------------------------------------------------
        var alphaX = Kmat.Solve(Vector<double>.Build.Dense(bx));
        var alphaY = Kmat.Solve(Vector<double>.Build.Dense(by));
        var alphaZ = Kmat.Solve(Vector<double>.Build.Dense(bz));
        var alphaW = Kmat.Solve(Vector<double>.Build.Dense(bw));

        // ------------------------------------------------------------------
        // 4) Evaluate at the target: sum_i(alpha[i] * Gaussian(||x - p_i||)).
        // ------------------------------------------------------------------
        double sumX = 0, sumY = 0, sumZ = 0, sumW = 0;

        for (int i = 0; i < n; i++)
        {
            double r  = Vector3.Distance(target, positions[i]);
            double phi = Gaussian(r, epsilon);

            sumX += alphaX[i] * phi;
            sumY += alphaY[i] * phi;
            sumZ += alphaZ[i] * phi;
            sumW += alphaW[i] * phi;
        }

        // Combine into a quaternion and normalize
        Quaternion qInterp = new Quaternion(
            (float)sumX,
            (float)sumY,
            (float)sumZ,
            (float)sumW
        );

        // Normalizing ensures we stay on the unit quaternion manifold
        qInterp = NormalizeQuaternion(qInterp);

        return qInterp;
    }

    /// <summary>
    /// Utility to normalize a quaternion safely.
    /// </summary>
    private static Quaternion NormalizeQuaternion(Quaternion q)
    {
        float mag = Mathf.Sqrt(q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w);
        if (mag > 1e-9f)
        {
            return new Quaternion(q.x/mag, q.y/mag, q.z/mag, q.w/mag);
        }
        return Quaternion.identity;
    }
}