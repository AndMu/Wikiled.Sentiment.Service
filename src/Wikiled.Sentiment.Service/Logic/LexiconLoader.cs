﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using Wikiled.Common.Arguments;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Sentiment.Text.Resources;

namespace Wikiled.Sentiment.Service.Logic
{
    public class LexiconLoader : ILexiconLoader
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private Dictionary<string, ISentimentDataHolder> table;

        public IEnumerable<string> Supported => table.Select(item => item.Key);

        public void Load(string path)
        {
            Guard.NotNullOrEmpty(() => path, path);
            logger.Info("Loading lexicons: {0}", path);
            table = new Dictionary<string, ISentimentDataHolder>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.GetFiles(path))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                SentimentDataReader reader = new SentimentDataReader(file);
                var holder = SentimentDataHolder.Load(reader.Read());
                table[name] = holder;
            }

            logger.Info("Loaded {0} lexicons", table.Count);
        }

        public ISentimentDataHolder GetLexicon(string name)
        {
            Guard.NotNullOrEmpty(() => name, name);
            logger.Debug("Get lexicon: {0}", name);
            if (!table.TryGetValue(name, out var value))
            {
                throw new ArgumentOutOfRangeException(nameof(name), "Lexicon not found: " + name);
            }

            return value;
        }
    }
}