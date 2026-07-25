using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Captures drawing strokes from a UI surface and paints a simple live preview.
/// Stores strokes for later export and recognition steps.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class StrokeCapture : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Surface")]
    [SerializeField] private RectTransform drawingArea;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Image fallbackPreviewImage;

    [Header("Texture")]
    [SerializeField] private int textureSize = 512;
    [SerializeField] private Color32 backgroundColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color32 strokeColor = new Color32(20, 20, 20, 255);
    [SerializeField] private int strokeRadius = 4;

    private readonly List<List<Vector2>> _strokes = new List<List<Vector2>>();
    private List<Vector2> _activeStroke;
    private Texture2D _previewTexture;
    private Sprite _previewSprite;
    private RectTransform _rectTransform;

    public IReadOnlyList<List<Vector2>> Strokes => _strokes;

    public int TextureSize => _previewTexture != null ? _previewTexture.width : textureSize;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        if (drawingArea == null)
        {
            drawingArea = _rectTransform;
        }

        if (previewImage == null)
        {
            previewImage = GetComponent<RawImage>();
        }

        if (fallbackPreviewImage == null)
        {
            fallbackPreviewImage = GetComponent<Image>();
        }

        CreatePreviewTexture();
    }

    private void OnDestroy()
    {
        if (_previewTexture != null)
        {
            Destroy(_previewTexture);
            _previewTexture = null;
        }

        if (_previewSprite != null)
        {
            Destroy(_previewSprite);
            _previewSprite = null;
        }
    }

    public void ClearStrokes()
    {
        _strokes.Clear();
        _activeStroke = null;

        if (_previewTexture == null)
        {
            return;
        }

        FillTexture(backgroundColor);
        _previewTexture.Apply();
    }

    public Texture2D CreateSnapshotTexture()
    {
        if (_previewTexture == null)
        {
            return null;
        }

        Texture2D snapshot = new Texture2D(_previewTexture.width, _previewTexture.height, TextureFormat.RGBA32, false)
        {
            filterMode = _previewTexture.filterMode,
            wrapMode = _previewTexture.wrapMode
        };

        snapshot.SetPixels32(_previewTexture.GetPixels32());
        snapshot.Apply();
        return snapshot;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!TryGetLocalPoint(eventData, out Vector2 localPoint))
        {
            return;
        }

        _activeStroke = new List<Vector2>();
        _strokes.Add(_activeStroke);
        AddPoint(localPoint);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_activeStroke == null)
        {
            return;
        }

        if (!TryGetLocalPoint(eventData, out Vector2 localPoint))
        {
            return;
        }

        AddPoint(localPoint);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_activeStroke == null)
        {
            return;
        }

        if (TryGetLocalPoint(eventData, out Vector2 localPoint))
        {
            AddPoint(localPoint);
        }

        _activeStroke = null;
    }

    private void CreatePreviewTexture()
    {
        if (textureSize <= 0)
        {
            textureSize = 512;
        }

        _previewTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        FillTexture(backgroundColor);
        _previewTexture.Apply();

        if (previewImage != null)
        {
            previewImage.texture = _previewTexture;
        }
        else if (fallbackPreviewImage != null)
        {
            _previewSprite = Sprite.Create(
                _previewTexture,
                new Rect(0f, 0f, _previewTexture.width, _previewTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            fallbackPreviewImage.sprite = _previewSprite;
            fallbackPreviewImage.type = Image.Type.Simple;
            fallbackPreviewImage.preserveAspect = false;
        }
        else
        {
            Debug.LogWarning("StrokeCapture: No RawImage or Image found for preview output.");
        }
    }

    private void FillTexture(Color32 color)
    {
        Color32[] pixels = new Color32[_previewTexture.width * _previewTexture.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        _previewTexture.SetPixels32(pixels);
    }

    private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
    {
        RectTransform targetRect = drawingArea != null ? drawingArea : _rectTransform;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint);
    }

    private void AddPoint(Vector2 localPoint)
    {
        if (_activeStroke == null)
        {
            return;
        }

        if (_activeStroke.Count > 0)
        {
            Vector2 lastPoint = _activeStroke[_activeStroke.Count - 1];
            if ((lastPoint - localPoint).sqrMagnitude < 0.25f)
            {
                return;
            }

            DrawLine(lastPoint, localPoint);
        }
        else
        {
            DrawPoint(localPoint);
        }

        _activeStroke.Add(localPoint);
        _previewTexture.Apply();
    }

    private void DrawLine(Vector2 start, Vector2 end)
    {
        Rect targetRect = GetTargetRect();
        int steps = Mathf.CeilToInt(Vector2.Distance(start, end) * 2f);
        steps = Mathf.Max(steps, 1);

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 point = Vector2.Lerp(start, end, t);
            DrawPoint(point, targetRect);
        }
    }

    private void DrawPoint(Vector2 localPoint)
    {
        DrawPoint(localPoint, GetTargetRect());
    }

    private void DrawPoint(Vector2 localPoint, Rect targetRect)
    {
        Vector2 normalized = new Vector2(
            Mathf.InverseLerp(targetRect.xMin, targetRect.xMax, localPoint.x),
            Mathf.InverseLerp(targetRect.yMin, targetRect.yMax, localPoint.y));

        int pixelX = Mathf.Clamp(Mathf.RoundToInt(normalized.x * (_previewTexture.width - 1)), 0, _previewTexture.width - 1);
        int pixelY = Mathf.Clamp(Mathf.RoundToInt(normalized.y * (_previewTexture.height - 1)), 0, _previewTexture.height - 1);

        PaintCircle(pixelX, pixelY, strokeRadius, strokeColor);
    }

    private Rect GetTargetRect()
    {
        RectTransform targetRectTransform = drawingArea != null ? drawingArea : _rectTransform;
        return targetRectTransform.rect;
    }

    private void PaintCircle(int centerX, int centerY, int radius, Color32 color)
    {
        int radiusSqr = radius * radius;

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y > radiusSqr)
                {
                    continue;
                }

                int pixelX = centerX + x;
                int pixelY = centerY + y;

                if (pixelX < 0 || pixelX >= _previewTexture.width || pixelY < 0 || pixelY >= _previewTexture.height)
                {
                    continue;
                }

                _previewTexture.SetPixel(pixelX, pixelY, color);
            }
        }
    }
}