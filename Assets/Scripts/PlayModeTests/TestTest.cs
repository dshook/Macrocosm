using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class TestTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void NumberFormatTestSimplePasses()
        {
            Assert.AreEqual("10",   10.ToShortFormat());
        }
    }
}
