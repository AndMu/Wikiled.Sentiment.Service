using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Wikiled.Sentiment.Text.Data.Review;

namespace Wikiled.Sentiment.Api.Request
{
    public class WorkRequest
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, double> Dictionary { get; set; }

        [Required]
        public SingleProcessingData[] Documents { get; set; }

        public bool CleanText { get; set; }

        public string Domain { get; set; }
    }
}
