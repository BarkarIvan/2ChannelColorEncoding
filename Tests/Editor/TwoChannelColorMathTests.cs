using UnityEngine;
using NUnit.Framework;

namespace TwoChannelColorEncoding.Tests
{
    public class TwoChannelColorMathTests
    {
        const float Epsilon = 1e-4f;

        [Test]
        public void Luminance_White_Returns1()
        {
            Assert.AreEqual(1f, ColorSpace.Luminance(Vector3.one), Epsilon);
        }

        [Test]
        public void Luminance_Black_Returns0()
        {
            Assert.AreEqual(0f, ColorSpace.Luminance(Vector3.zero), Epsilon);
        }

        [Test]
        public void Luminance_PureGreen_MatchesWeight()
        {
            Assert.AreEqual(0.7152f, ColorSpace.Luminance(new Vector3(0, 1, 0)), Epsilon);
        }

        [Test]
        public void EncodeLuminance_DecodeLuminance_RoundTrip()
        {
            float original = 0.25f;
            float encoded = ColorEncoding.EncodeLuminance(original);
            float decoded = ColorEncoding.DecodeLuminance(encoded);
            Assert.AreEqual(original, decoded, Epsilon);
        }

        [Test]
        public void EncodeLuminance_ZeroStaysZero()
        {
            Assert.AreEqual(0f, ColorEncoding.EncodeLuminance(0f), Epsilon);
        }

        [Test]
        public void EncodeLuminance_OneStaysOne()
        {
            Assert.AreEqual(1f, ColorEncoding.EncodeLuminance(1f), Epsilon);
        }

        [Test]
        public void BuildPlaneBasis_OrthogonalUnitVectors()
        {
            PlaneGeometry.BuildPlaneBasis(new Vector3(1, 0, 0), new Vector3(0, 1, 0), out Vector3 fx, out Vector3 fy);
            Assert.AreEqual(1f, fx.magnitude, Epsilon);
            Assert.AreEqual(1f, fy.magnitude, Epsilon);
            Assert.AreEqual(0f, Vector3.Dot(fx, fy), Epsilon);
        }

        [Test]
        public void BuildPlaneBasis_ArbitraryColors_Orthogonal()
        {
            PlaneGeometry.BuildPlaneBasis(
                new Vector3(0.5f, 0.8f, 0.1f), new Vector3(0.2f, 0.3f, 0.9f),
                out Vector3 fx, out Vector3 fy);
            Assert.AreEqual(1f, fx.magnitude, Epsilon);
            Assert.AreEqual(1f, fy.magnitude, Epsilon);
            Assert.AreEqual(0f, Vector3.Dot(fx, fy), Epsilon);
        }

        [Test]
        public void ComputeHueFactor_AtBc1_Returns0()
        {
            Vector3 bc1 = new Vector3(1, 0, 0);
            Vector3 bc2 = new Vector3(0, 1, 0);
            PlaneGeometry.BuildPlaneBasis(bc1, bc2, out Vector3 fx, out Vector3 fy);
            float t = ColorEncoding.ComputeHueFactor(bc1, bc1, bc2, fx, fy, false);
            Assert.AreEqual(0f, t, Epsilon);
        }

        [Test]
        public void ComputeHueFactor_AtBc2_Returns1()
        {
            Vector3 bc1 = new Vector3(1, 0, 0);
            Vector3 bc2 = new Vector3(0, 1, 0);
            PlaneGeometry.BuildPlaneBasis(bc1, bc2, out Vector3 fx, out Vector3 fy);
            float t = ColorEncoding.ComputeHueFactor(bc2, bc1, bc2, fx, fy, false);
            Assert.AreEqual(1f, t, Epsilon);
        }

        [Test]
        public void ComputeHueFactor_Midpoint_Returns05()
        {
            Vector3 bc1 = new Vector3(1, 0, 0);
            Vector3 bc2 = new Vector3(0, 1, 0);
            Vector3 mid = (bc1 + bc2) * 0.5f;
            PlaneGeometry.BuildPlaneBasis(bc1, bc2, out Vector3 fx, out Vector3 fy);
            float t = ColorEncoding.ComputeHueFactor(mid, bc1, bc2, fx, fy, false);
            Assert.AreEqual(0.5f, t, Epsilon);
        }

        [Test]
        public void ComputeHueFactor_ClampNegative()
        {
            Vector3 bc1 = new Vector3(1, 0, 0);
            Vector3 bc2 = new Vector3(0, 1, 0);
            Vector3 testPt = -bc1 * 0.5f;
            PlaneGeometry.BuildPlaneBasis(bc1, bc2, out Vector3 fx, out Vector3 fy);
            float t = ColorEncoding.ComputeHueFactor(testPt, bc1, bc2, fx, fy, true);
            Assert.AreEqual(0f, t, Epsilon);
        }

        [Test]
        public void ComputeHueFactor_ClampAbove1()
        {
            Vector3 bc1 = new Vector3(1, 0, 0);
            Vector3 bc2 = new Vector3(0, 1, 0);
            Vector3 testPt = bc2 * 2f;
            PlaneGeometry.BuildPlaneBasis(bc1, bc2, out Vector3 fx, out Vector3 fy);
            float t = ColorEncoding.ComputeHueFactor(testPt, bc1, bc2, fx, fy, true);
            Assert.AreEqual(1f, t, Epsilon);
        }

        [Test]
        public void SmallestEigenvector_DiagonalMatrix_ReturnsSmallestAxis()
        {
            Vector3 n = LinearAlgebra.SmallestEigenvector(10f, 1f, 5f, 0f, 0f, 0f);
            Assert.AreEqual(0f, Mathf.Abs(n.x), Epsilon);
            Assert.AreEqual(1f, Mathf.Abs(n.y), Epsilon);
            Assert.AreEqual(0f, Mathf.Abs(n.z), Epsilon);
        }

        [Test]
        public void SmallestEigenvector_PlaneXY_ReturnsZ()
        {
            Vector3 n = LinearAlgebra.SmallestEigenvector(10f, 10f, 0.001f, 0f, 0f, 0f);
            Assert.AreEqual(0f, Mathf.Abs(n.x), Epsilon);
            Assert.AreEqual(0f, Mathf.Abs(n.y), Epsilon);
            Assert.AreEqual(1f, Mathf.Abs(n.z), Epsilon);
        }

        [Test]
        public void BruteForceNormal_PlaneXY_ReturnsZ()
        {
            var m = new ScatterMatrix { rr = 10f, gg = 10f, bb = 0.001f };
            Vector3 n = LinearAlgebra.BruteForceNormal(m);
            Assert.IsTrue(Mathf.Abs(n.z) > 0.9f, $"Expected z component close to 1, got {Mathf.Abs(n.z)}");
        }

        [Test]
        public void UndoWeightDistortion_RescalesCorrectly()
        {
            Vector3 n = new Vector3(1f, 1f, 1f).normalized;
            Vector3 weights = new Vector3(0.5f, 1f, 0.25f);
            Vector3 result = LinearAlgebra.UndoWeightDistortion(n, weights);
            Assert.AreEqual(1f, result.magnitude, Epsilon);
            Assert.IsTrue(result.x < result.y, "x should be smaller after applying smaller weight");
            Assert.IsTrue(result.z < result.x, "z should be smallest after applying smallest weight");
        }

        [Test]
        public void ComputeBaseColors_PlaneZEquals0_GivesXYAxes()
        {
            PlaneGeometry.ComputeBaseColors(new Vector3(0, 0, 1), out Vector3 bc1, out Vector3 bc2);
            Assert.IsFalse(bc1.magnitude < Epsilon, "bc1 should not be zero");
            Assert.IsFalse(bc2.magnitude < Epsilon, "bc2 should not be zero");
            Assert.AreEqual(0f, bc1.z, Epsilon, "bc1 should be on z=0 plane");
            Assert.AreEqual(0f, bc2.z, Epsilon, "bc2 should be on z=0 plane");
        }

        [Test]
        public void ComputeBaseColors_PlaneXEquals0_GivesYZAxes()
        {
            PlaneGeometry.ComputeBaseColors(new Vector3(1, 0, 0), out Vector3 bc1, out Vector3 bc2);
            Assert.AreEqual(0f, bc1.x, Epsilon, "bc1 should be on x=0 plane");
            Assert.AreEqual(0f, bc2.x, Epsilon, "bc2 should be on x=0 plane");
        }

        [Test]
        public void DecodeColor_RoundTrip_PreservesLuminance()
        {
            Vector3 bc1 = new Vector3(0.8f, 0.2f, 0.05f);
            Vector3 bc2 = new Vector3(0.1f, 0.6f, 0.15f);
            Vector3 original = new Vector3(0.4f, 0.35f, 0.08f);

            float lum = ColorSpace.Luminance(original);
            float encLum = ColorEncoding.EncodeLuminance(lum);

            PlaneGeometry.BuildPlaneBasis(bc1, bc2, out Vector3 fx, out Vector3 fy);
            float t = ColorEncoding.ComputeHueFactor(original, bc1, bc2, fx, fy, true);
            Vector3 decoded = ColorEncoding.DecodeColor(encLum, t, bc1, bc2);

            Assert.AreEqual(lum, ColorSpace.Luminance(decoded), 0.01f);
        }

        [Test]
        public void DecodeColor_AtBc1_WithFullLuminance()
        {
            Vector3 bc1 = new Vector3(1, 0, 0);
            Vector3 bc2 = new Vector3(0, 1, 0);
            float encLum = ColorEncoding.EncodeLuminance(ColorSpace.Luminance(bc1));
            Vector3 decoded = ColorEncoding.DecodeColor(encLum, 0f, bc1, bc2);
            Assert.AreEqual(bc1.x, decoded.x, Epsilon);
            Assert.AreEqual(bc1.y, decoded.y, Epsilon);
            Assert.AreEqual(bc1.z, decoded.z, Epsilon);
        }

        [Test]
        public void AreBaseColorsDegenerate_Parallel_ReturnsTrue()
        {
            Assert.IsTrue(ColorEncoding.AreBaseColorsDegenerate(new Vector3(1, 0, 0), new Vector3(0.99f, 0, 0)));
        }

        [Test]
        public void AreBaseColorsDegenerate_Perpendicular_ReturnsFalse()
        {
            Assert.IsFalse(ColorEncoding.AreBaseColorsDegenerate(new Vector3(1, 0, 0), new Vector3(0, 1, 0)));
        }

        [Test]
        public void ScatterMatrix_SinglePixel()
        {
            Color[] pixels = new Color[] { new Color(1f, 0f, 0f, 1f) };
            var m = ScatterMatrix.Accumulate(pixels, 2f, Vector3.one);
            Assert.AreEqual(1f, m.rr, Epsilon);
            Assert.AreEqual(0f, m.gg, Epsilon);
            Assert.AreEqual(0f, m.bb, Epsilon);
            Assert.AreEqual(0f, m.rg, Epsilon);
        }

        [Test]
        public void ToLinearRGB_Gamma2_Squared()
        {
            Vector3 linear = ColorSpace.ToLinearRGB(new Color(0.5f, 0.5f, 0.5f, 1f), 2.0f);
            Assert.AreEqual(0.25f, linear.x, Epsilon);
            Assert.AreEqual(0.25f, linear.y, Epsilon);
            Assert.AreEqual(0.25f, linear.z, Epsilon);
        }

        [Test]
        public void FullPipeline_ReconstructsGrayColor()
        {
            Vector3 bc1 = new Vector3(0.9f, 0.85f, 0.8f);
            Vector3 bc2 = new Vector3(0.15f, 0.1f, 0.05f);
            Vector3 original = new Vector3(0.5f, 0.5f, 0.5f);

            float lum = ColorSpace.Luminance(original);
            float encLum = ColorEncoding.EncodeLuminance(lum);

            PlaneGeometry.BuildPlaneBasis(bc1, bc2, out Vector3 fx, out Vector3 fy);
            float t = ColorEncoding.ComputeHueFactor(original, bc1, bc2, fx, fy, true);
            Vector3 decoded = ColorEncoding.DecodeColor(encLum, t, bc1, bc2);

            Assert.AreEqual(lum, ColorSpace.Luminance(decoded), 0.02f);
        }

        [Test]
        public void GetOrthogonal_ReturnsPerpendicularVector()
        {
            Vector3 v = new Vector3(0, 1, 0);
            Vector3 ortho = LinearAlgebra.GetOrthogonal(v);
            Assert.AreEqual(0f, Vector3.Dot(v, ortho), Epsilon);
            Assert.AreEqual(1f, ortho.magnitude, Epsilon);
        }

        [Test]
        public void LinearToGamma_RoundTrip()
        {
            Vector3 original = new Vector3(0.5f, 0.3f, 0.8f);
            Color gamma = ColorSpace.LinearToGamma(original, 2.0f);
            Assert.IsTrue(gamma.r > original.x, "gamma-corrected should be brighter than linear for mid-values");
            Assert.IsTrue(gamma.r < 1f, "gamma-corrected should be less than 1 for non-white input");
        }
    }
}
