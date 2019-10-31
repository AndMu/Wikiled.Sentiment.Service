using System.ComponentModel.DataAnnotations;

namespace Wikiled.Sentiment.Api.Request
{
    public class SaveRequest
    {
        [Required]
        public SingleRequestData[] Documents { get; set; }

        public string Name { get; set; }
    }
}
