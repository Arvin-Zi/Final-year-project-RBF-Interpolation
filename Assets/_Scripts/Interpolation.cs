using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


public class Interpolation : MonoBehaviour
{
    public GameObject fingerTipObject;  // Assign a Unity object in the inspector
    public GameObject sphere;
    public ArticulationBody rootArticulation; // Root articulation body reference
    public string datasetFolderPath; // Folder path where 36 data files are stored
    private Vector3 targetPosition; // Target position set in the Inspector
    private List<ArticulationBody> articulationBodies = new List<ArticulationBody>();
    private Dictionary<Vector3, float[][]> dataset;

    void Start()
    {

        // Parse dataset on startup
        dataset = ParseFiles(datasetFolderPath);
        articulationBodies = GetAllArticulationBodies(rootArticulation.transform);
    }

    void Update()
    {
        UpdateTargetPositionFromLeapMotion();
        // Call ApplyInterpolatedRotations using the target position set in the Inspector
        ApplyInterpolatedRotations(targetPosition);
    }
    // Assuming you have a GameObject (e.g., a sphere) to represent the index finger


    void UpdateTargetPositionFromLeapMotion()
    {

        // targetPosition = sphere.transform.position;

        targetPosition = fingerTipObject.transform.position ;

    }

    private List<ArticulationBody> GetAllArticulationBodies(Transform root)
    {
        List<ArticulationBody> bodies = new List<ArticulationBody>();

        foreach (Transform child in root)
        {
            ArticulationBody articulation = child.GetComponent<ArticulationBody>();

            if (articulation != null && articulation.name != "FingerA" && articulation.name != "FingerB")
            {
                bodies.Add(articulation);
            }

            bodies.AddRange(GetAllArticulationBodies(child));
        }

        return bodies;
    }

    // Parse all text files in the dataset folder and extract positions and rotations
    private Dictionary<Vector3, float[][]> ParseFiles(string folderPath)
    {
        Dictionary<Vector3, float[][]> data = new Dictionary<Vector3, float[][]>();

        foreach (string file in Directory.GetFiles(folderPath, "*.txt"))
        {
            string[] lines = File.ReadAllLines(file);
            Vector3 targetPosition = Vector3.zero;
            List<float[]> rotations = new List<float[]>();

            foreach (string line in lines)
            {
                if (line.StartsWith("Target X Position"))
                {
                    targetPosition.x = float.Parse(line.Split(':')[1].Trim());
                }
                else if (line.StartsWith("Target Y Position"))
                {
                    targetPosition.y = float.Parse(line.Split(':')[1].Trim());
                }
                else if (line.StartsWith("Target Z Position"))
                {
                    targetPosition.z = float.Parse(line.Split(':')[1].Trim());
                }
                else if (line.Contains("Rotation"))
                {
                    string[] parts = line.Split(new[] { '(', ',', ')' }, StringSplitOptions.RemoveEmptyEntries);
                    float[] rotation = new float[]
                    {
                        float.Parse(parts[1].Trim()),  // X rotation
                        float.Parse(parts[2].Trim()),  // Y rotation
                        float.Parse(parts[3].Trim())   // Z rotation
                    };
                    rotations.Add(rotation);
                }
            }

            data[targetPosition] = rotations.ToArray();
        }

        return data;
    }



    private List<KeyValuePair<Vector3, float[][]>> FindNearest(Vector3 target, int k = 5)
    {
        // Log the target position to check it's being passed correctly
        Debug.Log($"Finding nearest points to target: {target}");

        var nearestPoints = dataset.OrderBy(entry => Vector3.Distance(entry.Key, target)).Take(k).ToList();

        // Log the nearest points and their distances to verify correctness
        Debug.Log($"Nearest {k} points:");
        foreach (var entry in nearestPoints)
        {
            float distance = Vector3.Distance(entry.Key, target);
            Debug.Log($"Point: {entry.Key}, Distance: {distance}");
        }

        return nearestPoints;
    }



    public Quaternion WeightedQuaternionAverage(Quaternion[] quaternions, double[] weights)
{
    // Assume quaternions.Length == weights.Length
    // Start with a reference from the first quaternion
    Quaternion reference = quaternions[0];
    
    // Accumulator in 4D
    double accumX = 0.0, accumY = 0.0, accumZ = 0.0, accumW = 0.0;
    
    for (int i = 0; i < quaternions.Length; i++)
    {
        // Flip sign if dot product with the reference is negative
        double dot = Quaternion.Dot(reference, quaternions[i]);
        if (dot < 0.0)
        {
            // Flip this quaternion
            quaternions[i] = new Quaternion(
                -quaternions[i].x,
                -quaternions[i].y,
                -quaternions[i].z,
                -quaternions[i].w
            );
        }
        
        // Accumulate weighted components
        accumX += quaternions[i].x * weights[i];
        accumY += quaternions[i].y * weights[i];
        accumZ += quaternions[i].z * weights[i];
        accumW += quaternions[i].w * weights[i];
    }
    
    // Create the weighted quaternion
    Quaternion result = new Quaternion(
        (float)accumX,
        (float)accumY,
        (float)accumZ,
        (float)accumW
    );
    
    // Normalize to get a valid unit quaternion
    result = NormalizeQuaternion(result);
    return result;
}

// Simple normalization utility
private Quaternion NormalizeQuaternion(Quaternion q)
{
    float mag = Mathf.Sqrt(q.x*q.x + q.y*q.y + q.z*q.z + q.w*q.w);
    if (mag > 1e-10f)
    {
        float invMag = 1f / mag;
        return new Quaternion(q.x * invMag, q.y * invMag, q.z * invMag, q.w * invMag);
    }
    // Fallback if something degenerate happened
    return Quaternion.identity;
}



public Quaternion RBFInterpolateRotation(
    Vector3 target,
    List<KeyValuePair<Vector3, float[][]>> nearestPoints,
    int jointIndex)
{
    // distances & angle arrays -> you already have these
    double[] distances = nearestPoints.Select(p => (double)Vector3.Distance(p.Key, target)).ToArray();
    Quaternion[] quaternions = nearestPoints.Select(p => Quaternion.Euler(
        p.Value[jointIndex][0],
        p.Value[jointIndex][1],
        p.Value[jointIndex][2])
    ).ToArray();

    double epsilon = 1e-6;

    // IDW weights
    double[] weights = new double[distances.Length];
    double sumOfWeights = 0.0;
    for (int i = 0; i < distances.Length; i++)
    {
        weights[i] = 1.0 / (distances[i] + epsilon);
        sumOfWeights += weights[i];
    }
    // Normalize
    for (int i = 0; i < weights.Length; i++)
    {
        weights[i] /= sumOfWeights;
    }

    // Now average the quaternions with these weights:
    Quaternion interpolated = WeightedQuaternionAverage(quaternions, weights);
    return interpolated;
}

    // Apply the interpolated rotations to the robot
    public void ApplyInterpolatedRotations(Vector3 targetPosition)
    {
        // Find the nearest data points to the target position
        List<KeyValuePair<Vector3, float[][]>> nearestPoints = FindNearest(targetPosition);

        for (int i = 0; i < articulationBodies.Count; i++)
        {
            // Interpolate the rotation for the current joint (assumes quaternion rotations)
            Quaternion targetRotation = RBFInterpolateRotation(targetPosition, nearestPoints, i);


            // Apply the interpolated rotation to the ArticulationBody
            articulationBodies[i].transform.localRotation = targetRotation;

            //Debug.Log("Applying interpolated rotation: " + targetRotation.eulerAngles); // Optional log to view the applied rotation
        }
    }
}


    // Find the 3 nearest points to the target position on each of the four lines
    // private List<KeyValuePair<Vector3, float[][]>> FindNearest(Vector3 target, int k = 10)
    // {
    //     // Log the target position to check it's being passed correctly
    //     Debug.Log($"Finding nearest points to target: {target}");

    //     // Group points based on the y-z lines (4 lines: (y=0, z=0), (y=0, z=1.25), (y=2, z=0), (y=2, z=1.25))
    //     var lines = new List<List<KeyValuePair<Vector3, float[][]>>>
    //     {
    //         dataset.Where(entry => entry.Key.y == 0.86f && entry.Key.z == 0f).ToList(), // Line 1: y = 0, z = 0
    //         dataset.Where(entry => entry.Key.y == 0.86f && entry.Key.z == 1.25f).ToList(), // Line 2: y = 0, z = 1.25
    //         dataset.Where(entry => entry.Key.y == 2f && entry.Key.z == 0f).ToList(), // Line 3: y = 2, z = 0
    //         dataset.Where(entry => entry.Key.y == 2f && entry.Key.z == 1.25f).ToList() // Line 4: y = 2, z = 1.25
    //     };

    //     List<KeyValuePair<Vector3, float[][]>> nearestPoints = new List<KeyValuePair<Vector3, float[][]>>();

    //     // For each line, find the k closest points
    //     foreach (var line in lines)
    //     {
    //         var sortedLine = line.OrderBy(entry => Vector3.Distance(entry.Key, target)).Take(k).ToList();
    //         nearestPoints.AddRange(sortedLine);  // Add the closest points to the final list
    //     }

    //     // Log the nearest points for verification
    //     Debug.Log($"Nearest {k * 4} points:");
    //     foreach (var entry in nearestPoints)
    //     {
    //         float distance = Vector3.Distance(entry.Key, target);
    //         Debug.Log($"Point: {entry.Key}, Distance: {distance}");
    //     }

    //     return nearestPoints;
    // }
    




