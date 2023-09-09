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

            if (sourceTexture.height % 2 != 0 || sourceTexture.width % 10 != 0)
            {
                throw new Exception($"Tile side (width and height) must be multiple of 2 (even).\nTile height: {sourceTexture.height}\nTile width: {sourceTexture.width / 5f}");
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in assets)
            {
                if (!(asset is AutoTile))
                {
                    DestroyImmediate(asset, true);
                }
            }

            var TileHeight = sourceTexture.height;
            var TileWidth = sourceTexture.width / 5;

            var TileHalfWidth = TileWidth / 2;
            var TileHalfHeight = TileHeight / 2;

            var AtlasWidth = (int) Mathf.Pow(2, Mathf.Ceil(Mathf.Log(TileWidth * 7, 2)));
            var AtlasHeight = (int) Mathf.Pow(2, Mathf.Ceil(Mathf.Log(TileHeight * 7, 2)));

            var texture = new Texture2D(AtlasWidth, AtlasHeight, TextureFormat.RGBA32, false)
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

                    var rect = new Rect(tileOffset % 7 * TileWidth, tileOffset / 7 * TileHeight, TileWidth, TileHeight);
                    var pivot = new Vector2(0.5f, 0.5f);
                    var sprite = Sprite.Create(texture, rect, pivot, TileHeight);
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

                var sourceX = tileMask * TileWidth + (sliceMask & 1) * TileHalfWidth;
                var sourceY = (sliceMask >> 1 & 1) * TileHalfHeight;

                var slicePixels = sourceTexture.GetPixels(sourceX, sourceY, TileHalfWidth, TileHalfHeight);

                foreach (var targetMask in entry.Value)
                {
                    var sliceX = targetMask % 7 * TileWidth + TileHalfWidth * (sourceMask & 1);
                    var sliceY = targetMask / 7 * TileHeight + TileHalfHeight * (sourceMask >> 1 & 1);

                    texture.SetPixels(sliceX, sliceY, TileHalfWidth, TileHalfHeight, slicePixels);
                }
            }

            texture.Apply();
            texture.name = "Tileset Texture";

            AssetDatabase.AddObjectToAsset(texture, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

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
            if (corner)
                return 4;
            if (vertical && horizontal)
                return 3;
            if (horizontal)
                return 2;
            if (vertical)
                return 1;
            return 0;
        }
    }
}