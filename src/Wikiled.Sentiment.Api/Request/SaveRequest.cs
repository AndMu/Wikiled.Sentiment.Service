using System.ComponentModel.DataAnnotations;

namespace Wikiled.Sentiment.Api.Request
{
    public class SaveRequest
    {
        [Required]
        public SingleRequestData[] Documents { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string User { get; set; }
    }
}
