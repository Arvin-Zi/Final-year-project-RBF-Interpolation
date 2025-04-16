using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;


public class RobotPoseInterpolation : MonoBehaviour
{
    public string folderPath; // Path to the folder containing pose data

    // Factor for interpolation
    [UnityEngine.Range(0f, 1f)]  // Using Unity's Range attribute
    public float interpolationFactorX;

    [UnityEngine.Range(0f, 1f)]  // Using Unity's Range attribute
    public float interpolationFactorZ;

    [UnityEngine.Range(0f, 1f)]  // Using Unity's Range attribute
    public float interpolationFactorY;



    public ArticulationBody rootArticulation; // Root articulation body reference
    private List<ArticulationBody> articulationBodies = new List<ArticulationBody>();



    void Start()
    {
        if (rootArticulation == null)
        {
            Debug.LogError("The root articulation body is missing.");
            return;
        }

        // Gather all ArticulationBody components in the hierarchy
        articulationBodies = GetAllArticulationBodies(rootArticulation.transform);

        if (articulationBodies.Count == 0)
        {
            Debug.LogError("No ArticulationBody components found in the hierarchy.");
            return;
        }

        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            Debug.LogError("Data folder path is either empty or does not exist.");
            return;
        }
    }

    void Update()
    {
        InterpolateXYZ();

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

            // Recursively find articulation bodies in child objects
            bodies.AddRange(GetAllArticulationBodies(child));
        }

        return bodies;
    }

    private List<PoseData> GetAllPoses(Func<PoseData, bool> condition)
    {
        string[] files = Directory.GetFiles(folderPath, "*.txt");
        List<PoseData> poses = new List<PoseData>();

        foreach (string file in files)
        {
            PoseData pose = ParsePoseFile(file);

            if (pose != null && condition(pose))  // predicate condition
            {
                poses.Add(pose);
            }
        }

        return poses;
    }

    private PoseData ParsePoseFile(string filePath)
    {
        PoseData pose = new PoseData();
        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines)
        {
            if (line.StartsWith("Target X Position"))
                pose.targetX = float.Parse(line.Split(':')[1].Trim());
            else if (line.StartsWith("Target Y Position"))
                pose.targetY = float.Parse(line.Split(':')[1].Trim());
            else if (line.StartsWith("Target Z Position"))
                pose.targetZ = float.Parse(line.Split(':')[1].Trim());
            else if (line.Contains(": Rotation("))
            {
                string[] parts = line.Split(new[] { ": Rotation(", ", ", ")" }, StringSplitOptions.RemoveEmptyEntries);
                string jointName = parts[0];
                Vector3 rotation = new Vector3(
                    float.Parse(parts[1]),
                    float.Parse(parts[2]),
                    float.Parse(parts[3])
                );
                pose.linkRotations[jointName] = rotation;
            }
        }

        return pose;
    }

    private bool Condition0(PoseData pose)
    {
        return pose.targetY == 0 && pose.targetZ == 0;
    }

    private bool Condition1(PoseData pose)
    {
        return pose.targetY == 2 && pose.targetZ == 0;
    }

    private bool Condition2(PoseData pose)
    {
        return pose.targetZ == 1.25;
    }
    private bool Condition3(PoseData pose)
    {
        return pose.targetY == 2 && pose.targetZ == 1.25;
    }
    private bool Condition4(PoseData pose)
    {
        return pose.targetY == 2;
    }
    private bool Condition5(PoseData pose)
    {
        return pose.targetY == 0;
    }


    private List<Quaternion> InterpolateAlongX(List<PoseData> poses)
    {
        List<Quaternion> interpolatedPoses = new List<Quaternion>();

        if (poses == null || poses.Count < 2)
        {
            Debug.LogWarning("Not enough poses to interpolate.");
            return interpolatedPoses;
        }

        for (int i = 0; i < poses.Count - 1; i++)
        {
            PoseData startPose = poses[i];

            foreach (var joint in startPose.linkRotations)
            {
                string jointName = joint.Key;

                // Calculate the total length between consecutive poses
                float totalLength = poses.Count - 1;

                // Calculate the interpolation factor based on the slider (0 to 1)
                float distanceAlongLine = Mathf.Lerp(0, totalLength - 1, interpolationFactorX);  // interpolationFactorX ranges from 0 to 1
                int currentIndex = Mathf.FloorToInt(distanceAlongLine);  // Index of the current pose

                float t = distanceAlongLine - currentIndex;  // SLERP factor between 0 and 1 within the current segment

                if (currentIndex >= 0 && currentIndex < poses.Count - 1)
                {
                    Quaternion startPoseQuat = Quaternion.Euler(poses[currentIndex].linkRotations[jointName]);
                    Quaternion endPoseQuat = Quaternion.Euler(poses[currentIndex + 1].linkRotations[jointName]);

                    // Perform SLERP between the current and next poses
                    Quaternion interpolatedQuat = Quaternion.Slerp(startPoseQuat, endPoseQuat, t);

                    interpolatedPoses.Add(interpolatedQuat); // Store the interpolated quaternion
                }
            }
        }

        return interpolatedPoses;
    }
    private List<Quaternion> InterpolateOnPlane(int planeNo)
    {
        List<PoseData> line1Poses, line2Poses;
        List<Quaternion> interpolatedPoses = new List<Quaternion>();

        if (planeNo == 0) // Top Plane (lines 1 and 3)
        {
            line1Poses = SortPosesByX(GetAllPosesByLine(1));
            line2Poses = SortPosesByX(GetAllPosesByLine(3));
        }
        else if (planeNo == 1) // Bottom Plane (lines 0 and 2)
        {
            line1Poses = SortPosesByX(GetAllPosesByLine(0));
            line2Poses = SortPosesByX(GetAllPosesByLine(2));
        }
        else
        {
            Debug.LogWarning("Invalid plane number specified.");
            return interpolatedPoses;
        }

        if (line1Poses.Count < 2 || line2Poses.Count < 2)
        {
            Debug.LogWarning("Not enough poses on the specified plane for interpolation.");
            return interpolatedPoses;
        }

        List<Quaternion> interpolatedLine1 = InterpolateAlongX(line1Poses);
        List<Quaternion> interpolatedLine2 = InterpolateAlongX(line2Poses);

        for (int i = 0; i < line1Poses.Count; i++)
        {
            PoseData pose1 = line1Poses[i];
            PoseData pose2 = line2Poses[i];

            foreach (var link in pose1.linkRotations)
            {
                string jointName = link.Key;

                int indexLine1 = pose1.linkRotations.Keys.ToList().IndexOf(jointName);
                int indexLine2 = pose2.linkRotations.Keys.ToList().IndexOf(jointName);

                Quaternion interpolatedQuatLine1 = interpolatedLine1[indexLine1];
                Quaternion interpolatedQuatLine2 = interpolatedLine2[indexLine2];

                Quaternion interpolatedQuat = Quaternion.Slerp(
                    interpolatedQuatLine1,
                    interpolatedQuatLine2,
                    interpolationFactorZ
                );

                interpolatedPoses.Add(interpolatedQuat);
            }
        }
        return interpolatedPoses;
    }

    private void InterpolateXYZ()
    {
        List<PoseData> topPoses, bottomPoses;

        topPoses = SortPosesByX(GetAllPosesByLine(4));
        bottomPoses = SortPosesByX(GetAllPosesByLine(5));


        List<Quaternion> interpolatedPosesTop = InterpolateOnPlane(0);   // Top Plane
        List<Quaternion> interpolatedPosesBottom = InterpolateOnPlane(1); // Bottom Plane

        if (interpolatedPosesTop.Count < 2 || interpolatedPosesBottom.Count < 2)
        {
            Debug.LogWarning("Not enough poses to perform XYZ interpolation.");
            return;
        }

        for (int i = 0; i < topPoses.Count; i++)
        {
            PoseData pose1 = topPoses[i];
            PoseData pose2 = bottomPoses[i];

            foreach (var link in pose1.linkRotations)
            {
                string jointName = link.Key;

                int top = pose1.linkRotations.Keys.ToList().IndexOf(jointName);
                int bottom = pose2.linkRotations.Keys.ToList().IndexOf(jointName);

                Quaternion topQuat = interpolatedPosesTop[top];
                Quaternion bottomQuat = interpolatedPosesBottom[bottom];


                // Interpolating between the top and bottom planes along Y-axis
                Quaternion finalInterpolation = Quaternion.Slerp(
                    topQuat,
                    bottomQuat,
                    interpolationFactorY // This would be the interpolation factor for Y-axis interpolation
                );
                ArticulationBody articulationBody = articulationBodies.Find(a => a.name == jointName); // Assuming articulationBodies list
                if (articulationBody != null)
                {
                    articulationBody.transform.localRotation = Quaternion.Euler(finalInterpolation.eulerAngles.x, finalInterpolation.eulerAngles.y, finalInterpolation.eulerAngles.z);
                }
            }
        }
    }


    public List<PoseData> SortPosesByX(List<PoseData> poses)
    {
        if (poses == null || poses.Count == 0)
        {
            throw new ArgumentException("Pose list cannot be null or empty.");
        }

        return poses.OrderBy(p => p.targetX).ToList();
    }


    private List<PoseData> GetAllPosesByLine(int lineIndex)
    {
        switch (lineIndex)
        {
            case 0:
                return GetAllPoses(Condition0);
            case 1:
                return GetAllPoses(Condition1);
            case 2:
                return GetAllPoses(Condition2);
            case 3:
                return GetAllPoses(Condition3);
            case 4:
                return GetAllPoses(Condition4);
            case 5:
                return GetAllPoses(Condition5);
            default:
                return new List<PoseData>();  // Return an empty list if the line index is invalid
        }
    }
}
