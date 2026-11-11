using UnityEngine;

/// <summary>Comfort teleport with a ballistic, downward-curving preview.</summary>
public sealed class PitchTeleport : MonoBehaviour
{
    [SerializeField] private Transform origin;
    [SerializeField] private Transform head;
    [SerializeField] private Transform rig;
    [SerializeField] private float launchSpeed = 6f;
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private float maxDistance = 7f;

    private LineRenderer arc;
    private bool wasAiming;
    private bool valid;
    private Vector3 destination;
    private readonly Vector3[] points = new Vector3[40];
    private MeshRenderer[] roomRenderers;

    public void Configure(Transform hand, Transform camera)
    {
        origin = hand;
        head = camera;
        roomRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        if (camera != null) rig = camera.root;
        if (rig != null)
        {
            // This room's floor art has no reliable walkable collider. Keep a
            // bounded invisible landing surface at the rig's starting height.
            GameObject floor = new GameObject("Teleport landing surface");
            floor.transform.position = rig.position + Vector3.down * 0.025f;
            BoxCollider collider = floor.AddComponent<BoxCollider>();
            collider.size = new Vector3(7f, 0.05f, 7f);
        }
    }

    private void Awake()
    {
        arc = gameObject.AddComponent<LineRenderer>();
        arc.useWorldSpace = true;
        arc.widthMultiplier = 0.012f;
        arc.numCapVertices = 4;
        arc.positionCount = 0;
        arc.material = new Material(Shader.Find("Sprites/Default"));
        arc.enabled = false;
    }

    private void Update()
    {
        if (head == null || rig == null) return;
        bool keyboardAiming = Input.GetKey(KeyCode.T);
        bool aiming = keyboardAiming ||
            XRControllerInput.Stick(XRControllerInput.Hand.Left).y > 0.65f;
        if (aiming)
        {
            DrawArc(keyboardAiming);
        }
        else if (wasAiming)
        {
            arc.enabled = false;
            if (valid) Teleport();
        }
        wasAiming = aiming;
    }

    private void DrawArc(bool keyboardAiming)
    {
        Transform source = keyboardAiming || origin == null ? head : origin;
        Vector3 start = source.position;
        Vector3 direction = source.forward.normalized;
        if (source == head) direction = (direction + Vector3.down * 0.25f).normalized;
        Vector3 velocity = direction * launchSpeed;
        Vector3 previous = start;
        points[0] = start;
        valid = false;
        int count = 1;

        for (int i = 1; i < points.Length; i++)
        {
            float time = i * 0.055f;
            Vector3 next = start + velocity * time + Vector3.down * (0.5f * gravity * time * time);
            if (Vector3.Distance(start, next) > maxDistance) break;
            if (Physics.Linecast(previous, next, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                points[count++] = hit.point;
                valid = hit.normal.y > 0.75f &&
                    Mathf.Abs(hit.point.y - rig.position.y) < 1.2f &&
                    HasClearance(hit.point);
                destination = hit.point;
                break;
            }
            points[count++] = next;
            previous = next;
        }

        arc.enabled = true;
        arc.positionCount = count;
        for (int i = 0; i < count; i++) arc.SetPosition(i, points[i]);
        Color color = valid ? new Color(0.1f, 0.9f, 0.75f) : new Color(1f, 0.35f, 0.25f);
        arc.startColor = color;
        arc.endColor = color;
    }

    private bool HasClearance(Vector3 landing)
    {
        // Much of the imported furniture has no physics collider. Its visible
        // bounds still need to keep the player's standing space clear.
        foreach (MeshRenderer renderer in roomRenderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;
            Bounds bounds = renderer.bounds;
            if (bounds.size.x > 4f || bounds.size.z > 4f || bounds.size.y > 3f)
                continue;
            if (bounds.max.y < landing.y + 0.2f || bounds.min.y > landing.y + 1.75f)
                continue;
            if (Mathf.Abs(bounds.center.x - landing.x) < bounds.extents.x + 0.3f &&
                Mathf.Abs(bounds.center.z - landing.z) < bounds.extents.z + 0.3f)
                return false;
        }
        return true;
    }

    private void Teleport()
    {
        // Preserve the headset's room-scale offset inside the rig.
        Vector3 horizontal = destination - head.position;
        rig.position += new Vector3(horizontal.x, destination.y - rig.position.y, horizontal.z);
    }
}
