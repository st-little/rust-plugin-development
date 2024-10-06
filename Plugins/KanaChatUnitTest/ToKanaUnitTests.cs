using Oxide.Plugins;
using static Oxide.Plugins.KanaChat;

namespace KanaChatUnitTest
{
    public class ToKanaUnitTests
    {
        // When empty, return an empty character.
        [Fact]
        public void ToKana_WhenEmptyReturnAnEmptyCharacter()
        {
            var str = "";
            var expected = "";
            var actual = KanaChat.ToKanaWrapper(str);

            Assert.Equal(expected, actual);
        }

        // When the string is space-delimited, return space-delimited kana.
        [Fact]
        public void ToKana_WhenTheStringIsSpaceDelimitedReturnSpaceDelimitedKana()
        {
            var str = "okanega nakute TC ni touroku dekinai;";
            var expected = "おかねが なくて tc に とうろく できない;";
            var actual = KanaChat.ToKanaWrapper(str);

            Assert.Equal(expected, actual);
        }

        // When a string contains roman characters that cannot be converted to katakana, return those roman characters without conversion.
        [Fact]
        public void ToKana_WhenAStringContainsRomanCharactersThatCannotBeConvertedToKatakanaReturnThoseRomanCharactersWithoutConversion()
        {
            var str = "okaneganakuteTCnitourokudekinai;";
            var expected = "おかねがなくてtcにとうろくできない;";
            var actual = KanaChat.ToKanaWrapper(str);

            Assert.Equal(expected, actual);
        }

        // When a string contains roman characters that cannot be converted to kana, return those roman characters without conversion.
        [Fact]
        public void ToKana_bug4()
        {
            var str = "okaneganakutetcnitourokudekinai;";
            var expected = "おかねがなくてtcにとうろくできない;";
            var actual = KanaChat.ToKanaWrapper(str);

            Assert.Equal(expected, actual);
        }

        // When a string contains roman characters that can be converted to katakana, return those roman characters converted to katakana.
        [Fact]
        public void ToKana_WhenAStringContainsRomanCharactersThatCanBeConvertedToKatakanaReturnThoseRomanCharactersConvertedToKatakana()
        {
            var str = "okaneganakuteTEXISIXInitourokudekinai;";
            var expected = "おかねがなくてティシィにとうろくできない;";
            var actual = KanaChat.ToKanaWrapper(str);

            Assert.Equal(expected, actual);
        }
    }
}

