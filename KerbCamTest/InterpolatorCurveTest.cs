using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KerbCam;

namespace KerbCamTest
{
    [TestClass]
    public class InterpolatorCurveTest
    {
        private InterpolatorCurve<String> ic;

        [TestInitialize]
        public void SetUp()
        {
            ic = new InterpolatorCurve<String>();
            ic.AddKey(0f, "zero");
            ic.AddKey(1f, "one");
            ic.AddKey(2f, "two");
        }

        [TestMethod]
        public void TestOrdered()
        {
            Assert.AreEqual(0f, ic.TimeAt(0));
            Assert.AreEqual(1f, ic.TimeAt(1));
            Assert.AreEqual(2f, ic.TimeAt(2));
        }

        [TestMethod]
        public void TestAddMiddle()
        {
            ic.AddKey(0.5f, "point five");
            Assert.AreEqual(1, ic.FindLowerIndex(0.5f));
            Assert.AreEqual(1, ic.FindLowerIndex(0.6f));
            
        }

        [TestMethod]
        public void TestAddStart()
        {
            ic.AddKey(-1f, "minus one");
            Assert.AreEqual(0, ic.FindLowerIndex(-1f));
            Assert.AreEqual(0, ic.FindLowerIndex(-0.5f));
            Assert.AreEqual(-1, ic.FindLowerIndex(-1.5f));
        }

        [TestMethod]
        public void TestFindIndex()
        {
            Assert.AreEqual(-1, ic.FindLowerIndex(-1.0f));
            Assert.AreEqual(0, ic.FindLowerIndex(0.0f));
            Assert.AreEqual(0, ic.FindLowerIndex(0.5f));
            Assert.AreEqual(1, ic.FindLowerIndex(1.0f));
            Assert.AreEqual(1, ic.FindLowerIndex(1.5f));
            Assert.AreEqual(2, ic.FindLowerIndex(2.0f));
            Assert.AreEqual(2, ic.FindLowerIndex(2.5f));
            Assert.AreEqual(2, ic.FindLowerIndex(3.0f));
        }
    }
}
