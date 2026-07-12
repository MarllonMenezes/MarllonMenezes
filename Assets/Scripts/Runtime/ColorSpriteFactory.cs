using UnityEngine;

namespace AlbaWorld.Runtime;

public static class ColorSpriteFactory
{
    private static Sprite? _square;
    private static Sprite? _circle;

    public static Sprite Square
    {
        get
        {
            if (_square == null) _square = CreateSolid(32, 32, Color.white);
            return _square;
        }
    }

    public static Sprite Circle
    {
        get
        {
            if (_circle == null) _circle = CreateCircle(64, Color.white);
            return _circle;
        }
    }

    private static Sprite CreateSolid(int width, int height, Color color)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var pixels = new Color[width * height];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = color;
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width);
    }

    private static Sprite CreateCircle(int size, Color color)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var center = (size - 1) / 2f;
        var radius = center - 1;
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
            texture.SetPixel(x, y, distance <= radius ? color : Color.clear);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
