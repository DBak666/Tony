using System;
using UnityEngine;

namespace AmazeRebuild
{
    [Serializable] public sealed class ArrowData { public int[] Points; }
    [Serializable] public sealed class LevelData
    {
        public int XSize, YSize;
        public string SourceId;
        public ArrowData[] Arrows;
        public int[] Solution;
        public static LevelData Load(int number)
        {
            var text = Resources.Load<TextAsset>("Levels/" + number.ToString("0000"));
            if (!text) throw new InvalidOperationException("Missing level " + number);
            return JsonUtility.FromJson<LevelData>(text.text);
        }
    }
    [Serializable] public sealed class CatalogEntry { public int number, arrows, width, height; public string sourceId; }
    [Serializable] public sealed class LevelCatalog { public CatalogEntry[] levels; }
}
