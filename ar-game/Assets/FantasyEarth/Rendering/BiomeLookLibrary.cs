using System;
using FantasyEarth.Fantasy;
using UnityEngine;

namespace FantasyEarth.Rendering
{
    [CreateAssetMenu(menuName = "Fantasy Earth/Biome Look Library")]
    public sealed class BiomeLookLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public Biome Biome;
            public BiomeLook Look;
        }

        public Entry[] Entries;

        public bool TryGet(Biome biome, out BiomeLook look)
        {
            if (Entries != null)
            {
                foreach (Entry entry in Entries)
                {
                    if (entry.Biome == biome && entry.Look != null)
                    {
                        look = entry.Look;
                        return true;
                    }
                }
            }
            look = null;
            return false;
        }
    }
}
