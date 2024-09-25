//Reference: WanaKanaShaapu
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

        private const string TeamPrefixColor = "#AAFF55";
        private const string PlayerNameColor = "#55AAFF";

        #endregion

        #region Configuration

        private Configuration _configuration;

        private class Configuration
        {
            public List<string> ProhibitedWords;
            public List<string> IgnoreWords;
            public bool UseBetterChat;
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                ProhibitedWords = new List<string>(),
                IgnoreWords = new List<string>(),
                UseBetterChat = false
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

        #region Prohibited words

        private string MaskProhibitedWords(string str, List<string> prohibitedWords)
        {
            prohibitedWords.ForEach(prohibitedWord => str = str.Replace(prohibitedWord, new string('*', prohibitedWord.Length), StringComparison.OrdinalIgnoreCase));
            return str;
        }

        #endregion

        #region Ignore words

        private class IgnoreWord
        {
            public string Word;
            public int StartIndex;
            public int EndIndex;
        }

        private List<IgnoreWord> IncludedIgnoreWords(string str, List<string> ignoreWords)
        {
            var includedIgnoreWords = new List<IgnoreWord>();
            foreach (var ignoreWord in ignoreWords)
            {
                var startIndex = str.IndexOf(ignoreWord, 0, str.Length, StringComparison.OrdinalIgnoreCase);
                if (0 <= startIndex) includedIgnoreWords.Add(new IgnoreWord { Word = ignoreWord, StartIndex = startIndex, EndIndex = startIndex + ignoreWord.Length });
            }
            return includedIgnoreWords.OrderBy(x => x.StartIndex).ToList();
        }

        #endregion

        #region Oxide hooks

        void Init()
        {
            permission.RegisterPermission(Permission, this);

            Unsubscribe(nameof(OnPlayerChat));
        }

        private void OnPlayerConnected(BasePlayer instance)
        {
            if (permission.UserHasPermission(instance.UserIDString, Permission) && !_configuration.UseBetterChat)
            {
                Subscribe(nameof(OnPlayerChat));
            }
        }

        private object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (_configuration.UseBetterChat) return null;

            switch (channel)
            {
                case Chat.ChatChannel.Global:
                    SendGlobalChat($"<color={PlayerNameColor}>{player.displayName}</color>: {makeChatMessage(message)}");
                    return false;
                case Chat.ChatChannel.Team:
                    SendTeamChat(player, $"<color={TeamPrefixColor}>[TEAM]</color> <color={PlayerNameColor}>{player.displayName}</color>: {makeChatMessage(message)}");
                    return false;
            }
            return null;
        }

        #endregion

        private string makeChatMessage(string romaji)
        {
            if (!WanaKana.IsRomaji(romaji)) return romaji;

            var maskedProhibitedWords = MaskProhibitedWords(romaji, _configuration.ProhibitedWords);
            var includedIgnoreWords = IncludedIgnoreWords(maskedProhibitedWords, _configuration.IgnoreWords);

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

        private void SendGlobalChat(string message)
        {
            PrintToChat(message);
        }

        private void SendTeamChat(BasePlayer player, string message)
        {
            foreach (var member in player.Team.members)
            {
                BasePlayer basePlayer = RelationshipManager.FindByID(member);
                PrintToChat(basePlayer, message);
            }
        }

        private void OnBetterChat(Dictionary<string, object> data)
        {
            if (!_configuration.UseBetterChat)
            {
                data["CancelOption"] = 2;   // cancel both Better Chat handling & default game chat
                return;
            }

            data["Message"] = makeChatMessage(data["Message"] as string);
        }
    }
}
