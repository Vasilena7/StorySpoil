using System.Text.Json.Serialization;


namespace StorySpoil.model
{
    internal class StoryDTO
    {
        [JsonPropertyName("name")]

        public string? Name { get; set; }

        [JsonPropertyName("description")]

        public string? Description { get; set; }

        [JsonPropertyName("url")]

        public string? Url { get; set; }
    }
}
