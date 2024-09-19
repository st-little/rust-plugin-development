//Reference: WanaKanaShaapu
using ConVar;
using WanaKanaShaapu;

namespace Oxide.Plugins
{
    [Info("Kana Chat", "st-little", "0.1.0")]
    [Description("Convert romaji typed in chat to kana.")]
    public class KanaChat : RustPlugin
    {
        #region Oxide hooks

        private object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            switch (channel)
            {
                case Chat.ChatChannel.Global:
                    SendMessageToGlobalChat(ToKana(message));
                    break;
                case Chat.ChatChannel.Team:
                    SendMessageToTeamChat(player, ToKana(message));
                    break;
            }
            return false;
        }

        #endregion

        private string ToKana(string message)
        {
            return $"{WanaKana.ToKana(message)}({message})";
        }

        private void SendMessageToGlobalChat(string message)
        {
            PrintToChat(message);
        }

        private void SendMessageToTeamChat(BasePlayer player, string message)
        {
            foreach (var member in player.Team.members)
            {
                BasePlayer basePlayer = RelationshipManager.FindByID(member);
                PrintToChat(basePlayer, message);
            }
        }
    }
}
