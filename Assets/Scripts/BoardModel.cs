using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmazeRebuild
{
    // All points are head-first. Occupancy includes every cell along each segment.
    public sealed class BoardModel
    {
        public readonly LevelData Level;
        public readonly List<Vector2Int>[] Paths;
        public readonly bool[] Removed;
        readonly Dictionary<Vector2Int, int> occupied = new Dictionary<Vector2Int, int>();
        public int Remaining { get; private set; }
        public BoardModel(LevelData level)
        {
            Level = level;
            Paths = new List<Vector2Int>[level.Arrows.Length];
            Removed = new bool[Paths.Length];
            Remaining = Paths.Length;
            for (int i = 0; i < Paths.Length; i++)
            {
                Paths[i] = Expand(level.Arrows[i].Points, level.XSize, level.YSize);
                foreach (var p in Paths[i])
                {
                    if (occupied.ContainsKey(p)) throw new ArgumentException("Overlapping arrow cell " + p);
                    occupied.Add(p, i);
                }
            }
        }
        public static List<Vector2Int> Expand(int[] points, int width, int height)
        {
            if (width < 1 || height < 1 || points == null || points.Length < 2) throw new ArgumentException("Invalid arrow");
            var path = new List<Vector2Int>();
            foreach (var p in points) if (p < 0 || p >= width * height) throw new ArgumentException("Out of bounds point");
            for (int i = 0; i < points.Length - 1; i++)
            {
                var a = new Vector2Int(points[i] % width, points[i] / width);
                var b = new Vector2Int(points[i + 1] % width, points[i + 1] / width);
                if ((a.x != b.x && a.y != b.y) || a == b) throw new ArgumentException("Non-orthogonal segment");
                var dir = new Vector2Int(Math.Sign(b.x - a.x), Math.Sign(b.y - a.y));
                for (var p = a; p != b; p += dir) path.Add(p);
            }
            int last = points[points.Length - 1];
            path.Add(new Vector2Int(last % width, last / width));
            return path;
        }
        public Vector2Int Direction(int index) => Paths[index][0] - Paths[index][1];
        public int Blocker(int index, out int freeSteps)
        {
            freeSteps = 0;
            if (Removed[index]) return -1;
            var dir = Direction(index);
            for (var p = Paths[index][0] + dir; p.x >= 0 && p.y >= 0 && p.x < Level.XSize && p.y < Level.YSize; p += dir)
            {
                if (occupied.TryGetValue(p, out int other) && other != index) return other;
                freeSteps++;
            }
            return -1;
        }
        public bool CanExit(int index) => !Removed[index] && Blocker(index, out _) < 0;
        public bool TryRemove(int index)
        {
            if (!CanExit(index)) return false;
            Removed[index] = true;
            foreach (var p in Paths[index]) occupied.Remove(p);
            Remaining--;
            return true;
        }
        public int Hint()
        {
            for (int i = 0; i < Paths.Length; i++) if (CanExit(i)) return i;
            return -1;
        }
        public void RestoreRemoved(IEnumerable<int> indices)
        {
            foreach (int i in indices)
            {
                if (i < 0 || i >= Paths.Length || Removed[i]) continue;
                Removed[i] = true;
                foreach (var p in Paths[i]) occupied.Remove(p);
                Remaining--;
            }
        }
    }
}
