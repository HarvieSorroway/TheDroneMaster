using System;
using UnityEngine;

namespace TheDroneMaster.DMPS.MistTest
{
    /// <summary>对雨世界房间地形的只读视图，不再复制一份容易失去同步的瓦片快照。</summary>
    internal sealed class RoomGrid
    {
        public const float TileSize = 20f;

        public Room Room { get; }
        public int Width => Room.TileWidth;
        public int Height => Room.TileHeight;
        public Vector2 WorldOrigin => Vector2.zero;
        public bool HasSlopes { get; }

        public RoomGrid(Room room)
        {
            Room = room ?? throw new ArgumentNullException(nameof(room));
            HasSlopes = FindSlopes();
        }

        public byte GetCollisionGeometry(int x, int y)
        {
            if (IsOutside(x, y) || Room.GetTile(x, y).Terrain == Room.Tile.TerrainType.Solid)
                return 1;
            if (Room.GetTile(x, y).Terrain != Room.Tile.TerrainType.Slope)
                return 0;

            var slope = Room.IdentifySlope(x, y);
            if (slope == Room.SlopeDirection.UpLeft) return 2;
            if (slope == Room.SlopeDirection.UpRight) return 3;
            if (slope == Room.SlopeDirection.DownLeft) return 4;
            if (slope == Room.SlopeDirection.DownRight) return 5;
            return 0;
        }

        public bool IsSolidAtWorldPosition(Vector2 worldPosition)
        {
            var tile = Room.GetTilePosition(worldPosition);
            if (IsOutside(tile.x, tile.y)) return true;
            if (Room.GetTile(tile).Terrain == Room.Tile.TerrainType.Solid) return true;

            var slope = Room.IdentifySlope(tile);
            if (slope == Room.SlopeDirection.Broken) return false;

            var localX = worldPosition.x / TileSize - tile.x;
            var localY = worldPosition.y / TileSize - tile.y;
            if (slope == Room.SlopeDirection.UpLeft) return localY < localX;
            if (slope == Room.SlopeDirection.UpRight) return localY < 1f - localX;
            if (slope == Room.SlopeDirection.DownLeft) return localY > 1f - localX;
            return slope == Room.SlopeDirection.DownRight && localY > localX;
        }

        private bool FindSlopes()
        {
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                if (Room.GetTile(x, y).Terrain == Room.Tile.TerrainType.Slope)
                    return true;
            }
            return false;
        }

        private bool IsOutside(int x, int y)
        {
            return x < 0 || y < 0 || x >= Width || y >= Height;
        }
    }
}
