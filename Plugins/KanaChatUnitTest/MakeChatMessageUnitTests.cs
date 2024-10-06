using Oxide.Plugins;

namespace KanaChatUnitTest
{
    public class MakeChatMessageUnitTests
    {
        // When the string contains other than romaji, return the string as it is.
        [Fact]
        public void MakeChatMessage_WhenTheStringContainsOtherThanRomaji_ReturnTheStringAsItIs()
        {
            var prohibitedWords = new List<string> { "pw1", "pw2" };
            var ignoreWords = new List<string> { "gg", "rig" };
            var str = "romaji igaino かな ga arimasu";
            var expected = "romaji igaino かな ga arimasu";

            var actual = KanaChat.MakeChatMessageWrapper(str, prohibitedWords, ignoreWords);

            Assert.Equal(expected, actual);
        }

        // When the string does not contain the ignored word, return the kana-converted string.
        [Fact]
        public void MakeChatMessage_WhenTheStringDoesNotContainTheIgnoredWord_ReturnTheKanaConvertedString()
        {
            var prohibitedWords = new List<string> { "pw1", "pw2" };
            var ignoreWords = new List<string> { "gg", "rig" };
            var str = "konbanwa-, hajimemasite!";
            var expected = "こんばんわー、 はじめまして！(konbanwa-, hajimemasite!)";

            var actual = KanaChat.MakeChatMessageWrapper(str, prohibitedWords, ignoreWords);

            Assert.Equal(expected, actual);
        }

        // When the string contains ignored words, return a kana-converted string, ignoring the ignored words.
        [Fact]
        public void MakeChatMessage_WhenTheStringContainsIgnoredWords_ReturnAKanaConvertedStringIgnoringTheIgnoredWords()
        {
            var prohibitedWords = new List<string> { "pw1", "pw2" };
            var ignoreWords = new List<string> { "gg", "rig" };
            var str = "GG, rig yatteruhito imasuka:happy:? gg";
            var expected = "GG、 rig やってるひと いますか:happy:？ gg(GG, rig yatteruhito imasuka:happy:? gg)";

            var actual = KanaChat.MakeChatMessageWrapper(str, prohibitedWords, ignoreWords);

            Assert.Equal(expected, actual);
        }
    }
}

