using Oxide.Plugins;
using static Oxide.Plugins.KanaChat;

namespace KanaChatUnitTest
{
    public class IncludedEmojisUnitTests
    {
        // When empty string, return an empty list.
        [Fact]
        public void IncludedEmojis_WhenEmptyStringReturnAnEmptyList()
        {
            var str = "";
            var expected = new List<WordPosition>();
            var actual = KanaChat.IncludedEmojisWrapper(str);

            Assert.Equal(expected, actual);
        }

        // When the string does not contain any emoji, return an empty list.
        [Fact]
        public void IncludedEmojis_WhenTheStringDoesNotContainAnyEmojiReturnAnEmptyList()
        {
            var str = "emoji ha arimasen";
            var expected = new List<WordPosition>();
            var actual = KanaChat.IncludedEmojisWrapper(str);

            Assert.Equal(expected, actual);
        }

        // When the string contains more than one emoji, it return the position of all the emoji.
        [Fact]
        public void IncludedEmojis_WhenTheStringContainsMoreThanOneEmojiItReturnThePositionOfAllTheEmoji()
        {
            var str = ":ammo.rifle: motteru:happy: demo hp ga hikuiyo :smilecry:";
            var expected = new List<WordPosition>() { new WordPosition { EndIndex = 12, StartIndex = 0, Word = ":ammo.rifle:" }, new WordPosition { EndIndex = 27, StartIndex = 20, Word = ":happy:" }, new WordPosition { EndIndex = 57, StartIndex = 47, Word = ":smilecry:" } };
            var actual = KanaChat.IncludedEmojisWrapper(str);

            Assert.Equivalent(expected, actual);
        }
    }
}

