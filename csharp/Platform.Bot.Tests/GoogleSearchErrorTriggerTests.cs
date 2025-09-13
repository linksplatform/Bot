using System.Threading.Tasks;
using Moq;
using Platform.Bot.Triggers;
using Storage.Remote.GitHub;
using Xunit;

namespace Platform.Bot.Tests
{
    /// <summary>
    /// <para>
    /// Tests for the GoogleSearchErrorTrigger class.
    /// </para>
    /// <para></para>
    /// </summary>
    public class GoogleSearchErrorTriggerTests
    {
        /// <summary>
        /// <para>
        /// Tests that the trigger can be instantiated without errors.
        /// </para>
        /// <para></para>
        /// </summary>
        [Fact]
        public void Constructor_WithValidStorage_CreatesInstance()
        {
            // Arrange
            var mockStorage = new Mock<GitHubStorage>("testuser", "testtoken", "testapp");

            // Act
            var trigger = new GoogleSearchErrorTrigger(mockStorage.Object);

            // Assert
            Assert.NotNull(trigger);
        }

        /// <summary>
        /// <para>
        /// Tests that the class has the expected public methods.
        /// </para>
        /// <para></para>
        /// </summary>
        [Fact]
        public void PublicMethods_AreAvailable()
        {
            // Arrange
            var mockStorage = new Mock<GitHubStorage>("testuser", "testtoken", "testapp");
            var trigger = new GoogleSearchErrorTrigger(mockStorage.Object);

            // Act & Assert - Just verify methods exist and can be called
            Assert.NotNull(trigger.GetType().GetMethod("Condition"));
            Assert.NotNull(trigger.GetType().GetMethod("Action"));
            Assert.NotNull(trigger.GetType().GetMethod("Dispose"));
        }
    }
}