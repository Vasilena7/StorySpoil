

using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoil.model;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;



namespace StorySpoil
{
   
   
    public class ApiResponseDTO
    {
        [JsonPropertyName("msg")]
        public string Msg { get; set; }

        [JsonPropertyName("storyId")]
        public string StoryId { get; set; }
    }

    
    public class StoryDTO
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } 
    }

    [TestFixture]
    public class StorySpoilerApiTests
    {
        private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";
        private static string StoryId;
        private RestClient client;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            
            var loginClient = new RestClient(BaseUrl);
            var loginRequest = new RestRequest("/api/User/Authentication", Method.Post);
            loginRequest.AddJsonBody(new { userName = "asi", password = "asivasi" });

            var loginResponse = loginClient.Execute(loginRequest);
            Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                        $"Login failed: {loginResponse.StatusCode} - {loginResponse.Content}");

            var loginJson = JsonSerializer.Deserialize<JsonElement>(loginResponse.Content);
            var token = loginJson.GetProperty("accessToken").GetString();

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };
            client = new RestClient(options);
        }

        [Test, Order(1)]
        public void CreateStory_ShouldReturnCreated()
        {
            var story = new StoryDTO
            {
                Title = "Test Story",
                Description = "A description for test story",
                Url = "https://example.com/image.png"
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                        $"Create failed: {response.StatusCode} - {response.Content}");

            var dto = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(dto.StoryId, Is.Not.Null.And.Not.Empty);
            Assert.That(dto.Msg, Is.EqualTo("Successfully created!"));

            StoryId = dto.StoryId;
        }

        [Test, Order(2)]
        public void EditStory_ShouldReturnOk()
        {
            Assert.That(StoryId, Is.Not.Null, "CreateStory_ShouldRunFirst");

            var updated = new StoryDTO
            {
                Title = "Updated Test Story",
                Description = "Updated description",
                Url = "https://example.com/updated.png"
            };

            var request = new RestRequest($"/api/Story/Edit/{StoryId}", Method.Put);
            request.AddJsonBody(updated);

            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                        $"Edit failed: {response.StatusCode} - {response.Content}");

            var dto = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(dto.Msg, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllStories_ShouldReturnOkAndNonEmpty()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                        $"GetAll failed: {response.StatusCode} - {response.Content}");

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(json.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(json.GetArrayLength(), Is.GreaterThan(0));
        }

        [Test, Order(4)]
        public void DeleteStory_ShouldReturnOk()
        {
            Assert.That(StoryId, Is.Not.Null, "CreateStory_ShouldRunFirst");

            var request = new RestRequest($"/api/Story/Delete/{StoryId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                        $"Delete failed: {response.StatusCode} - {response.Content}");

            var dto = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(dto.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateStory_MissingFields_ShouldReturnBadRequest()
        {
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(new { title = "", description = "" });

            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                        $"Expected BadRequest, got {response.StatusCode} - {response.Content}");
        }

        [Test, Order(6)]
        public void EditNonExistingStory_ShouldReturnNotFound()
        {
            var request = new RestRequest($"/api/Story/Edit/invalid-id-1234", Method.Put);
            request.AddJsonBody(new StoryDTO
            {
                Title = "Non-existing",
                Description = "No effect",
                Url = ""
            });

            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                        $"Expected NotFound, got {response.StatusCode} - {response.Content}");

            var dto = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(dto.Msg, Does.Contain("No spoilers"));
        }

        [Test, Order(7)]
        public void DeleteNonExistingStory_ShouldReturnBadRequest()
        {
            var request = new RestRequest($"/api/Story/Delete/invalid-id-5678", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                        $"Expected BadRequest, got {response.StatusCode} - {response.Content}");

            var dto = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(dto.Msg, Does.Contain("Unable to delete this story spoiler"));
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}