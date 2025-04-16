using UnityEngine;

public class SphereInterpolation : MonoBehaviour
{
    public GameObject spherePrefab; // Prefab for spheres
    public Material redMaterial;
    public Material targetMaterial;
    public float maxX = 10f;        // Maximum X value for lines
    public float maxY = 5f;         // Maximum Y value for lines
    public float maxZ = 5f;         // Maximum Z value for lines

    [Range(0f, 1f)]
    public float targetX;

    [Range(0f, 1f)]
    public float targetZ;


    [Range(0f, 1f)]
    public float targetY;

    private GameObject[] spheres = new GameObject[4]; // Array to store spheres
    private GameObject[] newSpheres = new GameObject[2];
    private GameObject targetSphere; // The target sphere that will interpolate

    void Start()
    {
        // Initialize four spheres with their respective positions
        spheres[0] = Instantiate(spherePrefab, new Vector3(0, 0, 0), Quaternion.identity);      // Bottom-left
        spheres[1] = Instantiate(spherePrefab, new Vector3(0, 0, maxZ), Quaternion.identity);    // Bottom-right
        spheres[2] = Instantiate(spherePrefab, new Vector3(0, maxY, 0), Quaternion.identity);    // Top-left
        spheres[3] = Instantiate(spherePrefab, new Vector3(0, maxY, maxZ), Quaternion.identity);  // Top-right

        // Initialize new spheres for top and bottom planes
        newSpheres[0] = Instantiate(spherePrefab, new Vector3(0, 0, 0), Quaternion.identity);  // Bottom sphere
        newSpheres[1] = Instantiate(spherePrefab, new Vector3(0, 0, 0), Quaternion.identity);  // Top sphere

        newSpheres[0].GetComponent<Renderer>().material = redMaterial; // Bottom sphere red
        newSpheres[1].GetComponent<Renderer>().material = redMaterial; // Top sphere red

        targetSphere = Instantiate(spherePrefab, new Vector3(0, 0, 0), Quaternion.identity);
        targetSphere.GetComponent<Renderer>().material = targetMaterial;

        // Position the spheres at their initial locations
        UpdateSpherePositions();
    }

    void Update()
    {
        // Update sphere positions dynamically based on the slider
        UpdateSpherePositions();
    }

    void UpdateSpherePositions()
    {
        // Calculate and update positions for each sphere along its respective line
        // Interpolate on X axis only based on the targetX value
        spheres[0].transform.position = Interpolate(new Vector3(0, 0, 0), new Vector3(maxX, 0, 0));     // Bottom-left line
        spheres[1].transform.position = Interpolate(new Vector3(0, 0, maxZ), new Vector3(maxX, 0, maxZ));  // Bottom-right line
        spheres[2].transform.position = Interpolate(new Vector3(0, maxY, 0), new Vector3(maxX, maxY, 0));  // Top-left line
        spheres[3].transform.position = Interpolate(new Vector3(0, maxY, maxZ), new Vector3(maxX, maxY, maxZ)); // Top-right line

        // Interpolate between Z values for the new spheres based on targetZ
        Vector3 bottomInterpolatedPosition = CalculateInterpolatedPosition(spheres[0].transform.position, spheres[1].transform.position, targetZ, 0);
        Vector3 topInterpolatedPosition = CalculateInterpolatedPosition(spheres[2].transform.position, spheres[3].transform.position, targetZ, maxY);

        // Position the new spheres at the averaged Z positions on their respective planes
        newSpheres[0].transform.position = bottomInterpolatedPosition; // Bottom plane sphere
        newSpheres[1].transform.position = topInterpolatedPosition;   // Top plane sphere

        // Interpolate between the top and bottom spheres for the target sphere based on targetY
        Vector3 targetPosition = CalculateTargetPosition(bottomInterpolatedPosition, topInterpolatedPosition, targetY);
        targetSphere.transform.position = targetPosition;
    }


    Vector3 Interpolate(Vector3 start, Vector3 end)
    {
     
        // Interpolate between the start and end points on the X axis, while keeping the Y and Z constant
        return Vector3.Lerp(start, end, targetX);
    }
    Vector3 CalculateInterpolatedPosition(Vector3 start, Vector3 end, float zValue, float yHeight)
    {
        // Apply your provided formula for the Z position interpolation
        float interpolatedZ = (1 - zValue) * start.z + zValue * end.z; // Linear interpolation for Z

        // Interpolate X value based on targetX for consistency
        float interpolatedX = Mathf.Lerp(start.x, end.x, Mathf.InverseLerp(0f, maxX, targetX));

        return new Vector3(interpolatedX, yHeight, interpolatedZ);
    }
    // Calculate the target position between the bottom and top spheres based on Y interpolation
    Vector3 CalculateTargetPosition(Vector3 bottomPosition, Vector3 topPosition, float yValue)
    {
        // Interpolate between the Y positions of the bottom and top spheres based on targetY
        float interpolatedY = Mathf.Lerp(bottomPosition.y, topPosition.y, yValue);

        // Return the target position in 3D space
        return new Vector3(bottomPosition.x, interpolatedY, bottomPosition.z);
    }
}
