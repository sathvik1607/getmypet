using System.Collections.Generic;
using System.Linq;

namespace Petcompanion;

internal static class PetCatalog
{
    public static IPetDefinition Default { get; } = new DragonPet();
    public static IReadOnlyList<IPetDefinition> All { get; } = new IPetDefinition[] { Default, new CatPet(), new OwlPet(), new CatPet(true) };
    public static IPetDefinition Find(string? id) => All.FirstOrDefault(p => p.Id == id) ?? Default;
}