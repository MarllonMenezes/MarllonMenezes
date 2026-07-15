#if UNITY_EDITOR
using UnityEditor;

namespace AlbaWorld.Editor;

// The implementation is kept in Assets/Scripts/Pets so AlbaWorld.Tests can call the
// editor-only API without an Assembly-CSharp-Editor reference. This menu bridge keeps
// the conventional Assets/Editor entry point for artists and CI asset generation.
internal static class KenneyPetAssetSetupMenu
{
    [MenuItem("Alba World/Build Kenney Pet Prefabs (Editor)")]
    private static void Build()
    {
        KenneyPetAssetSetup.Setup();
    }
}
#endif
