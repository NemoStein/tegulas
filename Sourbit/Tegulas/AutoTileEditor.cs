using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sourbit.Tegulas
{
    [CustomEditor(typeof(AutoTile))]
    public class AutoTileEditor : Editor
    {
        override public void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Generate Tileset"))
            {
                BuildTileset();
            }
        }

        void BuildTileset()
        {
            var autoTile = (target as AutoTile);
            var assetPath = AssetDatabase.GetAssetPath(autoTile);
            var sourceTexture = autoTile.SourceTexture;

            if (sourceTexture == null)
            {
                throw new Exception("No Source Texture");
            }

            if (sourceTexture.height % 2 == 1)
            {
                throw new Exception($"Tile side (width and height) must be multiple of 2 (even). Odd tile height: {sourceTexture.height}");
            }

            if (sourceTexture.height != sourceTexture.width / 5f)
            {
                throw new Exception($"Tile must be square. Tile height: {sourceTexture.height}. Expected texture width: {sourceTexture.height * 5}.");
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in assets)
            {
                if (!(asset is AutoTile))
                {
                    DestroyImmediate(asset, true);
                }
            }

            var TileSide = sourceTexture.height;
            var TileHalfSide = TileSide / 2;
            var minSize = TileSide * 7;
            var powerOfTwo = 2;

            while (powerOfTwo < minSize)
            {
                powerOfTwo *= 2;
            }

            var texture = new Texture2D(powerOfTwo, powerOfTwo, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            var slicesMap = new Dictionary<int, List<int>>();
            var tileOffset = 0;

            for (var mask = 0; mask <= Places.ALL; mask++)
            {
                if (!(
                    ((~mask & Places.X1_Y0) != 0 && ((mask & Places.X0_Y0) != 0 || (mask & Places.X2_Y0) != 0)) ||
                    ((~mask & Places.X0_Y1) != 0 && ((mask & Places.X0_Y0) != 0 || (mask & Places.X0_Y2) != 0)) ||
                    ((~mask & Places.X2_Y1) != 0 && ((mask & Places.X2_Y0) != 0 || (mask & Places.X2_Y2) != 0)) ||
                    ((~mask & Places.X1_Y2) != 0 && ((mask & Places.X0_Y2) != 0 || (mask & Places.X2_Y2) != 0))
                ))
                {
                    var slices = SlicesFromBitmask(mask);

                    for (var slice = 0; slice < slices.Length; slice++)
                    {
                        var sliceMask = (slices[slice] << 2) + slice;

                        if (!slicesMap.ContainsKey(sliceMask))
                        {
                            slicesMap.Add(sliceMask, new List<int>());
                        }

                        slicesMap[sliceMask].Add(tileOffset);
                    }

                    var rect = new Rect(tileOffset % 7 * TileSide, tileOffset / 7 * TileSide, TileSide, TileSide);
                    var pivot = new Vector2(0.5f, 0.5f);
                    var sprite = Sprite.Create(texture, rect, pivot, TileSide);
                    sprite.name = "Sprite " + mask;

                    AssetDatabase.AddObjectToAsset(sprite, autoTile);

                    tileOffset++;
                }
            }

            foreach (var entry in slicesMap)
            {
                var sourceMask = entry.Key;

                var tileMask = sourceMask >> 2 & 0b111;
                var sliceMask = sourceMask & 0b11;

                var sourceX = tileMask * TileSide + (sliceMask & 1) * TileHalfSide;
                var sourceY = (sliceMask >> 1 & 1) * TileHalfSide;

                var slicePixels = sourceTexture.GetPixels(sourceX, sourceY, TileHalfSide, TileHalfSide);

                foreach (var targetMask in entry.Value)
                {
                    var sliceX = targetMask % 7 * TileSide + TileHalfSide * (sourceMask & 1);
                    var sliceY = targetMask / 7 * TileSide + TileHalfSide * (sourceMask >> 1 & 1);

                    texture.SetPixels(sliceX, sliceY, TileHalfSide, TileHalfSide, slicePixels);
                }
            }

            texture.Apply();
            texture.name = "Tileset Texture";

            AssetDatabase.AddObjectToAsset(texture, assetPath);
            AssetDatabase.ImportAsset(assetPath);

            autoTile.Prepare();
        }

        int[] SlicesFromBitmask(int bitmask)
        {
            return new int[]
            {
              SliceFromBits((bitmask & Places.X1_Y0) != 0, (bitmask & Places.X0_Y1) != 0, (bitmask & Places.X0_Y0) != 0),
              SliceFromBits((bitmask & Places.X1_Y0) != 0, (bitmask & Places.X2_Y1) != 0, (bitmask & Places.X2_Y0) != 0),
              SliceFromBits((bitmask & Places.X1_Y2) != 0, (bitmask & Places.X0_Y1) != 0, (bitmask & Places.X0_Y2) != 0),
              SliceFromBits((bitmask & Places.X1_Y2) != 0, (bitmask & Places.X2_Y1) != 0, (bitmask & Places.X2_Y2) != 0)
            };
        }

        int SliceFromBits(bool vertical, bool horizontal, bool corner)
        {
            if (corner) return 4;
            if (vertical && horizontal) return 3;
            if (horizontal) return 2;
            if (vertical) return 1;
            return 0;
        }
    }
}