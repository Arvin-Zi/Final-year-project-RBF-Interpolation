using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class JointData
{
    public string jointName;        // Name of the joint
    public Quaternion rotation;    // Rotation of the joint
}

[System.Serializable]
public class PoseData
{
    public float targetX;
    public float targetY;
    public float targetZ;
    public Dictionary<string, Vector3> linkRotations = new Dictionary<string, Vector3>();
}

[System.Serializable]
public class TargetData
{
    public float targetXPosition;
    public float targetYPosition;
    public float targetZPosition;
    public List<JointData> joints = new List<JointData>();
}
