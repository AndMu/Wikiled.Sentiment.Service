using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Service.Tests.Data;

namespace Wikiled.Sentiment.Service.Tests.Logic
{
    [TestFixture]
    public class LexiconLoaderTests
    {
        private LexiconLoader instance;

        [SetUp]
        public void SetUp()
        {
            instance = CreateInstance();
        }

        [Test]
        public void Load()
        {
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Data", "Lexicons");
            instance.Load(path);
            Assert.AreEqual(2, instance.Supported.Count());
            var lexicons = instance.Supported.OrderBy(item => item).ToArray();
            Assert.AreEqual("base", lexicons[0]);
            Assert.AreEqual("other", lexicons[1]);
        }

        [Test]
        public void GetLexicon()
        {
            var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Data", "Lexicons");
            instance.Load(path);
            Assert.Throws<ArgumentOutOfRangeException>(() => instance.GetLexicon("Unknown"));
            TestWord word = new TestWord();
            word.Text = "one";
            var result = instance.GetLexicon("base").MeasureSentiment(word).DataValue.Value;
            Assert.AreEqual(1, result);

            result = instance.GetLexicon("other").MeasureSentiment(word).DataValue.Value;
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void CheckArguments()
        {
            Assert.Throws<ArgumentException>(() => instance.GetLexicon(null));
            Assert.Throws<ArgumentException>(() => instance.Load(null));
            Assert.Throws<ArgumentNullException>(() =>
            {
                var data = instance.Supported;
            });
        }

        private LexiconLoader CreateInstance()
        {
            return new LexiconLoader();
        }
    }
}
