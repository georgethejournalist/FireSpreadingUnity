using System;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public static class TextureUtils
    {

        public static void SaveTexture(Texture2D tex, string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                Debug.LogError("Trying to save texture to undefined path.");
                return;
            }

            var pngData = tex.EncodeToPNG();
            if (pngData == null)
            {
                Debug.LogError("Could not convert " + tex.name + " to png. Skipping saving texture.");
                return;
            }

            File.WriteAllBytes(path + "/" + tex.name + ".png", pngData);
        }

        /// <summary>
        /// Clears the texture to the provided color value. If no color provided, uses white.
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="resetColor"></param>
        public static void ClearTextureToColor(this Texture2D tex, Color? resetColor = null)
        {
            if (!resetColor.HasValue)
            {
                resetColor = new Color32(0, 0, 0, 1);
            }

            Color32[] resetColorArray = tex.GetPixels32();

            for (int i = 0; i < resetColorArray.Length; i++)
            {
                resetColorArray[i] = resetColor.Value;
            }

            tex.SetPixels32(resetColorArray);
        }


        /// <summary>
        /// Converts two dimensional array index to one dimensional array index. Can be used for reading from texture, for example.
        /// </summary>
        /// <param name="x">First array index.</param>
        /// <param name="y">Second array index.</param>
        /// <param name="size">Size of the texture/array in the specified direction, i.e. width for LeftToRight reading, height for TopToBottom.</param>
        /// <param name="readingDirection">The direction of reading the texture.</param>
        /// <returns>Index in one dimensional array.</returns>
        public static int TwoDimensionIndexToOneDimension(int x, int y, int size, TexReadingDirection readingDirection = TexReadingDirection.LeftToRight)
        {
            int index = -1;
            switch (readingDirection)
            {
                case TexReadingDirection.LeftToRight:
                    index = x + (y * size);
                    break;
                case TexReadingDirection.TopToBottom:
                    index = y + (x * size);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(readingDirection), readingDirection, null);
            }

            return index;
        }
    }
}
