using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KerbCam;

namespace KerbCamTest {
    public class FakeValueInterpolator : Interpolator2<string>.IValueInterpolator {
        public string Evaluate(string a, string b, float t) {
            return string.Format("{0} {1} {2:0.0}",
                a, b, t);
        }
    }

    [TestClass]
    public class ParamSeriesTest {
        private ParamSeries<String> ps;

        [TestInitialize]
        public void SetUp() {
            ps = new ParamSeries<String>();
            ps.AddKey(0f, "zero");
            ps.AddKey(1f, "one");
            ps.AddKey(2f, "two");
        }

        [TestMethod]
        public void TestOrdered() {
            Assert.AreEqual(0f, ps.GetParam(0));
            Assert.AreEqual(1f, ps.GetParam(1));
            Assert.AreEqual(2f, ps.GetParam(2));
        }

        [TestMethod]
        public void TestAddMiddle() {
            ps.AddKey(0.5f, "point five");
            Assert.AreEqual(1, ps.FindLowerIndex(0.5f));
            Assert.AreEqual(1, ps.FindLowerIndex(0.6f));
        }

        [TestMethod]
        public void TestMove() {
            Assert.AreEqual(2, ps.MoveKeyAt(1, 2.5f));
            Assert.AreEqual("zero", ps[0]);
            Assert.AreEqual("two", ps[1]);
            Assert.AreEqual("one", ps[2]);
            Assert.AreEqual(2.5f, ps.GetParam(2));
        }

        [TestMethod]
        public void TestMoveToSameIndex() {
            Assert.AreEqual(1, ps.MoveKeyAt(1, 1.5f));
            Assert.AreEqual("zero", ps[0]);
            Assert.AreEqual("one", ps[1]);
            Assert.AreEqual(1.5f, ps.GetParam(1));
            Assert.AreEqual("two", ps[2]);
        }

        [TestMethod]
        public void TestAddStart() {
            ps.AddKey(-1f, "minus one");
            Assert.AreEqual(0, ps.FindLowerIndex(-1f));
            Assert.AreEqual(0, ps.FindLowerIndex(-0.5f));
            Assert.AreEqual(-1, ps.FindLowerIndex(-1.5f));
        }

        [TestMethod]
        public void TestFindIndex() {
            Assert.AreEqual(-1, ps.FindLowerIndex(-1.0f));
            Assert.AreEqual(0, ps.FindLowerIndex(0.0f));
            Assert.AreEqual(0, ps.FindLowerIndex(0.5f));
            Assert.AreEqual(1, ps.FindLowerIndex(1.0f));
            Assert.AreEqual(1, ps.FindLowerIndex(1.5f));
            Assert.AreEqual(2, ps.FindLowerIndex(2.0f));
            Assert.AreEqual(2, ps.FindLowerIndex(2.5f));
            Assert.AreEqual(2, ps.FindLowerIndex(3.0f));
        }
    }

    public class Interpolator2Test {

        private Interpolator2<String> ps;

        [TestInitialize]
        public void SetUp() {
            ps = new Interpolator2<String>(new FakeValueInterpolator());
            ps.AddKey(0f, "zero");
            ps.AddKey(1f, "one");
            ps.AddKey(2f, "two");
        }

        [TestMethod]
        public void TestEvaluate() {
            Assert.AreEqual("zero", ps.Evaluate(-1f));
            Assert.AreEqual("zero", ps.Evaluate(-0.5f));
            Assert.AreEqual("zero one 0.0", ps.Evaluate(0.0f));
            Assert.AreEqual("zero one 0.2", ps.Evaluate(0.2f));
            Assert.AreEqual("one two 0.0", ps.Evaluate(1.0f));
            Assert.AreEqual("one two 0.3", ps.Evaluate(1.3f));
            Assert.AreEqual("two", ps.Evaluate(2.0f));
            Assert.AreEqual("two", ps.Evaluate(2.4f));

            // Testing 0 to 1 values between frames that are != 1 apart.
            ps.AddKey(4f, "four");
            Assert.AreEqual("two four 0.1", ps.Evaluate(2.2f));
            Assert.AreEqual("four", ps.Evaluate(4.0f));
            Assert.AreEqual("four", ps.Evaluate(4.4f));

            ps.AddKey(0.5f, "half");
            Assert.AreEqual("half one 0.0", ps.Evaluate(0.5f));
            Assert.AreEqual("half one 0.5", ps.Evaluate(0.75f));
        }
    }
}
