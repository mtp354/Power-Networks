using System;
using System.Text;

namespace FantasyEarth.Fantasy
{
    /// <summary>Versioned, platform-independent FNV-1a over UTF-8. Never uses runtime string hashing.</summary>
    public static class StableSeed
    {
        public static ulong Hash(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            ulong hash = 14695981039346656037UL;
            unchecked
            {
                foreach (byte value in Encoding.UTF8.GetBytes(key)) { hash ^= value; hash *= 1099511628211UL; }
            }
            return hash;
        }
    }
}
