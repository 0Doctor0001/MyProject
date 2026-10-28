using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

[DefaultExecutionOrder(1001)]
[RequireComponent(typeof(CharacterController))]
public sealed class SimulatedXRKeyboardLocomotion : MonoBehaviour
{
    [SerializeField, Min(0f)]
    float m_MoveSpeed = 5f;

    [SerializeField]
    float m_Gravity = -9.81f;

    CharacterController m_Controller;
    Transform m_Head;
    float m_VerticalSpeed;

    void Start()
    {
        if (!Application.isEditor)
        {
            enabled = false;
            return;
        }

        var simulator = FindObjectOfType<XRInteractionSimulator>();
        if (simulator == null)
        {
            enabled = false;
            return;
        }

        var xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin.Camera == null || xrOrigin.CameraFloorOffsetObject == null)
        {
            Debug.LogError("XR Origin must have a camera and camera floor offset object.", this);
            enabled = false;
            return;
        }

        m_Controller = GetComponent<CharacterController>();
        m_Head = xrOrigin.Camera.transform;

        var height = Mathf.Max(1f, xrOrigin.CameraFloorOffsetObject.transform.localPosition.y);
        m_Controller.height = height;
        m_Controller.center = new Vector3(m_Controller.center.x, height * 0.5f, m_Controller.center.z);
        m_Controller.radius = Mathf.Max(m_Controller.radius, 0.3f);

        simulator.translateXSpeed = 0f;
        simulator.translateYSpeed = 0f;
        simulator.translateZSpeed = 0f;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        var input = new Vector2(
            (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
            (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        var forward = m_Head.forward;
        forward.y = 0f;
        forward.Normalize();
        var right = Vector3.Cross(Vector3.up, forward);
        var movement = (forward * input.y + right * input.x) * m_MoveSpeed;

        m_VerticalSpeed = m_Controller.isGrounded
            ? -2f
            : m_VerticalSpeed + m_Gravity * Time.deltaTime;

        movement.y = m_VerticalSpeed;
        m_Controller.Move(movement * Time.deltaTime);
    }
}
