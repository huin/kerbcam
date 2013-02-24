using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KerbCam;

namespace KerbCamTest {
    public class FakeValueInterpolator : InterpolatorCurve<string>.IValueInterpolator {
        public string Evaluate(string a, string b, float t) {
            return string.Format("{0} {1} {2:0.0}",
                a, b, t);
        }
    }

    [TestClass]
    public class InterpolatorCurveTest {
        private InterpolatorCurve<String> ic;

        [TestInitialize]
        public void SetUp() {
            ic = new InterpolatorCurve<String>(new FakeValueInterpolator());
            ic.AddKey(0f, "zero");
            ic.AddKey(1f, "one");
            ic.AddKey(2f, "two");
        }

        [TestMethod]
        public void TestOrdered() {
            Assert.AreEqual(0f, ic[0].t);
            Assert.AreEqual(1f, ic[1].t);
            Assert.AreEqual(2f, ic[2].t);
        }

        [TestMethod]
        public void TestAddMiddle() {
            ic.AddKey(0.5f, "point five");
            Assert.AreEqual(1, ic.FindLowerIndex(0.5f));
            Assert.AreEqual(1, ic.FindLowerIndex(0.6f));
        }

        [TestMethod]
        public void TestMove() {
            Assert.AreEqual(2, ic.MoveKeyAt(1, 2.5f));
            Assert.AreEqual("zero", ic[0].value);
            Assert.AreEqual("two", ic[1].value);
            Assert.AreEqual("one", ic[2].value);
            Assert.AreEqual(2.5f, ic[2].t);
        }

        [TestMethod]
        public void TestMoveToSameIndex() {
            Assert.AreEqual(1, ic.MoveKeyAt(1, 1.5f));
            Assert.AreEqual("zero", ic[0].value);
            Assert.AreEqual("one", ic[1].value);
            Assert.AreEqual(1.5f, ic[1].t);
            Assert.AreEqual("two", ic[2].value);
        }

        [TestMethod]
        public void TestAddStart() {
            ic.AddKey(-1f, "minus one");
            Assert.AreEqual(0, ic.FindLowerIndex(-1f));
            Assert.AreEqual(0, ic.FindLowerIndex(-0.5f));
            Assert.AreEqual(-1, ic.FindLowerIndex(-1.5f));
        }

        [TestMethod]
        public void TestFindIndex() {
            Assert.AreEqual(-1, ic.FindLowerIndex(-1.0f));
            Assert.AreEqual(0, ic.FindLowerIndex(0.0f));
            Assert.AreEqual(0, ic.FindLowerIndex(0.5f));
            Assert.AreEqual(1, ic.FindLowerIndex(1.0f));
            Assert.AreEqual(1, ic.FindLowerIndex(1.5f));
            Assert.AreEqual(2, ic.FindLowerIndex(2.0f));
            Assert.AreEqual(2, ic.FindLowerIndex(2.5f));
            Assert.AreEqual(2, ic.FindLowerIndex(3.0f));
        }

        [TestMethod]
        public void TestEvaluate() {
            Assert.AreEqual("zero", ic.Evaluate(-1f));
            Assert.AreEqual("zero", ic.Evaluate(-0.5f));
            Assert.AreEqual("zero one 0.0", ic.Evaluate(0.0f));
            Assert.AreEqual("zero one 0.2", ic.Evaluate(0.2f));
            Assert.AreEqual("one two 0.0", ic.Evaluate(1.0f));
            Assert.AreEqual("one two 0.3", ic.Evaluate(1.3f));
            Assert.AreEqual("two", ic.Evaluate(2.0f));
            Assert.AreEqual("two", ic.Evaluate(2.4f));

            // Testing 0 to 1 values between frames that are != 1 apart.
            ic.AddKey(4f, "four");
            Assert.AreEqual("two four 0.1", ic.Evaluate(2.2f));
            Assert.AreEqual("four", ic.Evaluate(4.0f));
            Assert.AreEqual("four", ic.Evaluate(4.4f));

            ic.AddKey(0.5f, "half");
            Assert.AreEqual("half one 0.0", ic.Evaluate(0.5f));
            Assert.AreEqual("half one 0.5", ic.Evaluate(0.75f));
        }
    }
}
