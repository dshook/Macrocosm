using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class NumberFormatTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void NumberFormatTestSimplePasses()
        {
            Assert.AreEqual("-2.1B", int.MinValue.ToShortFormat());
            Assert.AreEqual("-123M", (-123456789).ToShortFormat());
            Assert.AreEqual("-12M",  (-12345678).ToShortFormat());
            Assert.AreEqual("-1.2M", (-1234567).ToShortFormat());
            Assert.AreEqual("-123K", (-123456).ToShortFormat());
            Assert.AreEqual("-12K",  (-12345).ToShortFormat());
            Assert.AreEqual("-1.2K", (-1234).ToShortFormat());
            Assert.AreEqual("-10",   (-10).ToShortFormat());
            Assert.AreEqual("0",     0.ToShortFormat());
            Assert.AreEqual("10",    10.ToShortFormat());
            Assert.AreEqual("103",   103.ToShortFormat());
            Assert.AreEqual("1,234", 1234.ToShortFormat());
            Assert.AreEqual("12.3K", 12345.ToShortFormat());
            Assert.AreEqual("123K",  123456.ToShortFormat());
            Assert.AreEqual("1.23M", 1234567.ToShortFormat());
            Assert.AreEqual("12.3M", 12345678.ToShortFormat());
            Assert.AreEqual("123M",  123456789.ToShortFormat());
            Assert.AreEqual("2.15B", int.MaxValue.ToShortFormat());
        }

        [Test]
        public void NumberFormatFloatTest()
        {
            Assert.AreEqual("-1.2B", (-1234567890.12345f).ToShortFormat());
            Assert.AreEqual("-123M", (-123456789.12345f).ToShortFormat());
            Assert.AreEqual("-12M",  (-12345678.12345f).ToShortFormat());
            Assert.AreEqual("-1.2M", (-1234567.12345f).ToShortFormat());
            Assert.AreEqual("-123K", (-123456.12345f).ToShortFormat());
            Assert.AreEqual("-12K",  (-12345.12345f).ToShortFormat());
            Assert.AreEqual("-1.2K", (-1234.12345f).ToShortFormat());
            Assert.AreEqual("-10.1", (-10.12345f).ToShortFormat());
            Assert.AreEqual("0",     0.ToShortFormat());
            Assert.AreEqual("1.1",   1.12345f.ToShortFormat());
            Assert.AreEqual("10.1",  10.12345f.ToShortFormat());
            Assert.AreEqual("103.1", 103.12345f.ToShortFormat());
            Assert.AreEqual("1,234", 1234.12345f.ToShortFormat());
            Assert.AreEqual("12.3K", 12345.12345f.ToShortFormat());
            Assert.AreEqual("123K",  123456.12345f.ToShortFormat());
            Assert.AreEqual("1.23M", 1234567.12345f.ToShortFormat());
            Assert.AreEqual("12.3M", 12345678.12345f.ToShortFormat());
            Assert.AreEqual("123M",  123456789.12345f.ToShortFormat());
            Assert.AreEqual("1.23B", 1234567890.12345f.ToShortFormat());
        }

        [Test]
        public void NumberSuperShortFormatTest()
        {
            Assert.AreEqual("-2B",   int.MinValue.ToSuperShortFormat());
            Assert.AreEqual("-.1B",  (-123456789).ToSuperShortFormat());
            Assert.AreEqual("-12M",  (-12345678).ToSuperShortFormat());
            Assert.AreEqual("-1M",   (-1234567).ToSuperShortFormat());
            Assert.AreEqual("-.1M",  (-123456).ToSuperShortFormat());
            Assert.AreEqual("-12K",  (-12345).ToSuperShortFormat());
            Assert.AreEqual("-1K",   (-1234).ToSuperShortFormat());
            Assert.AreEqual("-10",   (-10).ToSuperShortFormat());
            Assert.AreEqual("0",     0.ToSuperShortFormat());
            Assert.AreEqual("10",    10.ToSuperShortFormat());
            Assert.AreEqual("103",   103.ToSuperShortFormat());
            Assert.AreEqual("1K",    1234.ToSuperShortFormat());
            Assert.AreEqual("12K",   12345.ToSuperShortFormat());
            Assert.AreEqual(".1M",   123456.ToSuperShortFormat());
            Assert.AreEqual("1M",    1234567.ToSuperShortFormat());
            Assert.AreEqual("12M",   12345678.ToSuperShortFormat());
            Assert.AreEqual(".1B",   123456789.ToSuperShortFormat());
            Assert.AreEqual("2B",    int.MaxValue.ToSuperShortFormat());
        }
    }
}
