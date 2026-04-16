using UnityEngine;
using UnityEditor;

namespace TwoChannelColorEncoding
{
    [CustomEditor(typeof(TwoChannelColorEncodingAsset))]
    public class TwoChannelColorEncodingAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);

            TwoChannelColorEncodingAsset asset = (TwoChannelColorEncodingAsset)target;

            if (GUILayout.Button("Create Material", GUILayout.Height(28)))
                CreateMaterialForAsset(asset, "TwoChannelColor/Decode Unlit");

            EditorGUILayout.Space(4);

            if (asset.encodedTexture != null)
            {
                int channels = 2;
                if (asset.extraSourceB != ChannelSource.None) channels++;
                if (asset.extraSourceA != ChannelSource.None) channels++;

                string format = channels == 2 ? "RG16" : "RGBA32";
                string packed = "";
                if (asset.extraSourceB != ChannelSource.None)
                    packed += $"\n  B ← {asset.extraSourceB} channel";
                if (asset.extraSourceA != ChannelSource.None)
                    packed += $"\n  A ← {asset.extraSourceA} channel";

                EditorGUILayout.HelpBox(
                    $"Encoded as {format} ({channels} packed channels).\n" +
                    $"R = luminance (√L), G = hue factor (t){packed}\n\n" +
                    $"Assign the material to a MeshRenderer.\n" +
                    $"The texture and base colors are set automatically.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Encode a texture first (Window > 2-Channel Color Encoding) " +
                    "to populate this asset.",
                    MessageType.Warning);
            }
        }

        void CreateMaterialForAsset(TwoChannelColorEncodingAsset asset, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"Shader '{shaderName}' not found. Import the URP Decode Shader sample from Package Manager.");
                return;
            }

            Material mat = new Material(shader);
            mat.name = asset.name + "_Mat";

            if (asset.encodedTexture != null)
                mat.SetTexture("_EncodedTex", asset.encodedTexture);

            mat.SetVector("_BC1", new Vector4(asset.bc1Linear.r, asset.bc1Linear.g, asset.bc1Linear.b, 0f));
            mat.SetVector("_BC2", new Vector4(asset.bc2Linear.r, asset.bc2Linear.g, asset.bc2Linear.b, 0f));
            mat.SetFloat("_DecodeGamma", asset.gamma);

            string assetPath = AssetDatabase.GetAssetPath(asset);
            string dir = string.IsNullOrEmpty(assetPath) ? "Assets" : System.IO.Path.GetDirectoryName(assetPath);
            string matPath = System.IO.Path.Combine(dir, asset.name + "_Mat.mat").Replace('\\', '/');

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(mat);

            Debug.Log($"Created material at {matPath}");
        }
    }
}