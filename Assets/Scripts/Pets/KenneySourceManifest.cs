#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AlbaWorld.Pets;
using UnityEngine;

namespace AlbaWorld.Editor;

/// <summary>
/// Reads the project-local Kenney source manifest for editor validation.
/// This type is excluded from player builds by the UNITY_EDITOR guard.
/// </summary>
public sealed class KenneySourceManifest
{
    private const string ManifestPath = "Assets/Art3D/Pets/Source/KenneyCubePets/manifest.json";

    private KenneySourceManifest(string[] animalIds, IReadOnlyDictionary<string, string> sourcesById, string license)
    {
        AnimalIds = animalIds;
        SourcesById = sourcesById;
        License = license;
    }

    public string[] AnimalIds { get; }

    /// <summary>Authoritative source filename for each manifest animal ID.</summary>
    public IReadOnlyDictionary<string, string> SourcesById { get; }

    public string License { get; }

    public string SourceFor(string animalId)
    {
        if (string.IsNullOrWhiteSpace(animalId) || !SourcesById.TryGetValue(animalId, out var source))
            throw new KeyNotFoundException($"Kenney source manifest does not contain animal ID '{animalId}'.");
        return source;
    }

    public static KenneySourceManifest LoadForTests()
    {
        var path = AbsoluteProjectPath(ManifestPath);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Kenney source manifest is missing: {ManifestPath}", path);

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            throw new InvalidDataException($"Kenney source manifest could not be read: {ManifestPath}", exception);
        }

        ManifestData data;
        try
        {
            data = JsonUtility.FromJson<ManifestData>(json);
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"Kenney source manifest contains malformed JSON: {ManifestPath}", exception);
        }

        if (data == null)
            throw new InvalidDataException($"Kenney source manifest contains malformed JSON: {ManifestPath}");

        var animals = data.animals ?? Array.Empty<AnimalEntry>();
        Validate(data, animals);

        var ids = animals.Select(animal => animal.id).ToArray();
        var sourcesById = animals.ToDictionary(animal => animal.id, animal => animal.source, StringComparer.Ordinal);
        return new KenneySourceManifest(ids, sourcesById, data.license);
    }

    private static string AbsoluteProjectPath(string assetPath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("Unity project root could not be resolved from Application.dataPath.");
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void Validate(ManifestData data, IReadOnlyCollection<AnimalEntry> animals)
    {
        if (string.IsNullOrWhiteSpace(data.package))
            throw new InvalidDataException($"Kenney source manifest is missing package metadata: {ManifestPath}");
        if (string.IsNullOrWhiteSpace(data.sourcePath))
            throw new InvalidDataException($"Kenney source manifest is missing sourcePath metadata: {ManifestPath}");
        if (string.IsNullOrWhiteSpace(data.license))
            throw new InvalidDataException($"Kenney source manifest is missing license metadata: {ManifestPath}");
        if (animals.Count != KenneyPetIds.All.Length)
            throw new InvalidDataException(
                $"Kenney source manifest must contain exactly {KenneyPetIds.All.Length} animals, found {animals.Count}: {ManifestPath}");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var animal in animals)
        {
            if (animal == null || string.IsNullOrWhiteSpace(animal.id) || string.IsNullOrWhiteSpace(animal.source))
                throw new InvalidDataException($"Kenney source manifest contains an incomplete animal entry: {ManifestPath}");
            if (Path.IsPathRooted(animal.source) || animal.source.Replace('\\', '/').Split('/').Contains("..", StringComparer.Ordinal))
                throw new InvalidDataException($"Kenney source manifest contains an unsafe source path for '{animal.id}': {ManifestPath}");
            if (!ids.Add(animal.id))
                throw new InvalidDataException($"Kenney source manifest contains duplicate animal id '{animal.id}': {ManifestPath}");
        }
    }

    [Serializable]
    private sealed class ManifestData
    {
        public string package;
        public string sourcePath;
        public string license;
        public string creditUrl;
        public AnimalEntry[] animals;
    }

    [Serializable]
    private sealed class AnimalEntry
    {
        public string id;
        public string source;
    }
}
#endif
