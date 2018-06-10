using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Wikiled.Sentiment.Text.Data.Review;

namespace Wikiled.Sentiment.Service.Logic.Request
{
    public class WorkRequest
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, double> Dictionary { get; set; }

        [Required]
        public SingleProcessingData[] Documents { get; set; }
    }
}
