using UnityEngine;
using System.IO;

/// <summary>
/// Exports the current drawing surface into a standalone Texture2D snapshot.
/// This is the handoff point for recognition and DQS systems.
/// </summary>
public class CanvasToTexture : MonoBehaviour
{
    [SerializeField] private StrokeCapture strokeCapture;
    [SerializeField] private bool debugSavePng = true;

    private string DebugOutputPath
    {
        get
        {
            string dir = Path.Combine(Application.persistentDataPath, "DrawingDebug");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }
    }

    public Texture2D LastSubmittedTexture { get; private set; }

    private void Awake()
    {
        if (strokeCapture == null)
        {
            strokeCapture = GetComponentInChildren<StrokeCapture>(true);
        }
    }

    private void OnDestroy()
    {
        if (LastSubmittedTexture != null)
        {
            Destroy(LastSubmittedTexture);
            LastSubmittedTexture = null;
        }
    }

    public Texture2D ExportSnapshot()
    {
        if (strokeCapture == null)
        {
            Debug.LogWarning("CanvasToTexture: StrokeCapture reference is missing.");
            return null;
        }

        Texture2D snapshot = strokeCapture.CreateSnapshotTexture();
        if (snapshot == null)
        {
            Debug.LogWarning("CanvasToTexture: Snapshot export failed because source texture is not available.");
            return null;
        }

        if (LastSubmittedTexture != null)
        {
            Destroy(LastSubmittedTexture);
        }

        LastSubmittedTexture = snapshot;

        if (debugSavePng)
        {
            SaveDebugPng(snapshot);
        }

        return LastSubmittedTexture;
    }

    private void SaveDebugPng(Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        try
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            string filename = $"canvas_{timestamp}.png";
            string filepath = Path.Combine(DebugOutputPath, filename);

            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(filepath, pngData);

            Debug.Log($"CanvasToTexture: Debug PNG saved to {filepath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CanvasToTexture: Failed to save debug PNG: {e.Message}");
        }
    }
}
