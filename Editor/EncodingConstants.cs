namespace TwoChannelColorEncoding
{
    public static class EncodingConstants
    {
        public const float Epsilon_Normalized = 1e-6f;
        public const float Epsilon_Degenerate = 1e-10f;
        public const float Epsilon_Convergence = 1e-12f;
        public const float Epsilon_DuplicateVertex = 1e-4f;
        public const float Epsilon_LuminanceFloor = 1e-6f;

        public const float LuminanceR = 0.2126f;
        public const float LuminanceG = 0.7152f;
        public const float LuminanceB = 0.0722f;

        public const float DefaultRmsWarningThreshold = 0.1f;
        public const float DefaultHueRangeWarningThreshold = 2.0f;
    }
}
