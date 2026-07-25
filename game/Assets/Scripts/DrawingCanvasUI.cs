using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Minimal drawing canvas shell for the Week 3-4 vertical slice.
/// Handles show/hide, cursor state, and button wiring only.
/// </summary>
public class DrawingCanvasUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject canvasRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button submitButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button closeButton;

    [Header("Drawing")]
    [SerializeField] private StrokeCapture strokeCapture;
    [SerializeField] private CanvasToTexture canvasToTexture;

    [Header("Optional Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Input")]
    [SerializeField] private Key toggleKey = Key.Tab;
    [SerializeField] private bool openOnStart;

    private bool _isOpen;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            if (canvasRoot != null)
            {
                canvasGroup = canvasRoot.GetComponent<CanvasGroup>();
            }
        }

        if (canvasRoot == null)
        {
            Debug.LogWarning("DrawingCanvasUI: canvasRoot is not assigned. The UI panel will not toggle until it is wired in the Inspector.");
        }

        if (strokeCapture == null && canvasRoot != null)
        {
            strokeCapture = canvasRoot.GetComponentInChildren<StrokeCapture>(true);
        }

        if (canvasToTexture == null && canvasRoot != null)
        {
            canvasToTexture = canvasRoot.GetComponentInChildren<CanvasToTexture>(true);
        }

        if (canvasToTexture == null)
        {
            canvasToTexture = GetComponent<CanvasToTexture>();
        }
    }

    private void Start()
    {
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitClicked);
        }

        if (clearButton != null)
        {
            clearButton.onClick.AddListener(OnClearClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        SetOpen(openOnStart);
    }

    private void OnDestroy()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(OnSubmitClicked);
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveListener(OnClearClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            return;
        }

        if (_isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    private void SetOpen(bool isOpen)
    {
        _isOpen = isOpen;

        if (canvasRoot != null)
        {
            canvasRoot.SetActive(isOpen);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isOpen ? 1f : 0f;
            canvasGroup.interactable = isOpen;
            canvasGroup.blocksRaycasts = isOpen;
        }

        Cursor.visible = isOpen;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;

        if (statusText != null)
        {
            statusText.text = isOpen ? "Drawing Canvas Open" : string.Empty;
        }
    }

    private void OnSubmitClicked()
    {
        if (strokeCapture == null)
        {
            Debug.LogWarning("DrawingCanvasUI: Submit clicked but no StrokeCapture reference is assigned.");

            if (statusText != null)
            {
                statusText.text = "Submit failed: no StrokeCapture assigned.";
            }

            return;
        }

        int strokeCount = strokeCapture.Strokes.Count;
        int totalPointCount = 0;
        for (int i = 0; i < strokeCount; i++)
        {
            totalPointCount += strokeCapture.Strokes[i].Count;
        }

        Texture2D submittedTexture = null;
        if (canvasToTexture != null)
        {
            submittedTexture = canvasToTexture.ExportSnapshot();
        }

        if (submittedTexture == null)
        {
            Debug.LogWarning("DrawingCanvasUI: Submit did not produce a texture. Check CanvasToTexture wiring.");
        }

        string textureInfo = submittedTexture != null
            ? $", Texture={submittedTexture.width}x{submittedTexture.height}"
            : ", Texture=none";

        Debug.Log($"DrawingCanvasUI: Submit clicked. Strokes={strokeCount}, Points={totalPointCount}{textureInfo}");

        if (statusText != null)
        {
            statusText.text = submittedTexture != null
                ? $"Submitted: {strokeCount} strokes / {totalPointCount} points / {submittedTexture.width}x{submittedTexture.height}"
                : $"Submitted: {strokeCount} strokes / {totalPointCount} points / no texture";
        }
    }

    private void OnClearClicked()
    {
        Debug.Log("DrawingCanvasUI: Clear clicked.");

        if (strokeCapture != null)
        {
            strokeCapture.ClearStrokes();
        }
        else
        {
            Debug.LogWarning("DrawingCanvasUI: No StrokeCapture reference assigned. Clear will only update status text.");
        }

        if (statusText != null)
        {
            statusText.text = "Canvas cleared.";
        }
    }
}
