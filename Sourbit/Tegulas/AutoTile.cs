using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Sourbit.Tegulas
{
    [CreateAssetMenu(fileName = "AutoTile", menuName = "Sourbit/Tegulas AutoTile")]
    public class AutoTile : TileBase
    {
        public Texture2D SourceTexture;

        Dictionary<int, Sprite> Tiles;

        override public void RefreshTile(Vector3Int location, ITilemap tilemap)
        {
            for (var y = -1; y <= 1; y++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    var position = new Vector3Int(location.x + x, location.y + y, location.z);
                    if (IsAutoTile(tilemap, position))
                    {
                        tilemap.RefreshTile(position);
                    }
                }
            }
        }

        override public void GetTileData(Vector3Int location, ITilemap tilemap, ref TileData tileData)
        {
            if (Tiles == null)
            {
                Prepare();
            }

            var mask = 0;

            if (IsAutoTile(tilemap, location + Vector3Int.up))
            {
                mask += Places.X1_Y2;
            }

            if (IsAutoTile(tilemap, location + Vector3Int.down))
            {
                mask += Places.X1_Y0;
            }

            if (IsAutoTile(tilemap, location + Vector3Int.left))
            {
                mask += Places.X0_Y1;
            }

            if (IsAutoTile(tilemap, location + Vector3Int.right))
            {
                mask += Places.X2_Y1;
            }

            if ((mask & (Places.X1_Y2 | Places.X0_Y1)) == (Places.X1_Y2 | Places.X0_Y1) && IsAutoTile(tilemap, location + Vector3Int.up + Vector3Int.left))
            {
                mask += Places.X0_Y2;
            }

            if ((mask & (Places.X1_Y2 | Places.X2_Y1)) == (Places.X1_Y2 | Places.X2_Y1) && IsAutoTile(tilemap, location + Vector3Int.up + Vector3Int.right))
            {
                mask += Places.X2_Y2;
            }

            if ((mask & (Places.X1_Y0 | Places.X0_Y1)) == (Places.X1_Y0 | Places.X0_Y1) && IsAutoTile(tilemap, location + Vector3Int.down + Vector3Int.left))
            {
                mask += Places.X0_Y0;
            }

            if ((mask & (Places.X1_Y0 | Places.X2_Y1)) == (Places.X1_Y0 | Places.X2_Y1) && IsAutoTile(tilemap, location + Vector3Int.down + Vector3Int.right))
            {
                mask += Places.X2_Y0;
            }

            var sprite = Tiles[mask];
            tileData.sprite = sprite;
        }

        public void Prepare()
        {
            Tiles = new Dictionary<int, Sprite>();
            var all = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(this));
            foreach (var item in all)
            {
                if (item is Sprite sprite)
                {
                    var key = Convert.ToInt16(sprite.name.Split(' ')[1]);
                    Tiles.Add(key, sprite);
                }
            }
        }

        bool IsAutoTile(ITilemap tilemap, Vector3Int position)
        {
            return tilemap.GetTile(position) == this;
        }
    }

    public struct Places
    {
        public const int ALL = 0b11111111;
        public const int X0_Y0 = 0b00000001;
        public const int X1_Y0 = 0b00000010;
        public const int X2_Y0 = 0b00000100;
        public const int X0_Y1 = 0b00001000;
        public const int X2_Y1 = 0b00010000;
        public const int X0_Y2 = 0b00100000;
        public const int X1_Y2 = 0b01000000;
        public const int X2_Y2 = 0b10000000;
    }
}