using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DocumentViewer : MonoBehaviour
{
    public GameObject panel;        // DocumentCanvas
    public Image pageImage;         // PageImage
    public Sprite[] pages;          // страницы документа
    [SerializeField] Texture2D[] additionalPageTextures = System.Array.Empty<Texture2D>();
    public float distance = 0.7f;   // расстояние от глаз

    readonly List<Sprite> defaultPages = new List<Sprite>();
    readonly List<Sprite> allPages = new List<Sprite>();
    readonly List<Sprite> runtimePages = new List<Sprite>();
    int index;
    Transform cam;

    void Start()
    {
        if (Camera.main != null)
            cam = Camera.main.transform;
        else
            Debug.LogError("DocumentViewer could not find a camera tagged MainCamera.", this);

        allPages.Clear();
        defaultPages.Clear();
        if (pages != null)
        {
            defaultPages.AddRange(pages);
            allPages.AddRange(pages);
        }

        if (additionalPageTextures != null)
        {
            foreach (var texture in additionalPageTextures)
            {
                if (texture == null)
                {
                    Debug.LogError("DocumentViewer has an unassigned document page texture.", this);
                    continue;
                }

                var sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                defaultPages.Add(sprite);
                allPages.Add(sprite);
            }
        }

        panel.SetActive(false);
    }

    public void Toggle()
    {
        if (panel.activeSelf) Close(); else Open();
    }

    public void Open()
    {
        SetDefaultPages();
        OpenCurrentDocument();
    }

    public void OpenDocument(Texture2D[] textures)
    {
        foreach (var sprite in runtimePages)
            Destroy(sprite);
        runtimePages.Clear();
        allPages.Clear();

        if (textures != null)
        {
            foreach (var texture in textures)
            {
                if (texture == null)
                {
                    Debug.LogError("A page texture for the requested document is not assigned.", this);
                    continue;
                }

                var sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                runtimePages.Add(sprite);
                allPages.Add(sprite);
            }
        }

        OpenCurrentDocument();
    }

    void SetDefaultPages()
    {
        allPages.Clear();
        allPages.AddRange(defaultPages);
    }

    void OpenCurrentDocument()
    {
        if (cam == null && Camera.main != null)
            cam = Camera.main.transform;

        if (cam == null)
        {
            Debug.LogError("DocumentViewer cannot open because no MainCamera is available.", this);
            return;
        }

        if (panel == null || pageImage == null)
        {
            Debug.LogError("DocumentViewer is missing its panel or page image reference.", this);
            return;
        }

        index = 0;
        ShowPage();
        panel.SetActive(true);
        panel.transform.position = TargetPos();
        panel.transform.rotation = TargetRot();
    }

    public void Close()
    {
        panel.SetActive(false);
    }

    public void Next()
    {
        if (index < allPages.Count - 1) { index++; ShowPage(); }
    }

    public void Prev()
    {
        if (index > 0) { index--; ShowPage(); }
    }

    void ShowPage()
    {
        pageImage.rectTransform.localScale = Vector3.one * 2f;
        if (allPages.Count > 0) pageImage.sprite = allPages[index];
    }

    Vector3 TargetPos() => cam.position + cam.forward * distance;

    Quaternion TargetRot() => Quaternion.LookRotation(TargetPos() - cam.position, Vector3.up);
}