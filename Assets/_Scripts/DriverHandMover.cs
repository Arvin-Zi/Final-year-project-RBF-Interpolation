using UnityEngine;
using System.Collections;

public class DriverHandMover : MonoBehaviour
{
    public Transform driverHand; // Reference to the driver hand object
    public ArticulationDriver articulationDriver; // Reference to ArticulationDriver script

    // Main position arrays
    private float[] xPositions = { -0.55f, -0.4f, -0.25f, -0.1f, 0.25f, 0.5f, 0.75f, 1f };
    private float[] yPositions = { 0.86f, 1f, 1.25f, 1.5f, 1.75f, 2f };
    private float[] zPositions = { 0f, 0.5f, 1.25f };

    private int currentXIndex = 0, currentYIndex = 0, currentZIndex = 0;
    private bool isRecording = false;

    public float delayTime = 2f; // Delay before recording

    // Reset position after recording each target
    private readonly Vector3 resetPosition = new Vector3(0.563000023f, 2.31800008f, 0.509000003f);

    private void Start()
    {
        if (articulationDriver == null)
        {
            Debug.LogError("ArticulationDriver component reference is missing.");
            return;
        }
        StartCoroutine(HandleDriverHandMovement());
    }

    private IEnumerator HandleDriverHandMovement()
    {     
        while (currentXIndex < xPositions.Length)
        {
            Vector3 targetPosition = new Vector3(xPositions[currentXIndex], yPositions[currentYIndex], zPositions[currentZIndex]);
            yield return ProcessPoint(targetPosition);

            // After processing the target, return the driver hand to the reset position.
            driverHand.localPosition = resetPosition;
            Debug.Log($"Driver hand reset to starting position: {resetPosition}");
            yield return new WaitForSeconds(delayTime);

            MoveToNextPosition();
        }

        Debug.Log("Recording complete for all points.");
    }

    private IEnumerator ProcessPoint(Vector3 position)
    {
        driverHand.localPosition = position;
        Debug.Log($"Driver hand moved to position: {position}");

        yield return new WaitForSeconds(delayTime);

        articulationDriver.ResetRecordingState();
        isRecording = true;

        while (isRecording)
        {
            articulationDriver.CheckAndRecordTarget();

            if (articulationDriver.IsRecordingComplete())
            {
                Debug.Log($"Recording complete for position: {position}");
                isRecording = false;
            }

            yield return null;
        }
    }

    private void MoveToNextPosition()
    {
        currentZIndex++;
        if (currentZIndex >= zPositions.Length)
        {
            currentZIndex = 0;
            currentYIndex++;

            if (currentYIndex >= yPositions.Length)
            {
                currentYIndex = 0;
                currentXIndex++;
            }
        }
    }
}