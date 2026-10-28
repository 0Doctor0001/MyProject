using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(XROrigin))]
public sealed class SimulatedXRHeadHeight : MonoBehaviour
{
    [SerializeField, Min(0f)]
    float m_Height = 3.2f;

    void Start()
    {
        if (!Application.isEditor || FindObjectOfType<XRInteractionSimulator>() == null)
            return;

        var xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin.CameraFloorOffsetObject == null)
        {
            Debug.LogError("XR Origin has no Camera Floor Offset Object.", this);
            return;
        }

        var offset = xrOrigin.CameraFloorOffsetObject.transform;
        offset.localPosition = new Vector3(0f, m_Height, 0f);
    }
}
