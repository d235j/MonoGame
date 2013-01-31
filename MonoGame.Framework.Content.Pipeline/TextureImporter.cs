﻿// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

namespace Microsoft.Xna.Framework.Content.Pipeline
{
    /// <summary>
    /// Provides methods for reading texture files for use in the Content Pipeline.
    /// </summary>
    [ContentImporter(".png", ".jpg", ".bmp", DisplayName = "Texture Importer - MonoGame", DefaultProcessor = "TextureProcessor")]
    public class TextureImporter : ContentImporter<TextureContent>
    {
        /// <summary>
        /// Initializes a new instance of TextureImporter.
        /// </summary>
        public TextureImporter()
        {
        }

        /// <summary>
        /// Called by the XNA Framework when importing a texture file to be used as a game asset. This is the method called by the XNA Framework when an asset is to be imported into an object that can be recognized by the Content Pipeline.
        /// </summary>
        /// <param name="filename">Name of a game asset file.</param>
        /// <param name="context">Contains information for importing a game asset, such as a logger interface.</param>
        /// <returns>Resulting game asset.</returns>
        public override TextureContent Import(string filename, ContentImporterContext context)
        {
            var output = new Texture2DContent();
            output._bitmap = new Bitmap(filename);

            // Force the input's pixelformat to ARGB32, so we can have a common pixel format to deal with.
            if (output._bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                output._bitmap = output._bitmap.Clone(new System.Drawing.Rectangle(System.Drawing.Point.Empty, output._bitmap.Size),
                                                      System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var imageData = output._bitmap.GetData();

            var bitmapContent = new PixelBitmapContent<Color>(output._bitmap.Width, output._bitmap.Height);
            bitmapContent.SetPixelData(imageData);

            output.Faces.Add(new MipmapChain(bitmapContent));
            return output;
        }
    }
}
