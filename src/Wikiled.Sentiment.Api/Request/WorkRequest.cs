using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Wikiled.Sentiment.Api.Request
{
    public class WorkRequest : ICloneable
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, double> Dictionary { get; set; }

        [Required]
        public SingleRequestData[] Documents { get; set; }

        public bool AdjustDomain { get; set; }

        public bool CleanText { get; set; }

        public bool Emotions { get; set; }

        public string Domain { get; set; }

        public string Model { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
