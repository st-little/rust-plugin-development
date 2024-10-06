//Reference: WanaKanaShaapu
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using ConVar;
using Newtonsoft.Json;
using WanaKanaShaapu;

namespace Oxide.Plugins
{
    [Info("Kana Chat", "st-little", "0.1.2")]
    [Description("Convert romaji typed in chat to kana.")]
    public class KanaChat : RustPlugin
    {
        #region Fields

        private const string Permission = "kanachat.allow";

        #endregion

        #region Configuration

        private Configuration _configuration;

        private class Configuration
        {
            public List<string> ProhibitedWords;
            public List<string> IgnoreWords;
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                ProhibitedWords = new List<string>(),
                IgnoreWords = new List<string>(),
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _configuration = Config.ReadObject<Configuration>();

                if (_configuration == null)
                    LoadDefaultConfig();
            }
            catch
            {
                PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _configuration = GetDefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(_configuration);

        #endregion

        #region Oxide hooks

        void Init()
        {
            permission.RegisterPermission(Permission, this);
        }

        #endregion

        #region Better Chat hooks

        /// <summary>
        /// OnBetterChat is called when Better Chat is trying to send a chat message.
        /// </summary>
        /// <param name="data">dataDictionary<string, object></param>
        /// <returns></returns>
        /// <see href="https://umod.org/plugins/better-chat#for-developers-api">For Developers (API)</see>
        private void OnBetterChat(Dictionary<string, object> data)
        {
            if (!permission.UserHasPermission((data["Player"] as IPlayer).Id, Permission)) return;

            data["Message"] = MakeChatMessage(data["Message"] as string, _configuration.ProhibitedWords, _configuration.IgnoreWords);
        }

        #endregion

        #region Kana conversion

        /// <summary>
        /// Masks prohibited words in the string.
        /// </summary>
        /// <param name="str">String</param>
        /// <param name="prohibitedWords">Prohibited words</param>
        /// <returns>String with masked prohibited characters.</returns>
        private static string MaskProhibitedWords(string str, List<string> prohibitedWords)
        {
            prohibitedWords.ForEach(prohibitedWord => str = str.Replace(prohibitedWord, new string('*', prohibitedWord.Length), StringComparison.OrdinalIgnoreCase));
            return str;
        }

        public class WordPosition
        {
            public string Word;
            public int StartIndex;
            public int EndIndex;
        }

        /// <summary>
        /// Returns the position of the ignored words in the string.
        /// </summary>
        /// <param name="str">String</param>
        /// <param name="ignoreWords">Ignored words</param>
        /// <returns>The position of the ignored words in the string.</returns>
        private static List<WordPosition> IncludedIgnoreWords(string str, List<string> ignoreWords)
        {
            var includedIgnoreWords = new List<WordPosition>();
            foreach (var ignoreWord in ignoreWords)
            {
                int index = str.IndexOf(ignoreWord, StringComparison.OrdinalIgnoreCase);
                {
                    while (index != -1)
                    {
                        includedIgnoreWords.Add(new WordPosition { Word = ignoreWord, StartIndex = index, EndIndex = index + ignoreWord.Length });
                        index = str.IndexOf(ignoreWord, index + ignoreWord.Length, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            return includedIgnoreWords.OrderBy(x => x.StartIndex).ToList();
        }

        /// <summary>
        /// Returns the position of the emoji in the string.
        /// </summary>
        /// <param name="str">String</param>
        /// <returns>The positions of the emoji in the string.</returns>
        private static List<WordPosition> IncludedEmojis(string str)
        {
            const char delimiter = ':';
            var includedEmojis = new List<WordPosition>();
            int delimiterIndex = str.IndexOf(delimiter);
            int? startIndex = null;

            while (delimiterIndex != -1)
            {
                if (startIndex == null)
                {
                    startIndex = delimiterIndex;
                }
                else
                {
                    includedEmojis.Add(new WordPosition { Word = str.Substring(startIndex.Value, delimiterIndex - startIndex.Value + 1), StartIndex = startIndex.Value, EndIndex = delimiterIndex + 1 });
                    startIndex = null;
                }
                delimiterIndex = str.IndexOf(delimiter, delimiterIndex + 1);
            }

            return includedEmojis.OrderBy(x => x.StartIndex).ToList();
        }

        /// <summary>
        /// Returns the position of the uppercase words in the string.
        /// </summary>
        /// <param name="str">String</param>
        /// <returns>The position of the uppercase words in the string.</returns>
        private static List<(int start, int end)> UppercaseWords(string str)
        {
            var uppercaseWordRanges = new List<(int start, int end)>();

            int? startIndex = null;

            for (int i = 0; i < str.Length; i++)
            {
                if (char.IsUpper(str[i]))
                {
                    // Record the starting point of consecutive uppercase letters
                    if (startIndex == null)
                    {
                        startIndex = i;
                    }
                }
                else if (startIndex != null)
                {
                    // When a group of uppercase letters ends, add the range to the list
                    uppercaseWordRanges.Add((startIndex.Value, i - 1));
                    startIndex = null;
                }
            }

            // Add a range of uppercase letters ending in a sequence of uppercase letters at the end
            if (startIndex != null)
            {
                uppercaseWordRanges.Add((startIndex.Value, str.Length - 1));
            }

            return uppercaseWordRanges;
        }

        /// <summary>
        /// Converts romaji to kana.
        /// </summary>
        /// <param name="romaji">romaji string</param>
        /// <returns>String converted to kana.</returns>
        private static string ToKana(string romaji)
        {
            var uppercaseWords = UppercaseWords(romaji);

            if (uppercaseWords.Count == 0) return WanaKana.ToKana(romaji);

            var message = romaji;
            for (int i = 0; i < uppercaseWords.Count; i++)
            {
                if (i == 0)
                {
                    var kana = WanaKana.ToKana(romaji.Substring(0, uppercaseWords[0].start));
                    var katakana = WanaKana.ToKana(romaji.Substring(uppercaseWords[0].start, uppercaseWords[0].end - uppercaseWords[0].start + 1));
                    message = $"{kana}{katakana}";
                }
                else
                {
                    var kana = WanaKana.ToKana(romaji.Substring(uppercaseWords[i - 1].end + 1, uppercaseWords[i].start - uppercaseWords[i - 1].end - 1));
                    var katakana = WanaKana.ToKana(romaji.Substring(uppercaseWords[i].start, uppercaseWords[i].end - uppercaseWords[i].start + 1));
                    message += $"{kana}{katakana}";
                }

                if (i == uppercaseWords.Count - 1)
                {
                    var kana = WanaKana.ToKana(romaji.Substring(uppercaseWords[i].end + 1));
                    message += kana;
                }
            }
            return message;
        }

        /// <summary>
        /// Converts chats entered in romaji to kana. WanaKanaShaapu is used for the conversion.
        /// </summary>
        /// <param name="romaji">Chat messages typed in romaji.</param>
        /// <param name="prohibitedWords">Prohibited words</param>
        /// <param name="ignoreWords">Ignored words</param>
        /// <returns>Chet message converted to Kana.</returns>
        /// <see href="https://github.com/kmoroz/WanaKanaShaapu">WanaKanaShaapu</see>
        private static string MakeChatMessage(string romaji, List<string> prohibitedWords, List<string> ignoreWords)
        {
            if (!WanaKana.IsRomaji(romaji)) return romaji;

            var maskedProhibitedWords = MaskProhibitedWords(romaji, prohibitedWords);
            var includedIgnoreWords = IncludedIgnoreWords(maskedProhibitedWords, ignoreWords);
            var includedEmojis = IncludedEmojis(maskedProhibitedWords);

            includedIgnoreWords.AddRange(includedEmojis);
            includedIgnoreWords = includedIgnoreWords.OrderBy(x => x.StartIndex).ToList();

            if (includedIgnoreWords.Count == 0) return $"{ToKana(maskedProhibitedWords)}({maskedProhibitedWords})";

            var message = maskedProhibitedWords;
            for (int i = 0; i < includedIgnoreWords.Count; i++)
            {
                if (i == 0)
                {
                    var kana = ToKana(maskedProhibitedWords[0..includedIgnoreWords[0].StartIndex]);
                    var ignoreWord = maskedProhibitedWords[includedIgnoreWords[0].StartIndex..includedIgnoreWords[0].EndIndex];
                    message = $"{kana}{ignoreWord}";
                }
                else
                {
                    var kana = ToKana(maskedProhibitedWords[includedIgnoreWords[i - 1].EndIndex..includedIgnoreWords[i].StartIndex]);
                    var ignoreWord = maskedProhibitedWords[includedIgnoreWords[i].StartIndex..includedIgnoreWords[i].EndIndex];
                    message += $"{kana}{ignoreWord}";
                }

                if (i == includedIgnoreWords.Count - 1)
                {
                    var kana = ToKana(maskedProhibitedWords[includedIgnoreWords[i].EndIndex..]);
                    message += kana;
                }
            }
            return $"{message}({maskedProhibitedWords})";
        }

        #endregion

        #region Test Methods

        internal protected static string MaskProhibitedWordsWrapper(string str, List<string> prohibitedWords)
        {
            return KanaChat.MaskProhibitedWords(str, prohibitedWords);
        }

        internal protected static List<WordPosition> IncludedIgnoreWordsWrapper(string str, List<string> ignoreWords)
        {
            return KanaChat.IncludedIgnoreWords(str, ignoreWords);
        }

        internal protected static List<WordPosition> IncludedEmojisWrapper(string str)
        {
            return KanaChat.IncludedEmojis(str);
        }

        internal protected static List<(int start, int end)> UppercaseWordsWrapper(string input)
        {
            return KanaChat.UppercaseWords(input);
        }

        internal protected static string ToKanaWrapper(string romaji)
        {
            return KanaChat.ToKana(romaji);
        }

        internal protected static string MakeChatMessageWrapper(string romaji, List<string> prohibitedWords, List<string> ignoreWords)
        {
            return KanaChat.MakeChatMessage(romaji, prohibitedWords, ignoreWords);
        }

        #endregion
    }
}
