using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace TwoChannelColorEncoding
{
    public static class EncoderFacade
    {
        public static (EncodingData data, EncodingAssets assets) Encode(Texture2D source, EncodingSettings settings)
        {
            var data = new EncodingData { warnings = new List<string>() };
            var assets = new EncodingAssets();

            if (source == null)
            {
                data.warnings.Add("Source texture is null.");
                return (data, assets);
            }

            NativeArray<Color> pixels = AssetPipeline.GetPixelData(source);
            int w = source.width;
            int h = source.height;

            data = FitPipeline.FitPlane(pixels, w, h, settings);
            FitPipeline.EncodeAndMeasure(pixels, ref data, settings);

            AssetPipeline.ReleasePixelData(ref pixels);

            NativeArray<Color> extraB = AssetPipeline.GetPixelData(settings.extraTextureB);
            NativeArray<Color> extraA = AssetPipeline.GetPixelData(settings.extraTextureA);

            Texture2D outputTex = AssetPipeline.CreateOutputTexture(
                w, h, data.encodedPixels,
                extraB, settings.extraSourceB,
                extraA, settings.extraSourceA);

            AssetPipeline.ReleasePixelData(ref extraB);
            AssetPipeline.ReleasePixelData(ref extraA);

            bool isPacked = settings.IsPacked;

            string encodedPath = AssetPipeline.SaveEncodedTexture(source, outputTex, settings, isPacked, w, h);
            assets.asset = AssetPipeline.SaveMetadataAsset(source, data, settings, encodedPath, w, h, isPacked);

            if (settings.generatePreviews)
                PreviewGenerator.Generate(ref data, ref assets);

            return (data, assets);
        }
    }
}