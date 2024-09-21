//Reference: WanaKanaShaapu
using System;
using System.Collections.Generic;
using ConVar;
using Newtonsoft.Json;
using WanaKanaShaapu;

namespace Oxide.Plugins
{
    [Info("Kana Chat", "st-little", "0.1.0")]
    [Description("Convert romaji typed in chat to kana.")]
    public class KanaChat : RustPlugin
    {
        private const string Permission = "kanachat.allow";

        #region Configuration

        private Configuration _configuration;

        private class Configuration
        {
            public List<string> ProhibitedWords;
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                ProhibitedWords = new List<string>()
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

            Unsubscribe(nameof(OnPlayerChat));
        }

        private void OnPlayerConnected(BasePlayer instance)
        {
            if (permission.UserHasPermission(instance.UserIDString, Permission))
            {
                Subscribe(nameof(OnPlayerChat));
            }
        }

        private object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            switch (channel)
            {
                case Chat.ChatChannel.Global:
                    SendGlobalChat(ToKana(message));
                    break;
                case Chat.ChatChannel.Team:
                    SendTeamChat(player, ToKana(message));
                    break;
            }
            return false;
        }

        #endregion

        private string ToKana(string romaji)
        {
            var maskedProhibitedWords = MaskProhibitedWords(romaji, _configuration.ProhibitedWords);
            var kana = WanaKana.ToKana(maskedProhibitedWords);

            return $"{kana}({maskedProhibitedWords})";
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

        private string MaskProhibitedWords(string str, List<string> prohibitedWords)
        {
            prohibitedWords.ForEach(prohibitedWord => str = str.Replace(prohibitedWord, new string('*', prohibitedWord.Length), StringComparison.OrdinalIgnoreCase));
            return str;
        }
    }
}
