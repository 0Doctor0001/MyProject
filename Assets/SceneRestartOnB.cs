using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneRestartOnB : MonoBehaviour
{
    private InputAction restartAction;

    private void Awake()
    {
        restartAction = new InputAction(
            "Restart Scene",
            InputActionType.Button,
            "<XRController>{RightHand}/secondaryButton");
        restartAction.performed += OnRestartPressed;
    }

    private void OnEnable()
    {
        restartAction?.Enable();
    }

    private void OnDisable()
    {
        restartAction.Disable();
    }

    private void OnDestroy()
    {
        restartAction.performed -= OnRestartPressed;
        restartAction?.Dispose();
    }

    private void OnRestartPressed(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
