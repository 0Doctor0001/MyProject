using DoorScript;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

[DefaultExecutionOrder(-1000)]
public sealed class MetaControllerInteractionBridge : MonoBehaviour
{
    const string RussianCharacters =
        "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя";

    [SerializeField, Min(0.5f)]
    float interactionDistance = 4f;

    [SerializeField, Min(0.01f)]
    float interactionRadius = 0.06f;

    Transform m_RightController;
    SafeKeyInteraction m_SafeInteraction;
    DocumentViewer m_DocumentViewer;
    UnityEngine.XR.InputDevice m_RightDevice;
    LineRenderer m_RayLine;
    Material m_RayMaterial;
    bool m_PreviousTrigger;
    bool m_PreviousSecondaryButton;
    bool m_HoldingKey;

    void Awake()
    {
        EnsureRussianGlyphs();
        FindReferences();
        DisableUnconnectedXriInteractables();
        CreateRayLine();
    }

    static void EnsureRussianGlyphs()
    {
        var fallbackFont = Resources.Load<TMP_FontAsset>(
            "Fonts & Materials/LiberationSans SDF - Fallback");
        if (fallbackFont == null)
        {
            Debug.LogError("The LiberationSans dynamic fallback font asset could not be loaded.");
            return;
        }

        if (!fallbackFont.TryAddCharacters(RussianCharacters, out var missingCharacters))
        {
            Debug.LogError(
                $"The LiberationSans fallback could not provide these Russian characters: {missingCharacters}",
                fallbackFont);
        }
    }

    void Update()
    {
        HandleDesktopInput();

        if (m_RightController == null)
            FindReferences();

        if (m_RightController == null)
        {
            if (m_HoldingKey)
                ReleaseKey();
            SetRayVisible(false);
            return;
        }

        m_RightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        var triggerPressed = m_RightDevice.isValid &&
            m_RightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var trigger) && trigger;
        var secondaryPressed = m_RightDevice.isValid &&
            m_RightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out var secondary) && secondary;

        if (triggerPressed && !m_PreviousTrigger)
            HandleTriggerPressed();
        else if (!triggerPressed && m_PreviousTrigger && m_HoldingKey)
            ReleaseKey();

        if (secondaryPressed && !m_PreviousSecondaryButton)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        m_PreviousTrigger = triggerPressed;
        m_PreviousSecondaryButton = secondaryPressed;
        UpdateRayLine();
    }

    void OnDestroy()
    {
        if (m_RayMaterial != null)
            Destroy(m_RayMaterial);
    }

    void FindReferences()
    {
        var controller = GameObject.Find("RightHandAnchor");
        if (controller == null)
            controller = GameObject.Find("RightControllerAnchor");
        m_RightController = controller != null ? controller.transform : null;

        if (m_SafeInteraction == null)
            m_SafeInteraction = FindAnyObjectByType<SafeKeyInteraction>();
        if (m_DocumentViewer == null)
            m_DocumentViewer = FindAnyObjectByType<DocumentViewer>();
    }

    void DisableUnconnectedXriInteractables()
    {
        foreach (var interactable in FindObjectsByType<XRSimpleInteractable>())
            interactable.enabled = false;
    }

    void HandleTriggerPressed()
    {
        var ray = new Ray(m_RightController.position, m_RightController.forward);
        if (!Physics.SphereCast(
                ray,
                interactionRadius,
                out var hit,
                interactionDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            return;
        }

        if (m_SafeInteraction != null &&
            m_SafeInteraction.TryPickUpKey(hit.collider, m_RightController))
        {
            m_HoldingKey = true;
            return;
        }

        if (m_SafeInteraction != null &&
            m_SafeInteraction.TryOpenSafeDocument(hit.collider))
        {
            return;
        }

        var document = hit.collider.GetComponentInParent<DocumentObjectInteraction>();
        if (document != null)
        {
            document.OpenDocument();
            return;
        }

        var door = hit.collider.GetComponentInParent<Door>();
        if (door != null)
        {
            door.OpenDoor();
            return;
        }

        if (IsMonitor(hit.collider) && m_DocumentViewer != null)
            m_DocumentViewer.Toggle();
    }

    void HandleDesktopInput()
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        Ray ray;
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            ray = camera.ScreenPointToRay(mouse.position.ReadValue());
        else if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        else
            return;

        if (m_DocumentViewer != null &&
            Physics.Raycast(ray, out var hit, interactionDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
            IsMonitor(hit.collider))
        {
            m_DocumentViewer.Toggle();
        }
    }

    void ReleaseKey()
    {
        if (m_SafeInteraction != null)
            m_SafeInteraction.ReleaseControllerKey();
        m_HoldingKey = false;
    }

    void UpdateRayLine()
    {
        if (m_RayLine == null)
            return;

        var ray = new Ray(m_RightController.position, m_RightController.forward);
        var end = ray.GetPoint(interactionDistance);
        if (Physics.SphereCast(
                ray,
                interactionRadius,
                out var hit,
                interactionDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            end = hit.point;
        }

        m_RayLine.SetPosition(0, ray.origin);
        m_RayLine.SetPosition(1, end);
        SetRayVisible(m_RightDevice.isValid);
    }

    void CreateRayLine()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return;

        m_RayMaterial = new Material(shader);
        m_RayMaterial.color = new Color(1f, 0.82f, 0.2f, 0.9f);

        var lineObject = new GameObject("Controller Interaction Ray");
        lineObject.transform.SetParent(transform, false);
        m_RayLine = lineObject.AddComponent<LineRenderer>();
        m_RayLine.useWorldSpace = true;
        m_RayLine.positionCount = 2;
        m_RayLine.startWidth = 0.008f;
        m_RayLine.endWidth = 0.004f;
        m_RayLine.startColor = m_RayMaterial.color;
        m_RayLine.endColor = m_RayMaterial.color;
        m_RayLine.material = m_RayMaterial;
        m_RayLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        m_RayLine.receiveShadows = false;
        m_RayLine.enabled = false;
    }

    void SetRayVisible(bool visible)
    {
        if (m_RayLine != null)
            m_RayLine.enabled = visible;
    }

    static bool IsMonitor(Collider target)
    {
        for (var current = target.transform; current != null; current = current.parent)
        {
            if (current.name == "Monitor_Apt_01" &&
                current.GetComponent<XRSimpleInteractable>() != null)
            {
                return true;
            }
        }

        return false;
    }
}
