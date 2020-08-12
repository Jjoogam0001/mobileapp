using System;
using FluentAssertions;
using Toggl.Networking.Sync.Push;
using Toggl.Networking.Serialization;
using Xunit;
using Toggl.Shared.Models;

namespace Toggl.Networking.Tests.Sync.Push
{
    public sealed class UpdateActionResultTests
    {
        string validSuccessJson = @"
        {
            ""type"": ""update"",
            ""meta"": {""id"": ""123""},
            ""payload"": {
                ""success"": true
            }
        }";

        string validErrorJson = @"
        {
            ""type"": ""update"",
            ""meta"": {""id"": 123},
            ""payload"": {
                ""success"": false,
                ""result"": {
                    ""error_message"": {
                        ""code"": 42,
                        ""default_message"": ""Cannot update the tag.""
                    }
                }
            }
        }";

        public static object[][] InvalidJsons = {
            new[] {
                @"
                {
                    ""type"": ""update"",
                    ""payload"": {
                        ""success"": true
                    }
                }",
            },
            new[] {
                @"
                {
                    ""type"": ""update"",
                    ""meta"": {""id"": 123},
                    ""payload"": { ""success"": false }
                }",
            },
            new[] {
                @"
                {
                    ""type"": ""update"",
                    ""meta"": {""id"": 123},
                    ""payload"": {
                        ""success"": false,
                        ""result"": {
                            ""code"": 42,
                            ""default_message"": ""Cannot update the tag.""
                        }
                    }
                }",
            },
        };

        [Fact, LogIfTooSlow]
        public void CanDeserializeValidSuccessActionResult()
        {
            var serializer = new JsonSerializer();

            var result = serializer.Deserialize<IEntityActionResult<ITag>>(validSuccessJson);

            result.Should().NotBeNull();
            result.Should().BeOfType<UpdateActionResult<ITag>>();
            result.Id.Should().Be(123);

            result.Result.Success.Should().BeTrue();
        }

        [Fact, LogIfTooSlow]
        public void CanDeserializeValidErrorActionResult()
        {
            var serializer = new JsonSerializer();

            var result = serializer.Deserialize<IEntityActionResult<ITag>>(validErrorJson);

            result.Should().NotBeNull();
            result.Should().BeOfType<UpdateActionResult<ITag>>();
            result.Id.Should().Be(123);

            result.Result.Should().BeOfType<ErrorResult<ITag>>();
            var error = result.Result as ErrorResult<ITag>;

            error.ErrorMessage.Code.Should().Be(42);
            error.ErrorMessage.DefaultMessage.Should().Be("Cannot update the tag.");
        }

        [Theory, LogIfTooSlow]
        [MemberData(nameof(InvalidJsons))]
        public void ThrowsForInvalidResults(string invalidJson)
        {
            var serializer = new JsonSerializer();

            Action deserialization = () => serializer.Deserialize<IEntityActionResult<ITag>>(invalidJson);

            deserialization.Should().Throw<DeserializationException>();
        }
    }
}
