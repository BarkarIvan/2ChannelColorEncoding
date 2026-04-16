using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TwoChannelColorEncoding
{
    public class TwoChannelColorEncodingWindow : EditorWindow
    {
        static readonly GUIContent GC_SourceTexture = new GUIContent("Source Texture", "Color/albedo texture to encode. Values are treated as gamma/sRGB-like input.");
        static readonly GUIContent GC_Gamma = new GUIContent("Gamma", "Power curve for sRGB→linear approximation. pow(c, gamma). Default 2.0 matches the article's method.");
        static readonly GUIContent GC_ChannelWeights = new GUIContent("Channel Weights", "Per-channel weighting for plane fitting. Higher weight makes that channel more important in the fit.");
        static readonly GUIContent GC_WeightR = new GUIContent("R", "Weight for the Red channel during plane fitting.");
        static readonly GUIContent GC_WeightG = new GUIContent("G", "Weight for the Green channel during plane fitting. Higher default reflects human eye sensitivity.");
        static readonly GUIContent GC_WeightB = new GUIContent("B", "Weight for the Blue channel during plane fitting.");
        static readonly GUIContent GC_UseEigenSolve = new GUIContent("Use Eigen Solve", "Use Jacobi eigenvector method for plane fitting (faster, more accurate). Disable to use brute-force sphere sampling instead.");
        static readonly GUIContent GC_ClampHueFactor = new GUIContent("Clamp Hue Factor", "Clamp the hue interpolation factor t to [0,1]. Optional safety mode — may worsen round-trip for wide gamut textures. Default off per article.");
        static readonly GUIContent GC_Overwrite = new GUIContent("Overwrite Existing", "Overwrite existing encoded texture and metadata assets without confirmation.");
        static readonly GUIContent GC_GeneratePreviews = new GUIContent("Generate Previews", "Create decoded, error heatmap, hue factor and plane debug preview textures after encoding.");
        static readonly GUIContent GC_PreviewSize = new GUIContent("Preview Size", "Width/height of the preview thumbnails in pixels.");
        static readonly GUIContent GC_ExtraTexB = new GUIContent("Pack Texture → B", "Optional texture whose channel will be packed into the output B channel. Leave None for no packing (B=0).");
        static readonly GUIContent GC_ExtraChannelB = new GUIContent("B Source Channel", "Which channel from the extra texture to pack into B.");
        static readonly GUIContent GC_ExtraTexA = new GUIContent("Pack Texture → A", "Optional texture whose channel will be packed into the output A channel. Leave None for no packing (A=255).");
        static readonly GUIContent GC_ExtraChannelA = new GUIContent("A Source Channel", "Which channel from the extra texture to pack into A.");
        static readonly GUIContent GC_Compression = new GUIContent("Compression", "Platform-specific texture compression applied on import.\nBC5: best for RG data (2 channels)\nBC7/DXT5: for packed RGBA");

        Texture2D _sourceTexture;
        float _gamma = 2.0f;
        Vector3 _channelWeights = new Vector3(0.5f, 1.0f, 0.25f);
        bool _useEigenSolve = true;
        bool _clampHueFactor = false;
        bool _overwrite;
        bool _generatePreviews = true;
        string _outputFolder = "";
        Texture2D _extraTextureB;
        ChannelSource _extraSourceB;
        Texture2D _extraTextureA;
        ChannelSource _extraSourceA;
        TextureImporterFormat _compression = TextureImporterFormat.BC5;
        OutputFileFormat _fileFormat = OutputFileFormat.PNG;

        Vector2 _scrollPosition;
        float _previewSize = 256f;
        int _previewTab;

        bool _hasEncoded;
        EncodingData _data;
        EncodingAssets _assets;

        [MenuItem("Window/2-Channel Color Encoding")]
        public static void ShowWindow()
        {
            var window = GetWindow<TwoChannelColorEncodingWindow>("2-Ch Color Enc");
            window.minSize = new Vector2(420, 500);
        }

        void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.Space(2);

            DrawSourceSection();
            DrawSettingsSection();
            DrawActionsSection();

            if (_hasEncoded)
            {
                DrawWarningsSection();
                DrawPreviewSection();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.EndScrollView();
        }

        void DrawSourceSection()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("Source Texture", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _sourceTexture = (Texture2D)EditorGUILayout.ObjectField(
                GC_SourceTexture, _sourceTexture, typeof(Texture2D), false, GUILayout.Height(64));
            if (EditorGUI.EndChangeCheck())
            {
                _hasEncoded = false;
                _data.Dispose();
            }
            if (_sourceTexture != null)
            {
                EditorGUILayout.LabelField($"Size: {_sourceTexture.width} x {_sourceTexture.height}");
                EditorGUILayout.LabelField($"Format: {_sourceTexture.format}");
                EditorGUILayout.LabelField($"Readable: {_sourceTexture.isReadable}");
            }
            EditorGUILayout.EndVertical();
        }

        void DrawSettingsSection()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("Encoding Settings", EditorStyles.boldLabel);

            _gamma = EditorGUILayout.FloatField(GC_Gamma, _gamma);
            if (_gamma <= 0f) _gamma = 0.01f;

            EditorGUILayout.LabelField(GC_ChannelWeights);
            EditorGUI.indentLevel++;
            _channelWeights.x = EditorGUILayout.FloatField(GC_WeightR, _channelWeights.x);
            _channelWeights.y = EditorGUILayout.FloatField(GC_WeightG, _channelWeights.y);
            _channelWeights.z = EditorGUILayout.FloatField(GC_WeightB, _channelWeights.z);
            EditorGUI.indentLevel--;

            _useEigenSolve = EditorGUILayout.Toggle(GC_UseEigenSolve, _useEigenSolve);
            _clampHueFactor = EditorGUILayout.Toggle(GC_ClampHueFactor, _clampHueFactor);
            _overwrite = EditorGUILayout.Toggle(GC_Overwrite, _overwrite);
            _generatePreviews = EditorGUILayout.Toggle(GC_GeneratePreviews, _generatePreviews);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Channel Packing", EditorStyles.boldLabel);

            _extraTextureB = (Texture2D)EditorGUILayout.ObjectField(GC_ExtraTexB, _extraTextureB, typeof(Texture2D), false);
            if (_extraTextureB != null)
                _extraSourceB = (ChannelSource)EditorGUILayout.EnumPopup(GC_ExtraChannelB, _extraSourceB);
            else
                _extraSourceB = ChannelSource.None;

            _extraTextureA = (Texture2D)EditorGUILayout.ObjectField(GC_ExtraTexA, _extraTextureA, typeof(Texture2D), false);
            if (_extraTextureA != null)
                _extraSourceA = (ChannelSource)EditorGUILayout.EnumPopup(GC_ExtraChannelA, _extraSourceA);
            else
                _extraSourceA = ChannelSource.None;

            EditorGUILayout.Space(4);
            _fileFormat = (OutputFileFormat)EditorGUILayout.EnumPopup("File Format", _fileFormat);
            _compression = (TextureImporterFormat)EditorGUILayout.EnumPopup(GC_Compression, _compression);

            EditorGUILayout.Space(4);
            _previewSize = EditorGUILayout.Slider(GC_PreviewSize, _previewSize, 64, 512);

            EditorGUILayout.EndVertical();
        }

        void DrawActionsSection()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(_sourceTexture == null);

            if (GUILayout.Button("Encode", GUILayout.Height(28)))
                DoEncode();

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
        }

        void DrawWarningsSection()
        {
            if (_data.warnings == null || _data.warnings.Count == 0) return;

            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("Warnings", EditorStyles.boldLabel);
            foreach (string w in _data.warnings)
                EditorGUILayout.HelpBox(w, MessageType.Warning);
            EditorGUILayout.EndVertical();
        }

        void DrawPreviewSection()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            string[] tabs = new string[] { "Source", "Decoded", "Error", "Hue", "Plane" };
            _previewTab = GUILayout.Toolbar(_previewTab, tabs);

            EditorGUILayout.Space(4);

            float pw = Mathf.Min(_previewSize, position.width - 40);
            Rect previewRect = GUILayoutUtility.GetRect(pw, pw, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            previewRect.width = pw;
            previewRect.height = pw;

            Texture previewTex = null;
            switch (_previewTab)
            {
                case 0: previewTex = _sourceTexture; break;
                case 1: previewTex = _assets.decodedPreview; break;
                case 2: previewTex = _assets.errorHeatmap; break;
                case 3: previewTex = _assets.hueVisualization; break;
                case 4: previewTex = _assets.planeVisualization; break;
            }

            if (previewTex != null)
                EditorGUI.DrawPreviewTexture(previewRect, previewTex, null, ScaleMode.ScaleToFit);
            else
                EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f, 1f));

            if (_previewTab == 2)
                EditorGUILayout.LabelField("Blue=Low Error  Green=Mid  Red=High Error");
            if (_previewTab == 3)
                EditorGUILayout.LabelField("Hue factor mapped to color spectrum");
            if (_previewTab == 4)
                EditorGUILayout.LabelField("Red cross = bc1  Blue cross = bc2");

            EditorGUILayout.EndVertical();
        }

        void DoEncode()
        {
            _data.Dispose();
            EditorUtility.DisplayProgressBar("Encoding", $"Encoding {_sourceTexture.name}...", 0f);
            try
            {
                (_data, _assets) = EncoderFacade.Encode(_sourceTexture, GetCurrentSettings());
                _hasEncoded = true;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Repaint();
        }

        EncodingSettings GetCurrentSettings()
        {
            return new EncodingSettings
            {
                gamma = _gamma,
                channelWeights = _channelWeights,
                useEigenSolve = _useEigenSolve,
                clampHueFactor = _clampHueFactor,
                outputFolder = _outputFolder,
                overwrite = _overwrite,
                generatePreviews = _generatePreviews,
                extraTextureB = _extraTextureB,
                extraSourceB = _extraSourceB,
                extraTextureA = _extraTextureA,
                extraSourceA = _extraSourceA,
                compression = _compression,
                fileFormat = _fileFormat
            };
        }

        void OnDestroy()
        {
            _data.Dispose();
        }
    }
}
