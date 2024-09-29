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
    [Info("Kana Chat", "st-little", "0.1.0")]
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

        private void OnBetterChat(Dictionary<string, object> data)
        {
            if (!permission.UserHasPermission((data["Player"] as IPlayer).Id, Permission)) return;

            data["Message"] = MakeChatMessage(data["Message"] as string, _configuration.ProhibitedWords, _configuration.IgnoreWords);
        }

        #endregion

        #region Kana conversion

        private static string MaskProhibitedWords(string str, List<string> prohibitedWords)
        {
            prohibitedWords.ForEach(prohibitedWord => str = str.Replace(prohibitedWord, new string('*', prohibitedWord.Length), StringComparison.OrdinalIgnoreCase));
            return str;
        }

        public class IgnoreWord
        {
            public string Word;
            public int StartIndex;
            public int EndIndex;
        }

        private static List<IgnoreWord> IncludedIgnoreWords(string str, List<string> ignoreWords)
        {
            var includedIgnoreWords = new List<IgnoreWord>();
            foreach (var ignoreWord in ignoreWords)
            {
                int index = str.IndexOf(ignoreWord, StringComparison.OrdinalIgnoreCase);
                {
                    while (index != -1)
                    {
                        includedIgnoreWords.Add(new IgnoreWord { Word = ignoreWord, StartIndex = index, EndIndex = index + ignoreWord.Length });
                        index = str.IndexOf(ignoreWord, index + ignoreWord.Length, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            return includedIgnoreWords.OrderBy(x => x.StartIndex).ToList();
        }

        private static string MakeChatMessage(string romaji, List<string> prohibitedWords, List<string> ignoreWords)
        {
            if (!WanaKana.IsRomaji(romaji)) return romaji;

            var maskedProhibitedWords = MaskProhibitedWords(romaji, prohibitedWords);
            var includedIgnoreWords = IncludedIgnoreWords(maskedProhibitedWords, ignoreWords);

            if (includedIgnoreWords.Count == 0) return $"{WanaKana.ToKana(maskedProhibitedWords)}({maskedProhibitedWords})";

            var message = maskedProhibitedWords;
            for (int i = 0; i < includedIgnoreWords.Count; i++)
            {
                if (i == 0)
                {
                    var kana = WanaKana.ToKana(maskedProhibitedWords.Substring(0, includedIgnoreWords[0].StartIndex));
                    var ignoreWord = maskedProhibitedWords.Substring(includedIgnoreWords[0].StartIndex, includedIgnoreWords[0].Word.Length);
                    message = $"{kana}{ignoreWord}";
                }
                else
                {
                    var kana = WanaKana.ToKana(maskedProhibitedWords.Substring(includedIgnoreWords[i - 1].EndIndex, includedIgnoreWords[i].StartIndex - includedIgnoreWords[i - 1].EndIndex));
                    var ignoreWord = maskedProhibitedWords.Substring(includedIgnoreWords[i].StartIndex, includedIgnoreWords[i].Word.Length);
                    message += $"{kana}{ignoreWord}";
                }

                if (i == includedIgnoreWords.Count - 1)
                {
                    var kana = WanaKana.ToKana(maskedProhibitedWords.Substring(includedIgnoreWords[i].EndIndex));
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

        internal protected static List<IgnoreWord> IncludedIgnoreWordsWrapper(string str, List<string> ignoreWords)
        {
            return KanaChat.IncludedIgnoreWords(str, ignoreWords);
        }

        internal protected static string MakeChatMessageWrapper(string romaji, List<string> prohibitedWords, List<string> ignoreWords)
        {
            return KanaChat.MakeChatMessage(romaji, prohibitedWords, ignoreWords);
        }

        #endregion
    }
}
