using Unity.Collections;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TwoChannelColorEncoding
{
    public enum OutputFileFormat
    {
        PNG,
        TGA
    }

    public struct EncodingSettings
    {
        public float gamma;
        public Vector3 channelWeights;
        public bool useEigenSolve;
        public bool clampHueFactor;
        public string outputFolder;
        public bool overwrite;
        public bool generatePreviews;

        public Texture2D extraTextureB;
        public ChannelSource extraSourceB;
        public Texture2D extraTextureA;
        public ChannelSource extraSourceA;
        public TextureImporterFormat compression;
        public OutputFileFormat fileFormat;

        public static EncodingSettings Default => new EncodingSettings
        {
            gamma = 2.0f,
            channelWeights = new Vector3(0.5f, 1.0f, 0.25f),
            useEigenSolve = true,
            clampHueFactor = false,
            outputFolder = "",
            overwrite = false,
            generatePreviews = true,
            extraSourceB = ChannelSource.None,
            extraSourceA = ChannelSource.None,
            compression = TextureImporterFormat.BC5,
            fileFormat = OutputFileFormat.PNG
        };

        public bool IsPacked => extraSourceB != ChannelSource.None || extraSourceA != ChannelSource.None;
    }

    public struct EncodingData : System.IDisposable
    {
        public Vector3 bc1;
        public Vector3 bc2;
        public Vector3 planeNormal;
        public ErrorMetrics metrics;
        public List<string> warnings;
        public int width;
        public int height;

        public Vector3 fx;
        public Vector3 fy;

        public NativeArray<Color> encodedPixels;
        public NativeArray<Vector3> linearPixels;
        public NativeArray<float> hueValues;

        public bool IsCreated => encodedPixels.IsCreated && linearPixels.IsCreated && hueValues.IsCreated;

        public void Dispose()
        {
            if (encodedPixels.IsCreated) encodedPixels.Dispose();
            if (linearPixels.IsCreated) linearPixels.Dispose();
            if (hueValues.IsCreated) hueValues.Dispose();
        }
    }

    public struct EncodingAssets
    {
        public TwoChannelColorEncodingAsset asset;
        public Texture2D decodedPreview;
        public Texture2D errorHeatmap;
        public Texture2D hueVisualization;
        public Texture2D planeVisualization;
    }
}