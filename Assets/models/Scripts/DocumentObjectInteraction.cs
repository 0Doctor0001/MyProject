using UnityEngine;
using UnityEngine.InputSystem;

public sealed class DocumentObjectInteraction : MonoBehaviour
{
    [SerializeField]
    Texture2D documentPage;

    [SerializeField, Min(0.5f)]
    float interactionDistance = 3f;

    Collider m_TargetCollider;
    DocumentViewer m_DocumentViewer;

    void Start()
    {
        m_DocumentViewer = FindAnyObjectByType<DocumentViewer>();
        if (m_DocumentViewer == null)
            Debug.LogError("DocumentViewer was not found. This document cannot be opened.", this);

        if (documentPage == null)
        {
            Debug.LogError("The document page texture is not assigned.", this);
            return;
        }

        m_TargetCollider = GetComponent<Collider>();
        if (m_TargetCollider == null)
            m_TargetCollider = AddBoundsCollider();

    }

    void Update()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse == null && keyboard == null)
            return;

        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Camera.main != null)
            TryOpen(Camera.main.ScreenPointToRay(mouse.position.ReadValue()));
        else if (keyboard != null && keyboard.eKey.wasPressedThisFrame && Camera.main != null)
            TryOpen(Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f)));
    }

    void TryOpen(Ray ray)
    {
        foreach (var hit in Physics.RaycastAll(ray, interactionDistance))
        {
            if (hit.collider == m_TargetCollider)
            {
                OpenDocument();
                return;
            }
        }
    }

    public void OpenDocument()
    {
        if (m_DocumentViewer == null)
        {
            Debug.LogError("DocumentViewer was not found. Cannot open this document.", this);
            return;
        }

        if (documentPage == null)
        {
            Debug.LogError("The document page texture is not assigned.", this);
            return;
        }

        m_DocumentViewer.OpenDocument(new[] { documentPage });
    }

    Collider AddBoundsCollider()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogError("This document object has no renderer for collider placement.", this);
            return null;
        }

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        var box = gameObject.AddComponent<BoxCollider>();
        var localMin = Vector3.positiveInfinity;
        var localMax = Vector3.negativeInfinity;
        for (var corner = 0; corner < 8; corner++)
        {
            var worldPoint = bounds.center + Vector3.Scale(bounds.extents, new Vector3(
                (corner & 1) == 0 ? -1f : 1f,
                (corner & 2) == 0 ? -1f : 1f,
                (corner & 4) == 0 ? -1f : 1f));
            var localPoint = transform.InverseTransformPoint(worldPoint);
            localMin = Vector3.Min(localMin, localPoint);
            localMax = Vector3.Max(localMax, localPoint);
        }

        box.center = (localMin + localMax) * 0.5f;
        box.size = localMax - localMin;
        return box;
    }
}
