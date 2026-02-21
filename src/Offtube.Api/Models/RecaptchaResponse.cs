using System.Text.Json.Serialization;

namespace Offtube.Api.Models
{
    public class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public float Score { get; set; } // Для v3: оценка от 0.0 (бот) до 1.0 (человек)

        [JsonPropertyName("action")]
        public string Action { get; set; }
    }
}
