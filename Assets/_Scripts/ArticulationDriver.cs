/*
 
    This is the main script responsible for driving the articulation bodies of the target manipulator/hand/arm etc. 
    
    Author: Diar Abdlakrim
    Email: contact@obirobotics.com
    Date: 21st December 2019
    
    This software is propriatery and may not be used, copied, modified, or distributed 
    for any commercial purpose without explicit written permission Obi Robotics Ltd (R) 2024.
 */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class ArticulationDriver : MonoBehaviour
{
    private bool isRecordingComplete = false;

    // Physics body driver
    public ArticulationBody _palmBody;
    public Transform driverHand;

    public ArticulationBody[] articulationBods;
    public Vector3 driverHandOffset;
    public Vector3 rotataionalOffset;
    public MeshCollider[] _meshColliders;
    public MeshCollider[] _palmColliders;

    // Dictionary to store the initial (starting) local rotations for each articulation body
    private Dictionary<ArticulationBody, Quaternion> initialRotations = new Dictionary<ArticulationBody, Quaternion>();

    private List<TransformData> recordedTransforms = new List<TransformData>();

    private bool hasRecordedForCurrentTarget = false;

    ArticulationBody thisArticulation; // Root-Parent articulation body 
    float xTargetAngle, yTargetAngle = 0f;

    [Range(-90f, 90f)]
    public float angle = 0f;

    void Start()
    {
        thisArticulation = GetComponent<ArticulationBody>();

        // Save the initial local rotations for all articulation bodies.
        if (articulationBods != null)
        {
            foreach (ArticulationBody body in articulationBods)
            {
                if (body != null)
                {
                    initialRotations[body] = body.transform.localRotation;
                }
            }
        }
    }

    void FixedUpdate()
    {
        #region End-Effector Positioning
        // Counter Gravity; force = mass * acceleration
        _palmBody.AddForce(-Physics.gravity * _palmBody.mass);
        foreach (ArticulationBody body in articulationBods)
        {
            float velLimit = 1.75f;
            body.maxAngularVelocity = velLimit;
            body.maxDepenetrationVelocity = 3f;

            body.AddForce(-Physics.gravity * body.mass);
        }

        // Apply tracking position velocity; force = (velocity * mass) / deltaTime
        float massOfHand = _palmBody.mass;
        Vector3 palmDelta = ((driverHand.transform.position + driverHandOffset) +
          (driverHand.transform.rotation * Vector3.back * driverHandOffset.x) +
          (driverHand.transform.rotation * Vector3.up * driverHandOffset.y)) - _palmBody.worldCenterOfMass;

        float alpha = 0.05f; // Blend between existing velocity and all new velocity
        _palmBody.linearVelocity *= alpha;
        _palmBody.AddForce(Vector3.ClampMagnitude((((palmDelta / Time.fixedDeltaTime) / Time.fixedDeltaTime) * (_palmBody.mass + (1f * 5))) * (1f - alpha), 8000f * 1f));

        // Apply tracking rotation velocity 
        Quaternion palmRot = _palmBody.transform.rotation * Quaternion.Euler(rotataionalOffset);
        Quaternion rotation = driverHand.transform.rotation * Quaternion.Inverse(palmRot);
        Vector3 angularVelocity = Vector3.ClampMagnitude((new Vector3(
          Mathf.DeltaAngle(0, rotation.eulerAngles.x),
          Mathf.DeltaAngle(0, rotation.eulerAngles.y),
          Mathf.DeltaAngle(0, rotation.eulerAngles.z)) / Time.fixedDeltaTime) * Mathf.Deg2Rad, 45f * 1f);

        _palmBody.angularVelocity = angularVelocity;
        _palmBody.angularDamping = 50f;
        #endregion

        #region End-Effector Orienting
        Vector3 endEffectorXAxis = _palmBody.transform.right; // Local x-axis
        Vector3 driverHandZAxis = driverHand.forward;          // Local z-axis of driverHand

        Vector3 rotationAxis = _palmBody.transform.up; // Axis of rotation (local y-axis)

        Vector3 projectedEndEffectorX = Vector3.ProjectOnPlane(endEffectorXAxis, rotationAxis);
        Vector3 projectedDriverHandZ = Vector3.ProjectOnPlane(driverHandZAxis, rotationAxis);

        float angleToRotate = Vector3.SignedAngle(projectedEndEffectorX, projectedDriverHandZ, rotationAxis);

        if (articulationBods.Length > 0)
        {
            ArticulationBody revoluteJoint = articulationBods[articulationBods.Length - 1];
            ArticulationDrive drive = revoluteJoint.xDrive; // Assuming rotation around x-axis

            drive.target = angleToRotate;
            revoluteJoint.xDrive = drive;

            drive.lowerLimit = Mathf.Min(drive.lowerLimit, angleToRotate);
            drive.upperLimit = Mathf.Max(drive.upperLimit, angleToRotate);
            revoluteJoint.xDrive = drive;
        }
        #endregion

        #region Stabilize ArticulationBody / Prevent Random Jittering
        foreach (MeshCollider collider in _palmColliders)
        {
            collider.enabled = false;
        }
        foreach (MeshCollider collider in _meshColliders)
        {
            collider.enabled = false;
        }

        if (articulationBods != null && articulationBods.Length > 0)
        {
            for (int a = 0; a < articulationBods.Length; a++)
            {
                ArticulationBody body = articulationBods[a];
                int dofCount = body.jointVelocity.dofCount;

                if (dofCount == 3)
                {
                    body.jointVelocity = new ArticulationReducedSpace(0f, 0f, 0f);
                }
                else if (dofCount == 2)
                {
                    body.jointVelocity = new ArticulationReducedSpace(0f, 0f);
                }
                else if (dofCount == 1)
                {
                    body.jointVelocity = new ArticulationReducedSpace(0f);
                }

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        if (_palmColliders != null)
        {
            foreach (MeshCollider collider in _palmColliders)
            {
                if (collider != null)
                    collider.enabled = false;
            }
        }
        if (_meshColliders != null)
        {
            foreach (MeshCollider collider in _meshColliders)
            {
                if (collider != null)
                    collider.enabled = false;
            }
        }
        #endregion
    }

    public void CheckAndRecordTarget()
    {
        if (IsAtTargetPoint())
        {
            if (!hasRecordedForCurrentTarget)
            {
                RecordJointData();
                hasRecordedForCurrentTarget = true;
                isRecordingComplete = true; 
            }
        }
    }

    private bool IsAtTargetPoint()
    {
        Collider targetCollider = driverHand.GetComponent<Collider>();
        Collider palmCollider = _palmBody.GetComponent<Collider>();

        if (targetCollider != null && palmCollider != null)
        {
            Vector3 targetCenter = targetCollider.bounds.center;
            float proximityRadius = 1e-13f; // Adjust radius as needed
            bool isIntersecting = Physics.CheckSphere(targetCenter, proximityRadius, ~0, QueryTriggerInteraction.Ignore);
            Debug.Log($"Intersection detected: {isIntersecting}");
            return isIntersecting;
        }
        else
        {
            Debug.LogWarning("Colliders not found on driverHand or palmBody.");
            return false;
        }
    }

    private void RecordJointData()
    {
        recordedTransforms.Clear();

        foreach (ArticulationBody body in articulationBods)
        {
            TransformData data = new TransformData
            {
                rotation = body.transform.localRotation,
                jointName = body.name
            };
            Debug.Log($"Recording: {body.name}, Rotation: {data.rotation}");
            recordedTransforms.Add(data);
        }

        string filePath = $"Assets/RecordedXAxisPoints/RecordedTransforms_X{driverHand.position.x:F3}_Y{driverHand.position.y:F3}_Z{driverHand.position.z:F3}.txt";

        if (System.IO.File.Exists(filePath))
        {
            Debug.Log($"File already exists: {filePath}. Deleting it...");
            System.IO.File.Delete(filePath);
        }

        WriteTransformsToFile(filePath);
        Debug.Log("Joint data recorded for the target.");
    }

    private void WriteTransformsToFile(string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine($"Target X Position: {driverHand.position.x}");
            writer.WriteLine($"Target Y Position: {driverHand.position.y}");
            writer.WriteLine($"Target Z Position: {driverHand.position.z}");
            writer.WriteLine();
            foreach (TransformData data in recordedTransforms)
            {
                writer.WriteLine($"{data.jointName}: Rotation({data.rotation.eulerAngles.x}, {data.rotation.eulerAngles.y}, {data.rotation.eulerAngles.z})");
            }
            writer.WriteLine("--------------------------------------------------");
        }
        Debug.Log($"Transformations recorded successfully to {filePath}.");
    }

    public bool IsRecordingComplete()
    {
        return isRecordingComplete;
    }

    public void ResetRecordingState()
    {
        isRecordingComplete = false;
        hasRecordedForCurrentTarget = false;
    }


    private struct TransformData
    {
        public string jointName;
        public Quaternion rotation;
    }
}