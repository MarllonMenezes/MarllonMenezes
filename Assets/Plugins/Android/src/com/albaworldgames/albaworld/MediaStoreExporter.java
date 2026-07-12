package com.albaworldgames.albaworld;

import android.content.ContentResolver;
import android.content.ContentValues;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.net.Uri;
import android.os.Build;
import android.os.Environment;
import android.provider.MediaStore;

import java.io.File;
import java.io.FileOutputStream;
import java.io.OutputStream;

/** Minimal gallery writer: MediaStore on Android 10+, Pictures/Alba World on older devices. */
public final class MediaStoreExporter {
    private MediaStoreExporter() { }

    public static boolean savePng(byte[] png, String fileName, String appName) {
        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                ContentResolver resolver = com.unity3d.player.UnityPlayer.currentActivity.getContentResolver();
                ContentValues values = new ContentValues();
                values.put(MediaStore.Images.Media.DISPLAY_NAME, fileName);
                values.put(MediaStore.Images.Media.MIME_TYPE, "image/png");
                values.put(MediaStore.Images.Media.RELATIVE_PATH, Environment.DIRECTORY_PICTURES + "/Alba World");
                values.put(MediaStore.Images.Media.IS_PENDING, 1);
                Uri uri = resolver.insert(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, values);
                if (uri == null) return false;
                try (OutputStream stream = resolver.openOutputStream(uri)) {
                    if (stream == null) return false;
                    stream.write(png);
                }
                values.clear();
                values.put(MediaStore.Images.Media.IS_PENDING, 0);
                resolver.update(uri, values, null, null);
                return true;
            }

            File pictures = Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_PICTURES);
            File folder = new File(pictures, "Alba World");
            if (!folder.exists() && !folder.mkdirs()) return false;
            File output = new File(folder, fileName);
            try (FileOutputStream stream = new FileOutputStream(output)) {
                stream.write(png);
            }
            return true;
        } catch (Exception ignored) {
            return false;
        }
    }
}
