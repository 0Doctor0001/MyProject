using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SafeKeyInteraction : MonoBehaviour
{
    [SerializeField]
    string keyObjectName = "key_with_tag";

    [SerializeField]
    string safeObjectName = "animated_safe";

    [SerializeField, Min(0.5f)]
    float interactionDistance = 2.5f;

    [SerializeField, Min(0.01f)]
    float safeProximityDistance = 0.25f;

    [SerializeField, Min(1f)]
    float doorOpenAngle = 55f;

    [SerializeField, Min(0.1f)]
    float doorOpenDuration = 1.5f;

    [SerializeField]
    string safeDocumentObjectName = "SOmeOnes";

    [SerializeField]
    Texture2D[] safeDocumentPages = System.Array.Empty<Texture2D>();

    GameObject m_Safe;
    GameObject m_Key;
    GameObject m_SafeDocumentObject;
    Rigidbody m_KeyBody;
    Collider m_KeyCollider;
    Collider m_SafeCollider;
    Transform m_Door;
    Quaternion m_DoorClosedRotation;
    DocumentViewer m_DocumentViewer;
    bool m_SafeColliderIsInteractionProxy;
    bool m_HasKey;
    bool m_ControllerKeyHeld;
    bool m_IsOpen;

    void Start()
    {
        m_DocumentViewer = FindAnyObjectByType<DocumentViewer>();
        if (m_DocumentViewer == null)
            Debug.LogError("DocumentViewer was not found. The safe document cannot be opened.", this);
        SetupSafeDocumentFolder();

        m_Safe = GameObject.Find(safeObjectName);
        if (m_Safe == null)
        {
            Debug.LogError($"Safe '{safeObjectName}' was not found in the scene.", this);
            return;
        }

        m_Key = GameObject.Find(keyObjectName);
        if (m_Key == null)
        {
            Debug.LogError(
                $"Key '{keyObjectName}' was not found in the active scene. Save the scene with the key object before entering Play mode.",
                this);
            return;
        }

        var safeBounds = GetRenderBounds(m_Safe);
        m_SafeCollider = m_Safe.GetComponent<Collider>();
        if (m_SafeCollider == null)
        {
            m_SafeCollider = EnsureBoundsCollider(m_Safe, safeBounds);
            m_SafeColliderIsInteractionProxy = true;
        }

        m_Door = FindChild(m_Safe.transform, "Safe door");
        if (m_Door == null)
        {
            Debug.LogError("The safe model has no 'Safe door' transform.", this);
            return;
        }

        m_DoorClosedRotation = m_Door.localRotation;

        var keyBounds = GetRenderBounds(m_Key);
        m_KeyCollider = EnsureBoundsCollider(m_Key, keyBounds);
        m_KeyBody = m_Key.GetComponent<Rigidbody>();
        if (m_KeyBody == null)
            m_KeyBody = m_Key.AddComponent<Rigidbody>();
        m_KeyBody.isKinematic = true;
        m_KeyBody.useGravity = false;
    }

    void Update()
    {
        if (m_Door != null && !m_IsOpen &&
            m_HasKey &&
            IsKeyNearSafe())
        {
            m_HasKey = true;
            m_IsOpen = true;
            if (m_SafeColliderIsInteractionProxy)
                m_SafeCollider.enabled = false;
            StartCoroutine(OpenDoor());
        }

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null && mouse == null)
            return;

        if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            HandleDesktopRay(Camera.main != null ? Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)) : default);
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Camera.main != null)
            HandleDesktopRay(Camera.main.ScreenPointToRay(mouse.position.ReadValue()));
    }

    void HandleDesktopRay(Ray ray)
    {
        if (ray.direction == Vector3.zero)
            return;

        var hits = Physics.RaycastAll(ray, interactionDistance);
        foreach (var hit in hits)
        {
            if (IsColliderOnObject(hit.collider, m_SafeDocumentObject))
            {
                OpenSafeDocument();
                return;
            }
        }

        foreach (var hit in hits)
        {
            if (m_KeyCollider != null && hit.collider == m_KeyCollider && !m_HasKey)
            {
                PickUpForDesktop();
                return;
            }
        }
    }

    static bool IsColliderOnObject(Collider collider, GameObject target)
    {
        return collider != null && target != null &&
               (collider.transform == target.transform ||
                collider.transform.IsChildOf(target.transform));
    }

    void PickUpForDesktop()
    {
        var cameraTransform = Camera.main != null ? Camera.main.transform : null;
        var hand = FindRightController();
        if (hand == null)
            hand = cameraTransform;

        if (hand == null)
        {
            Debug.LogError("Could not find a camera or right controller to hold the key.", this);
            return;
        }

        m_HasKey = true;
        m_KeyCollider.enabled = false;
        m_KeyBody.isKinematic = true;
        m_Key.transform.SetParent(hand, false);
        m_Key.transform.localPosition = hand == cameraTransform
            ? new Vector3(0.22f, -0.18f, 0.4f)
            : new Vector3(0.04f, -0.04f, 0.12f);
        m_Key.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Debug.Log("Safe key picked up.", this);
    }

    public bool TryPickUpKey(Collider target, Transform hand)
    {
        if (m_Key == null || m_KeyCollider == null ||
            m_ControllerKeyHeld || hand == null || !IsColliderOnObject(target, m_Key))
        {
            return false;
        }

        m_ControllerKeyHeld = true;
        m_HasKey = true;
        m_KeyCollider.enabled = false;
        m_Key.transform.SetParent(hand, false);
        m_Key.transform.localPosition = new Vector3(0f, 0f, 0.12f);
        m_Key.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        return true;
    }

    public void ReleaseControllerKey()
    {
        if (!m_ControllerKeyHeld)
            return;

        m_ControllerKeyHeld = false;
        m_HasKey = false;
        m_Key.transform.SetParent(null, true);
        m_KeyCollider.enabled = true;
    }

    public bool TryOpenSafeDocument(Collider target)
    {
        if (!IsColliderOnObject(target, m_SafeDocumentObject))
            return false;

        OpenSafeDocument();
        return true;
    }

    bool IsKeyNearSafe()
    {
        if (m_Key == null || m_SafeCollider == null)
            return false;

        var safeBounds = m_SafeCollider.bounds;
        safeBounds.Expand(safeProximityDistance * 2f);
        return GetRenderBounds(m_Key).Intersects(safeBounds);
    }

    IEnumerator OpenDoor()
    {
        var startRotation = m_Door.localRotation;
        var endRotation = m_DoorClosedRotation * Quaternion.Euler(0f, -doorOpenAngle, 0f);
        for (var elapsed = 0f; elapsed < doorOpenDuration; elapsed += Time.deltaTime)
        {
            m_Door.localRotation = Quaternion.Slerp(
                startRotation, endRotation, Mathf.Clamp01(elapsed / doorOpenDuration));
            yield return null;
        }

        m_Door.localRotation = endRotation;
    }

    void SetupSafeDocumentFolder()
    {
        if (safeDocumentPages == null || safeDocumentPages.Length == 0)
        {
            Debug.LogError("No pages are assigned to the safe document.", this);
            return;
        }

        m_SafeDocumentObject = GameObject.Find(safeDocumentObjectName);
        if (m_SafeDocumentObject == null)
        {
            Debug.LogError($"Safe document object '{safeDocumentObjectName}' was not found.", this);
            return;
        }

        if (m_SafeDocumentObject.GetComponentInChildren<Collider>() == null)
            EnsureBoundsCollider(m_SafeDocumentObject, GetRenderBounds(m_SafeDocumentObject));

        Debug.Log($"Safe document interaction is ready on '{m_SafeDocumentObject.name}'.", this);
    }

    void OpenSafeDocument()
    {
        if (!m_IsOpen)
            return;

        if (m_DocumentViewer == null)
        {
            Debug.LogError("DocumentViewer was not found. Cannot open the safe document.", this);
            return;
        }

        Debug.Log("Opening the safe document.", this);
        m_DocumentViewer.OpenDocument(safeDocumentPages);
    }

    static Transform FindRightController()
    {
        foreach (var transform in FindObjectsByType<Transform>())
        {
            if (transform.name == "Right Controller")
                return transform;
        }

        return null;
    }

    static Transform FindChild(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
                return child;

            var found = FindChild(child, targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    static Bounds GetRenderBounds(GameObject target)
    {
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(target.transform.position, Vector3.one * 0.1f);

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    static Collider EnsureBoundsCollider(GameObject target, Bounds bounds)
    {
        var existing = target.GetComponent<Collider>();
        if (existing != null)
            return existing;

        var box = target.AddComponent<BoxCollider>();
        var localMin = Vector3.positiveInfinity;
        var localMax = Vector3.negativeInfinity;
        for (var corner = 0; corner < 8; corner++)
        {
            var worldPoint = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                (corner & 1) == 0 ? -1f : 1f,
                (corner & 2) == 0 ? -1f : 1f,
                (corner & 4) == 0 ? -1f : 1f));
            var localPoint = target.transform.InverseTransformPoint(worldPoint);
            localMin = Vector3.Min(localMin, localPoint);
            localMax = Vector3.Max(localMax, localPoint);
        }

        box.center = (localMin + localMax) * 0.5f;
        box.size = localMax - localMin;
        return box;
    }
}
