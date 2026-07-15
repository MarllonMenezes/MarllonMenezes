using System.Collections.Generic;

namespace AlbaWorld.Catalog
{
    public interface ICharacterPresetCatalog
    {
        CharacterPresetDefinition? Get(string id);
        IEnumerable<CharacterPresetDefinition> All { get; }
    }
}
