using Oxide.Plugins;

namespace KanaChatUnitTest
{
    public class MaskProhibitedWordsUnitTests
    {
        // When an empty string is passed, return an empty string.
        [Fact]
        public void MaskProhibitedWords_WhenAnEmptyStringIsPassed_ReturnAnEmptyString()
        {
            var prohibitedWords = new List<string> { "pw1", "pw2" };
            var str = "";
            var expected = "";
            var actual = KanaChat.MaskProhibitedWordsWrapper(str, prohibitedWords);

            Assert.Equal(expected, actual);
        }

        // When the prohibited words is not present, return without masking.
        [Fact]
        public void MaskProhibitedWords_WhenTheProhibitedWordsIsNotPresentReturnWithoutMasking()
        {
            var prohibitedWords = new List<string> { };
            var str = "prohibitedWord ha arimasen";
            var expected = "prohibitedWord ha arimasen";
            var actual = KanaChat.MaskProhibitedWordsWrapper(str, prohibitedWords);

            Assert.Equal(expected, actual);
        }

        // When a string is passed that does not contain a prohibited word, return it without masking.
        [Fact]
        public void MaskProhibitedWords_WhenAStringIsPassedThatDoesNotContainAProhibitedWordReturnItWithoutMasking()
        {
            var prohibitedWords = new List<string> { "pw1", "pw2" };
            var str = "prohibitedWord ha arimasen";
            var expected = "prohibitedWord ha arimasen";
            var actual = KanaChat.MaskProhibitedWordsWrapper(str, prohibitedWords);

            Assert.Equal(expected, actual);
        }

        // When a string containing a prohibited word is passed, return it with the prohibited word masked.
        [Fact]
        public void MaskProhibitedWords_WhenAStringContainingAProhibitedWordIsPassedReturnItWithTheProhibitedWordMasked()
        {
            var prohibitedWords = new List<string> { "prohibitedWord1", "pw2" };
            var str = "prohibitedWord1 desu. 2komeno prohibitedWord1. saigo ni pw2";
            var expected = "*************** desu. 2komeno ***************. saigo ni ***";
            var actual = KanaChat.MaskProhibitedWordsWrapper(str, prohibitedWords);

            Assert.Equal(expected, actual);
        }
    }

}
