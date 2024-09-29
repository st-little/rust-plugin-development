using Oxide.Plugins;
using static Oxide.Plugins.KanaChat;

namespace KanaChatUnitTest
{
    public class IncludedIgnoreWordsUnitTests
    {
        // When the string does not contain any ignored words, return an empty array.
        [Fact]
        public void IncludedIgnoreWords_WhenTheStringDoesNotContainAnyIgnoredWords_ReturnAnEmptyArray()
        {
            var ignoreWords = new List<string> { "gg", "rig" };
            var str = "";
            var expected = new List<IgnoreWord>();
            var actual = KanaChat.IncludedIgnoreWordsWrapper(str, ignoreWords);

            Assert.Equal(expected, actual);
        }

        // When the string does not contain any ignored words, return the string as is.
        [Fact]
        public void IncludedIgnoreWords_WhenTheStringDoesNotContainAnyIgnoredWordsReturnTheStringAsIs()
        {
            var ignoreWords = new List<string> { "gg", "rig" };
            var str = "ignoreWords ha arimasen";
            var expected = new List<IgnoreWord>();
            var actual = KanaChat.IncludedIgnoreWordsWrapper(str, ignoreWords);

            Assert.Equal(expected, actual);
        }

        // When the string contains ignored words, return the position of the ignored words.
        [Fact]
        public void IncludedIgnoreWords_WhenTheStringContainsIgnoredWords_ReturnThePositionOfTheIgnoredWords()
        {
            var ignoreWords = new List<string> { "GG", "rig" };
            var str = "rig yattemasuka? gg";
            var expected = new List<IgnoreWord>() { new IgnoreWord { EndIndex = 3, StartIndex = 0, Word = "rig" }, new IgnoreWord { EndIndex = 19, StartIndex = 17, Word = "GG" } };
            var actual = KanaChat.IncludedIgnoreWordsWrapper(str, ignoreWords);

            Assert.Equivalent(expected, actual);
        }
    }
}

