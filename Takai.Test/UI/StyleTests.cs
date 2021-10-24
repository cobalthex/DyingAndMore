using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Takai.UI;

namespace Takai.Test.UI
{
    [TestClass]
    public class StyleTests
    {
        struct NotStyleSheet : IStyleSheet<NotStyleSheet>
        {
            public string Name { get; set; }

            public void LerpWith(NotStyleSheet other, float t)
            {
                throw new System.NotImplementedException();
            }

            public void MergeWith(NotStyleSheet other)
            {
                throw new System.NotImplementedException();
            }
        }

        public StyleTests()
        {
            Data.Serializer.RegisterType<StyleSheet>();
            Data.Serializer.RegisterType<NotStyleSheet>();
        }

        [TestMethod]
        public void ImportsStyles()
        {
            StylesDictionary sdict = new StylesDictionary();

            var import = new Dictionary<string, IStyleSheet>
            {
                ["A"] = new StyleSheet(),
                ["A B"] = new StyleSheet(),
                ["A C"] = new StyleSheet(),
                ["A B C"] = new StyleSheet(),
                ["B A C"] = new StyleSheet { Color = Color.Gray },
                ["B C"] = new StyleSheet(),
            };

            sdict.ImportStyleSheets(import);
        }

        [TestMethod]
        public void DefaultStyles()
        {
            StylesDictionary sdict = new StylesDictionary();

            var import = new Dictionary<string, IStyleSheet>
            {
                [""] = new StyleSheet(),
                ["`Not"] = new NotStyleSheet(),
            };

            sdict.ImportStyleSheets(import);
        }

        [TestMethod]
        public void MismatchedDefaultStyles()
        {
            StylesDictionary sdict = new StylesDictionary();

            var import = new Dictionary<string, IStyleSheet>
            {
                [""] = new NotStyleSheet(),
            };

            Assert.ThrowsException<InvalidOperationException>(() => sdict.ImportStyleSheets(import));
        }

        // TODO: fill this out
    }
}
