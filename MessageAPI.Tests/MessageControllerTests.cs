using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MessageAPI.Models;
using MessageAPI;
using Xunit;
using MessageAPI.Infrastructure;

namespace MessageAPI.Tests
{
    [Collection("Integration Tests")]
    public class MessageControllerTests : IClassFixture<MessageControllerTests.DbSetup>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        // This class provides setup before all class tests, then clean-up after
        [Collection("Integration Tests")]
        public class DbSetup : IDisposable
        {
            private MessageDbContext _dbContext;

            public DbSetup(WebApplicationFactory<Startup> factory)
            {
                // This fetches the same single lifetime instantiation used by Controller classes
                _dbContext = factory.Services.GetRequiredService<MessageDbContext>();

                // Seed in-memory database with some data needed for tests
                var message1 = new Message
                {
                    Id = 1,
                    Content = "Cool Message 1",
                };
                _dbContext.Messages.Add(message1);
                var message2 = new Message
                {
                    Id = 2,
                    Content = "Fun Message 2",
                };

                _dbContext.Messages.Add(message2);
                _dbContext.SaveChanges();
            }

            public void Dispose()
            {
                var message = _dbContext.Messages.ToArray();
                _dbContext.Messages.RemoveRange(message);
                _dbContext.SaveChanges();
            }
        }

        public MessageControllerTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetMessage_ReturnsSuccessAndMessage()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/message/1");

            response.EnsureSuccessStatusCode();
            Assert.NotNull(response.Content);
            var responseMesssage = JsonSerializer.Deserialize<Message>(
                await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
            Assert.NotNull(responseMesssage);
            Assert.Equal(1, responseMesssage.Id);
            Assert.Equal("Cool Message 1", responseMesssage.Content);
        }

        [Fact]
        public async Task GetMessage_ReturnsNotFound()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/message/999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetAllMessages_ReturnsSuccessAndMessages()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/message");

            response.EnsureSuccessStatusCode();
            Assert.NotNull(response.Content);
            var responseMessages = JsonSerializer.Deserialize<IEnumerable<Message>>(
                await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
            Assert.NotNull(responseMessages);
            Assert.Equal(2, responseMessages.Count());
            Assert.Contains(responseMessages, student => student.Id == 1);
            Assert.Contains(responseMessages, student => student.Id == 2);
        }

        [Fact]
        public async Task CreateMessage_ReturnsSuccessNewMessageAndLocationHeader()
        {
            var client = _factory.CreateClient();
            var message = new Message
            {
                Id = 3,
                Content = "Awesome message 3",
            };
            var content = new StringContent(JsonSerializer.Serialize(message, 
                new JsonSerializerOptions{IgnoreNullValues = true}), Encoding.UTF8, "application/json");

            try
            {
                // Act
                var response = await client.PostAsync("/message", content);

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.NotNull(response.Content);
                var responseMessage = JsonSerializer.Deserialize<Message>(
                    await response.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
                Assert.NotNull(responseMessage);
                Assert.Equal(message.Id, responseMessage.Id);
                Assert.Equal(message.Content, responseMessage.Content);
                Assert.Equal(new Uri(client.BaseAddress, "/Message/3"), response.Headers.Location);
            }
            finally
            {
                // Clean-up (so it doesn't mess up other tests)
                var dbContext = _factory.Services.GetRequiredService<MessageDbContext>();
                var student = await dbContext.Messages.FindAsync(message.Id);
                dbContext.Messages.Remove(student);
                await dbContext.SaveChangesAsync();
            }
        }

        [Theory]
        [InlineData(1, "Conflicting", HttpStatusCode.Conflict)]  // Id already exists
        [InlineData(3, null, HttpStatusCode.BadRequest)]      // missing (null) Name
        [InlineData(3, "", HttpStatusCode.BadRequest)]        // missing (empty) Name
        public async Task CreateMessage_ReturnsErrorCode(int id, string messageContent, HttpStatusCode expectedStatusCode)
        {
            var client = _factory.CreateClient();
            var message = new Message
            {
                Id = id,
                Content = messageContent,
            };
            var requestContent = new StringContent(JsonSerializer.Serialize(message, 
                new JsonSerializerOptions{IgnoreNullValues = true}), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/message", requestContent);

            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        [Fact]
        public async Task UpdateMessage_ReturnsSuccess()
        {
            var client = _factory.CreateClient();
            var message = new Message
            {
                Id = 2,
                Content = "Message changed",
            };
            var content = new StringContent(JsonSerializer.Serialize(message, 
                new JsonSerializerOptions{IgnoreNullValues = true}), Encoding.UTF8, "application/json");

            var response = await client.PutAsync("/message/2", content);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Theory]
        [InlineData(2, 999, "Jaine Dough", HttpStatusCode.BadRequest)] // url and dto Id's don't match
        [InlineData(2, 2, null, HttpStatusCode.BadRequest)]           // missing (null) Content
        [InlineData(2, 2, "",  HttpStatusCode.BadRequest)]             // missing (empty) Content
        [InlineData(999, 999, "Jaine Dough", HttpStatusCode.NotFound)] // Message not found
        public async Task UpdateMessage_ReturnsErrorCode(int urlId, int dtoId, string messageContent, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var client = _factory.CreateClient();
            var message = new Message
            {
                Id = dtoId,
                Content = messageContent,
            };
            var requestContent = new StringContent(JsonSerializer.Serialize(message, 
                new JsonSerializerOptions{IgnoreNullValues = true}), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PutAsync($"/message/{urlId}", requestContent);

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);

        }

        [Fact]
        public async Task DeleteMessage_ReturnsSuccessAndMessage()
        {
            // Arrange
            var client = _factory.CreateClient();
            var dbContext = _factory.Services.GetRequiredService<MessageDbContext>();
            var student = new Message
            {
                Id = 4,
                Content = "Another message",
            };
            dbContext.Messages.Add(student);
            await dbContext.SaveChangesAsync();

            // Act
            var response = await client.DeleteAsync("/message/4");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(response.Content);
            var responseMessage = JsonSerializer.Deserialize<Message>(
                await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
            Assert.NotNull(responseMessage);
            Assert.Equal(student.Id, responseMessage.Id);
            Assert.Equal(student.Content, responseMessage.Content);
        }

        [Fact]
        public async Task DeleteMessage_ReturnsNotFound()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.DeleteAsync("/message/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}