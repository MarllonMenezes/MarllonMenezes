using System;
using System.IO;
using AlbaWorld.Game;
using UnityEngine;

namespace AlbaWorld.Runtime;

public sealed class PhotoExporter : IPhotoExportService
{
    public bool CaptureAndSave(SceneSnapshot snapshot)
    {
        try
        {
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            var bytes = texture.EncodeToPNG();
            UnityEngine.Object.Destroy(texture);

            var name = $"alba-world-{DateTime.Now:yyyyMMdd-HHmmss}.png";
            if (Application.platform == RuntimePlatform.Android)
                return AndroidPhotoBridge.Save(bytes, name);

            var folder = Path.Combine(Application.persistentDataPath, "AlbaWorldExports");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name), bytes);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Photo export failed: {exception.Message}");
            return false;
        }
    }
}

public static class AndroidPhotoBridge
{
    public static bool Save(byte[] png, string fileName)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using var version = new AndroidJavaClass("android.os.Build$VERSION");
        var sdkInt = version.GetStatic<int>("SDK_INT");
        if (sdkInt <= 28 &&
            !UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageWrite))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageWrite);
            return false;
        }
        using var exporter = new AndroidJavaClass("com.albaworldgames.albaworld.MediaStoreExporter");
        return exporter.CallStatic<bool>("savePng", png, fileName, Application.productName);
#else
        var folder = Path.Combine(Application.persistentDataPath, "AlbaWorldExports");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, fileName), png);
        return true;
#endif
    }
}
