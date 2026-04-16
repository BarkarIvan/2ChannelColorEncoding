using Unity.Collections;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace TwoChannelColorEncoding
{
    public static class AssetPipeline
    {
        public static Texture2D CreateOutputTexture(int w, int h, NativeArray<Color> encodedPixels,
            NativeArray<Color> extraPixelsB, ChannelSource sourceB,
            NativeArray<Color> extraPixelsA, ChannelSource sourceA)
        {
            bool isPacked = (extraPixelsB.IsCreated && sourceB != ChannelSource.None) ||
                            (extraPixelsA.IsCreated && sourceA != ChannelSource.None);

            TextureFormat fmt = isPacked ? TextureFormat.RGBA32 : TextureFormat.RG16;

            var tex = new Texture2D(w, h, fmt, false, true);
            int len = encodedPixels.Length;

            if (!isPacked)
            {
                var rg = new NativeArray<byte>(len * 2, Allocator.Temp);
                for (int i = 0; i < len; i++)
                {
                    Color c = encodedPixels[i];
                    rg[i * 2] = (byte)(Mathf.Clamp01(c.r) * 255f);
                    rg[i * 2 + 1] = (byte)(Mathf.Clamp01(c.g) * 255f);
                }
                tex.SetPixelData(rg, 0);
                rg.Dispose();
            }
            else
            {
                bool hasB = extraPixelsB.IsCreated && sourceB != ChannelSource.None;
                bool hasA = extraPixelsA.IsCreated && sourceA != ChannelSource.None;
                var rgba = new NativeArray<Color32>(len, Allocator.Temp);
                for (int i = 0; i < len; i++)
                {
                    Color enc = encodedPixels[i];
                    byte r = (byte)(Mathf.Clamp01(enc.r) * 255f);
                    byte g = (byte)(Mathf.Clamp01(enc.g) * 255f);
                    byte b = hasB ? ReadChannel(extraPixelsB, i, sourceB) : (byte)0;
                    byte a = hasA ? ReadChannel(extraPixelsA, i, sourceA) : (byte)255;
                    rgba[i] = new Color32(r, g, b, a);
                }
                tex.SetPixelData(rgba, 0);
                rgba.Dispose();
            }

            tex.Apply();
            return tex;
        }

        static byte ReadChannel(NativeArray<Color> pixels, int index, ChannelSource source)
        {
            if (!pixels.IsCreated || index >= pixels.Length) return 0;
            Color c = pixels[index];
            switch (source)
            {
                case ChannelSource.Red: return (byte)(Mathf.Clamp01(c.r) * 255f);
                case ChannelSource.Green: return (byte)(Mathf.Clamp01(c.g) * 255f);
                case ChannelSource.Blue: return (byte)(Mathf.Clamp01(c.b) * 255f);
                case ChannelSource.Alpha: return (byte)(Mathf.Clamp01(c.a) * 255f);
                default: return 0;
            }
        }

        public static string SaveEncodedTexture(
            Texture2D source, Texture2D encodedTex, EncodingSettings settings,
            bool isPacked, int width, int height)
        {
            string assetPath = AssetDatabase.GetAssetPath(source);
            string sourceDir = string.IsNullOrEmpty(assetPath) ? "Assets" : Path.GetDirectoryName(assetPath);
            string outputDir = string.IsNullOrEmpty(settings.outputFolder) ? sourceDir : settings.outputFolder;
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string baseName = string.IsNullOrEmpty(assetPath) ? source.name : Path.GetFileNameWithoutExtension(assetPath);
            string ext = settings.fileFormat == OutputFileFormat.TGA ? ".tga" : ".png";
            string encodedPath = Path.Combine(outputDir, baseName + "_Encoded" + ext).Replace('\\', '/');

            if (settings.overwrite || !File.Exists(encodedPath))
            {
                byte[] texBytes;
                if (settings.fileFormat == OutputFileFormat.TGA)
                    texBytes = encodedTex.EncodeToTGA();
                else
                    texBytes = encodedTex.EncodeToPNG();

                File.WriteAllBytes(encodedPath, texBytes);
                AssetDatabase.ImportAsset(encodedPath, ImportAssetOptions.ForceUpdate);
                ConfigureTextureImporter(encodedPath, settings.compression, isPacked);
            }

            return encodedPath;
        }

        public static TwoChannelColorEncodingAsset SaveMetadataAsset(
            Texture2D source, EncodingData data, EncodingSettings settings,
            string encodedPath, int width, int height, bool isPacked)
        {
            string assetPath = AssetDatabase.GetAssetPath(source);
            string sourceDir = string.IsNullOrEmpty(assetPath) ? "Assets" : Path.GetDirectoryName(assetPath);
            string outputDir = string.IsNullOrEmpty(settings.outputFolder) ? sourceDir : settings.outputFolder;
            string baseName = string.IsNullOrEmpty(assetPath) ? source.name : Path.GetFileNameWithoutExtension(assetPath);
            string metaPath = Path.Combine(outputDir, baseName + "_Encoding.asset").Replace('\\', '/');

            bool existingMeta = !settings.overwrite && File.Exists(metaPath);
            TwoChannelColorEncodingAsset metaAsset;

            if (existingMeta)
                metaAsset = AssetDatabase.LoadAssetAtPath<TwoChannelColorEncodingAsset>(metaPath);
            else
                metaAsset = ScriptableObject.CreateInstance<TwoChannelColorEncodingAsset>();

            metaAsset.encodedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(encodedPath);
            metaAsset.bc1Linear = new Color(data.bc1.x, data.bc1.y, data.bc1.z, 1f);
            metaAsset.bc2Linear = new Color(data.bc2.x, data.bc2.y, data.bc2.z, 1f);
            metaAsset.gamma = settings.gamma;
            metaAsset.channelWeights = settings.channelWeights;
            metaAsset.sourceTexture = source;
            metaAsset.sourceSize = new Vector2Int(width, height);
            metaAsset.rmsError = data.metrics.rmsError;
            metaAsset.maxError = data.metrics.maxError;
            metaAsset.clampHueFactor = settings.clampHueFactor;
            metaAsset.sourceAssetPath = assetPath;
            metaAsset.extraTextureB = settings.extraTextureB;
            metaAsset.extraSourceB = settings.extraSourceB;
            metaAsset.extraTextureA = settings.extraTextureA;
            metaAsset.extraSourceA = settings.extraSourceA;
            metaAsset.compression = settings.compression;

            if (!existingMeta)
                AssetDatabase.CreateAsset(metaAsset, metaPath);
            else
                EditorUtility.SetDirty(metaAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return metaAsset;
        }

        static void ConfigureTextureImporter(string path, TextureImporterFormat format, bool isPacked)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.sRGBTexture = false;
            importer.alphaSource = isPacked ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            var pc = new TextureImporterPlatformSettings
            {
                name = "PC",
                overridden = true,
                format = format,
                compressionQuality = (int)TextureCompressionQuality.Normal,
                maxTextureSize = 16384
            };
            importer.SetPlatformTextureSettings(pc);

            var ios = new TextureImporterPlatformSettings
            {
                name = "iPhone",
                overridden = true,
                format = format,
                compressionQuality = (int)TextureCompressionQuality.Normal,
                maxTextureSize = 4096
            };
            importer.SetPlatformTextureSettings(ios);

            var android = new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                format = format,
                compressionQuality = (int)TextureCompressionQuality.Normal,
                maxTextureSize = 4096
            };
            importer.SetPlatformTextureSettings(android);

            importer.SaveAndReimport();
        }

        public static NativeArray<Color> GetPixelData(Texture2D source)
        {
            if (source == null) return default;

            if (source.isReadable)
            {
                TextureFormat fmt = source.format;
                if (fmt == TextureFormat.RGBAFloat || fmt == TextureFormat.RGBA64 ||
                    fmt == TextureFormat.RGFloat || fmt == TextureFormat.RHalf)
                {
                    return source.GetRawTextureData<Color>();
                }
            }

            Texture2D readable = MakeReadableCopy(source);
            var data = readable.GetRawTextureData<Color>();
            NativeArray<Color> copy = new NativeArray<Color>(data.Length, Allocator.TempJob);
            copy.CopyFrom(data);
            Object.DestroyImmediate(readable);
            return copy;
        }

        public static void ReleasePixelData(ref NativeArray<Color> pixels)
        {
            if (pixels.IsCreated) pixels.Dispose();
        }

        static Texture2D MakeReadableCopy(Texture2D source)
        {
            RenderTexture rt = RenderTexture.GetTemporary(
                source.width, source.height, 0,
                RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false, true);
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readable.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return readable;
        }
    }
}