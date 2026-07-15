using System;
using AlbaWorld.Core;
using AlbaWorld.Game;
using AlbaWorld.Pets;
using UnityEngine;

namespace AlbaWorld;

/// <summary>
/// Shared save/assembly seam used by the 3D studio, house and future photo mode.
/// </summary>
public sealed class PetPersistenceCoordinator
{
    public const string DefaultPetId = "pet.cat";

    private readonly GameSaveData _save;
    private readonly ISaveService _saveService;
    private readonly PetAssemblyController _assembly;

    public PetPersistenceCoordinator(GameSaveData save, ISaveService saveService, PetAssemblyController assembly)
    {
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        SaveMigration.Upgrade(_save);
    }

    public GameObject? ActivePetRoot => _assembly.ActiveInstance;

    public bool TrySelect(string petId)
    {
        var candidate = CloneLoadout(_save.pet);
        candidate.petId = string.IsNullOrWhiteSpace(petId) ? DefaultPetId : petId;
        if (!_assembly.TryApply(candidate))
            return false;

        var acceptedId = _assembly.ActivePetId;
        if (string.IsNullOrWhiteSpace(acceptedId))
            return false;

        _save.pet.petId = acceptedId;
        _save.selectedPetId = acceptedId;
        _save.schemaVersion = SaveMigration.CurrentSchemaVersion;
        _saveService.Save(_save);
        return true;
    }

    public bool Restore()
    {
        SaveMigration.Upgrade(_save);
        var requestedPetWasUnknown = !_assembly.HasUsablePet(_save.pet.petId);
        if (_assembly.TryApply(_save.pet))
        {
            MirrorActivePetId();
            return true;
        }

        var fallback = CloneLoadout(_save.pet);
        fallback.petId = DefaultPetId;
        var fallbackApplied = _assembly.TryApply(fallback);
        if (fallbackApplied && _assembly.ActiveInstance != null && _assembly.ActivePetId == DefaultPetId)
        {
            SetPetId(DefaultPetId);
            PersistRepairIfNeeded(requestedPetWasUnknown);
            return true;
        }

        if (_assembly.ActiveInstance != null)
            MirrorActivePetId();
        else
        {
            SetPetId(DefaultPetId);
            PersistRepairIfNeeded(requestedPetWasUnknown);
        }
        return false;
    }

    public bool TryPrepareHouse(Transform roomRoot, Vector3 desiredLocalPosition, Vector3 roomMin, Vector3 roomMax)
    {
        if (roomRoot == null || ActivePetRoot == null)
            return false;

        var min = Vector3.Min(roomMin, roomMax);
        var max = Vector3.Max(roomMin, roomMax);
        return Attach(roomRoot, new Vector3(
            Mathf.Clamp(desiredLocalPosition.x, min.x, max.x),
            Mathf.Clamp(desiredLocalPosition.y, min.y, max.y),
            Mathf.Clamp(desiredLocalPosition.z, min.z, max.z)));
    }

    public bool TryPreparePhoto(Transform photoRoot, Vector3 localPosition) =>
        photoRoot != null && ActivePetRoot != null && Attach(photoRoot, localPosition);

    private bool Attach(Transform parent, Vector3 localPosition)
    {
        var root = ActivePetRoot;
        if (root == null)
            return false;

        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        return true;
    }

    private void MirrorActivePetId()
    {
        if (!string.IsNullOrWhiteSpace(_assembly.ActivePetId))
            SetPetId(_assembly.ActivePetId);
    }

    private void SetPetId(string petId)
    {
        _save.pet.petId = petId;
        _save.selectedPetId = petId;
    }

    private void PersistRepairIfNeeded(bool requestedPetWasUnknown)
    {
        if (requestedPetWasUnknown)
            _saveService.Save(_save);
    }

    private static PetLoadoutData CloneLoadout(PetLoadoutData source) => new()
    {
        petId = source.petId,
        colorId = source.colorId,
        accessoryIds = source.accessoryIds == null ? Array.Empty<string>() : (string[])source.accessoryIds.Clone(),
        followCharacter = source.followCharacter,
        position = source.position == null ? new SerializableVector3(1.45f, 0.2f, 0.2f) : new SerializableVector3(source.position.x, source.position.y, source.position.z)
    };
}
