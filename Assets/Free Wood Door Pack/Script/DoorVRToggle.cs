using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class DoorVRToggle : MonoBehaviour
{
    public float openAngle = 90f;
    public float speed = 3f;
    public AudioClip openSound;
    public AudioClip closeSound;

    bool isOpen;
    Quaternion closedRot;
    Quaternion openRot;
    AudioSource audioSource;

    void Awake()
    {
        closedRot = transform.localRotation;
        openRot = closedRot * Quaternion.Euler(0f, openAngle, 0f);
        audioSource = GetComponent<AudioSource>();
        GetComponent<XRSimpleInteractable>().selectEntered.AddListener(_ => Toggle());
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        if (audioSource != null)
        {
            var clip = isOpen ? openSound : closeSound;
            if (clip != null) audioSource.PlayOneShot(clip);
        }
    }

    void Update()
    {
        var target = isOpen ? openRot : closedRot;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * speed);
    }
}