using UnityEngine;

namespace TwoChannelColorEncoding
{
    public enum ChannelSource
    {
        None,
        Red,
        Green,
        Blue,
        Alpha
    }

    public enum CompressionFormat
    {
        BC5,
        BC7,
        DXT5,
        EAC_RG,
        ASTC4x4,
        ASTC6x6,
        RGBA32,
        RG16
    }

    public enum OutputFileFormat
    {
        PNG,
        TGA
    }

    [CreateAssetMenu(fileName = "TwoChannelColorEncodingAsset", menuName = "Encoding/2-Channel Color Encoding")]
    public class TwoChannelColorEncodingAsset : ScriptableObject
    {
        public Texture2D encodedTexture;
        public Color bc1Linear;
        public Color bc2Linear;
        public float gamma = 2.0f;
        public Vector3 channelWeights = new Vector3(0.5f, 1.0f, 0.25f);

#if UNITY_EDITOR
        public Texture sourceTexture;
        public Vector2Int sourceSize;
        public float rmsError;
        public float maxError;
        public string sourceAssetPath;

        [Header("Channel Packing")]
        public Texture2D extraTextureB;
        public ChannelSource extraSourceB = ChannelSource.None;
        public Texture2D extraTextureA;
        public ChannelSource extraSourceA = ChannelSource.None;

        [Header("Compression")]
        public CompressionFormat compression = CompressionFormat.BC5;

        public bool IsPacked => extraSourceB != ChannelSource.None || extraSourceA != ChannelSource.None;
#endif

        public Vector3 BC1 => new Vector3(bc1Linear.r, bc1Linear.g, bc1Linear.b);
        public Vector3 BC2 => new Vector3(bc2Linear.r, bc2Linear.g, bc2Linear.b);
    }
}
