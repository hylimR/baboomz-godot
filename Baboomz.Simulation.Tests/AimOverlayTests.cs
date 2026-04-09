using System;
using NUnit.Framework;

namespace Baboomz.Tests.Editor
{
    /// <summary>
    /// Validates the power-percentage calculation used by AimOverlay.
    /// The formula: powerPct = (aimPower - minPower) / (maxPower - minPower) * 100, clamped 0-100.
    /// </summary>
    [TestFixture]
    public class AimOverlayTests
    {
        // Helper mirrors the formula in AimOverlay.LateUpdate
        static int CalcPowerPct(float aimPower, float minPower, float maxPower)
        {
            float powerRange = maxPower - minPower;
            if (powerRange <= 0f) return 100;
            int pct = (int)MathF.Round((aimPower - minPower) / powerRange * 100f);
            return Math.Clamp(pct, 0, 100);
        }

        [Test]
        public void PowerPct_AtMinPower_Returns0()
        {
            Assert.AreEqual(0, CalcPowerPct(minPower: 10f, aimPower: 10f, maxPower: 30f));
        }

        [Test]
        public void PowerPct_AtMaxPower_Returns100()
        {
            Assert.AreEqual(100, CalcPowerPct(minPower: 10f, aimPower: 30f, maxPower: 30f));
        }

        [Test]
        public void PowerPct_AtMidpoint_Returns50()
        {
            Assert.AreEqual(50, CalcPowerPct(minPower: 10f, aimPower: 20f, maxPower: 30f));
        }

        [Test]
        public void PowerPct_BelowMin_ClampsTo0()
        {
            Assert.AreEqual(0, CalcPowerPct(minPower: 10f, aimPower: 5f, maxPower: 30f));
        }

        [Test]
        public void PowerPct_AboveMax_ClampsTo100()
        {
            Assert.AreEqual(100, CalcPowerPct(minPower: 10f, aimPower: 50f, maxPower: 30f));
        }

        [Test]
        public void PowerPct_ZeroRange_Returns100()
        {
            // When min == max (no charge range), always show 100%
            Assert.AreEqual(100, CalcPowerPct(minPower: 15f, aimPower: 15f, maxPower: 15f));
        }

        [Test]
        public void PowerPct_ThreeQuarters_Returns75()
        {
            Assert.AreEqual(75, CalcPowerPct(minPower: 0f, aimPower: 15f, maxPower: 20f));
        }
    }
}
