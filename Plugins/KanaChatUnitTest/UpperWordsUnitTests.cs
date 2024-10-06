using Oxide.Plugins;
using static Oxide.Plugins.KanaChat;

namespace KanaChatUnitTest
{
    public class UpperWordsUnitTests
    {
        // When the string contains multiple uppercase letters, return the first and last position of each word.
        [Fact]
        public void UppercaseWords_WhenTheStringContainsMultipleUppercaseLettersReturnTheFirstAndLastPositionOfEachWord()
        {
            string input = "ThisIsASampleSTRINGWithCAPS";
            var expected = new List<(int start, int end)>
            {
                (0, 0),
                (4, 4),
                (6, 7),
                (13, 19),
                (23, 26)
            };

            var result = KanaChat.UppercaseWordsWrapper(input);

            Assert.Equal(expected, result);
        }

        // When the string has no uppercase letters, return an empty list.
        [Fact]
        public void UppercaseWords_WhenTheStringHasNoUppercaseLettersReturnAnEmptyList()
        {
            string input = "helloworld";
            var expected = new List<(int start, int end)>();

            var result = KanaChat.UppercaseWordsWrapper(input);

            Assert.Equal(expected, result);
        }

        // When the string is all uppercase, return the first and last positions of the string.
        [Fact]
        public void UppercaseWords_WhenTheStringIsAllUppercaseReturnTheFirstAndLastPositionsOfTheString()
        {
            string input = "HELLO";
            var expected = new List<(int start, int end)>
            {
                (0, 4)
            };

            var result = KanaChat.UppercaseWordsWrapper(input);

            Assert.Equal(expected, result);
        }
    }
}

