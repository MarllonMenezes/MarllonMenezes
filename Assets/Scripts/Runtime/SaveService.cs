using System;
using System.IO;
using AlbaWorld.Core;
using AlbaWorld.Game;
using UnityEngine;

namespace AlbaWorld.Runtime;

public sealed class LocalSaveService : ISaveService
{
    private const string FileName = "alba-world-save.json";
    private readonly string _path;

    public LocalSaveService()
    {
        _path = Path.Combine(Application.persistentDataPath, FileName);
    }

    public GameSaveData Load()
    {
        try
        {
            if (!File.Exists(_path))
                return SaveMigration.Upgrade(null);

            var json = File.ReadAllText(_path);
            return SaveMigration.Upgrade(JsonUtility.FromJson<GameSaveData>(json));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Save load failed; starting safely: {exception.Message}");
            return SaveMigration.Upgrade(null);
        }
    }

    public void Save(GameSaveData data)
    {
        var safe = SaveMigration.Upgrade(data);
        var temporary = _path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(temporary, JsonUtility.ToJson(safe, prettyPrint: true));
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(temporary, _path);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Save write failed: {exception.Message}");
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
