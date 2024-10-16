using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using static IOEntity;

namespace Oxide.Plugins
{
    [Info("Electrician", "st-little", "0.2.1")]
    [Description("This plugin upgrades the wire tool and streamlines the electrician's work.")]
    public class Electrician : RustPlugin
    {
        #region Fields

        private Timer? GUIRefreshTimer = null;
        private readonly Dictionary<ulong, BasePlayer> UsePowerInfoPanelPlayers = new Dictionary<ulong, BasePlayer>();

        private const string PowerInfoPermission = "electrician.powerinfo.allow";
        private const string InvisibleWirePermission = "electrician.invisiblewire.allow";

        private const string WireToolPrefab = "assets/prefabs/tools/wire/wiretool.entity.prefab";
        private const string ElectricFurnacePrefab = "assets/prefabs/deployable/playerioents/electricfurnace/electricfurnace.deployed.prefab";

        #endregion

        #region Configuration

        private Configuration _configuration;

        private class Configuration
        {
            [JsonProperty(PropertyName = "GUI refresh interval")]
            public float GUIRefreshInterval;
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                GUIRefreshInterval = 1.0f
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

        #region Setting

        private class Setting
        {
            public enum Visibility
            {
                visible,
                invisible
            }

            public ulong PlayerID;
            public Visibility WireVisibility;
            public Visibility PowerInfoPanel;

            public Setting(ulong playerID, Visibility wireVisibility = Visibility.visible, Visibility powerInfoPanel = Visibility.visible)
            {
                PlayerID = playerID;
                WireVisibility = wireVisibility;
                PowerInfoPanel = powerInfoPanel;
            }
        }

        private readonly Dictionary<ulong, Setting> PlayerSetting = new Dictionary<ulong, Setting>();

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ConsumptionAmountLabel"] = "Power Usage",
                ["ShortageAmountLabel"] = "Power Shortage",
                ["SetWireVisibility"] = "Wire visibility set to {0}.",
                ["SetPowerInfoPanelVisibility"] = "Power info visibility set to {0}.",
            }, this);

            // Japanese
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ConsumptionAmountLabel"] = "パワー 使用量",
                ["ShortageAmountLabel"] = "パワー 不足量",
                ["SetWireVisibility"] = "ワイヤーの可視性を {0} に設定しました。",
                ["SetPowerInfoPanelVisibility"] = "パワー情報の可視性を {0} に設定しました。",
            }, this, "ja");
        }

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(PowerInfoPermission, this);
            permission.RegisterPermission(InvisibleWirePermission, this);
        }

        private void OnServerInitialized()
        {
            GUIRefreshTimer = timer.Every(_configuration.GUIRefreshInterval, () =>
            {
                foreach (var player in UsePowerInfoPanelPlayers.Values)
                {
                    PowerInfoPanel.Close(player);

                    if (!permission.UserHasPermission(player.UserIDString, PowerInfoPermission))
                    {
                        UsePowerInfoPanelPlayers.Remove(player.userID);
                        return;
                    }

                    var lookEntity = GetLookEntity(player);
                    if (lookEntity == null) return;

                    var ioEntity = GetIOEntity(lookEntity);
                    if (ioEntity == null) return;
                    if (ioEntity.ioType != IOEntity.IOType.Electric && ioEntity.ioType != IOEntity.IOType.Industrial) return;

                    var consumptionAmount = ioEntity.ConsumptionAmount();

                    if (ioEntity.inputs == null || ioEntity.inputs.Length == 0) return;
                    var input = ioEntity.inputs[0];

                    var inputAmount = InputAmount(input);
                    var shortageAmount = ShortageAmount(consumptionAmount, inputAmount);

                    string consumptionAmountLabel = lang.GetMessage("ConsumptionAmountLabel", this, player.UserIDString);
                    string shortageAmountLabel = lang.GetMessage("ShortageAmountLabel", this, player.UserIDString);

                    PowerInfoPanel.Open(player, consumptionAmountLabel, consumptionAmount.ToString(), shortageAmountLabel, shortageAmount.ToString());
                }
            });
        }

        private void Unload()
        {
            GUIRefreshTimer?.Destroy();
        }

        private void OnPlayerDisconnected(BasePlayer player, string strReason)
        {
            UsePowerInfoPanelPlayers.Remove(player.userID);
        }

        private void OnActiveItemChanged(BasePlayer player, Item activeItem, Item heldEntity)
        {
            if (!permission.UserHasPermission(player.UserIDString, PowerInfoPermission))
            {
                UsePowerInfoPanelPlayers.Remove(player.userID);
                return;
            }

            NextTick(() =>
            {
                if (player == null || heldEntity == null || heldEntity.GetHeldEntity() == null) return;

                if (heldEntity.GetHeldEntity().name != WireToolPrefab)
                {
                    PowerInfoPanel.Close(player);
                    UsePowerInfoPanelPlayers.Remove(player.userID);
                    return;
                }

                UsePowerInfoPanelPlayers[player.userID] = player;
            });
        }

        object OnWireConnect(BasePlayer player, IOEntity entity1, int inputs, IOEntity entity2, int outputs)
        {
            if (!permission.UserHasPermission(player.UserIDString, InvisibleWirePermission))
            {
                UsePowerInfoPanelPlayers.Remove(player.userID);
                return null;
            }

            NextTick(() =>
            {
                var playerSetting = PlayerSetting.ContainsKey(player.userID) ? PlayerSetting[player.userID] : new Setting(player.userID);
                if (playerSetting.WireVisibility == Setting.Visibility.visible) return;

                foreach (var slot in entity1.inputs)
                {
                    slot.wireColour = WireTool.WireColour.Invisible;
                    entity1.SendNetworkUpdate();
                }
                foreach (var slot in entity1.outputs)
                {
                    slot.wireColour = WireTool.WireColour.Invisible;
                    entity1.SendNetworkUpdate();
                }

                foreach (var slot in entity2.inputs)
                {
                    slot.wireColour = WireTool.WireColour.Invisible;
                    entity2.SendNetworkUpdate();
                }
                foreach (var slot in entity2.outputs)
                {
                    slot.wireColour = WireTool.WireColour.Invisible;
                    entity2.SendNetworkUpdate();
                }
            });

            return null;
        }

        #endregion

        #region Commands

        [ChatCommand("elec.wire")]
        private void SetWireVisibilityCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, InvisibleWirePermission))
            {
                PrintToChat(player, "You don't have permission to use this command!");
                return;
            }

            if (args.Length != 1) return;

            var arg = args[0];
            if (arg != Setting.Visibility.visible.ToString() && arg != Setting.Visibility.invisible.ToString()) return;

            var playerSetting = PlayerSetting.ContainsKey(player.userID) ? PlayerSetting[player.userID] : new Setting(player.userID);
            playerSetting.WireVisibility = arg == Setting.Visibility.visible.ToString() ? Setting.Visibility.visible : Setting.Visibility.invisible;
            PlayerSetting[player.userID] = playerSetting;

            string setWireVisibilityMessage = lang.GetMessage("SetWireVisibility", this, player.UserIDString);
            PrintToChat(player, string.Format(setWireVisibilityMessage, playerSetting.WireVisibility.ToString()));
        }

        [ChatCommand("elec.powerinfo")]
        private void SetPowerInfoPanelVisibilityCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PowerInfoPermission))
            {
                PrintToChat(player, "You don't have permission to use this command!");
                return;
            }

            if (args.Length != 1) return;

            var arg = args[0];
            if (arg != Setting.Visibility.visible.ToString() && arg != Setting.Visibility.invisible.ToString()) return;

            var playerSetting = PlayerSetting.ContainsKey(player.userID) ? PlayerSetting[player.userID] : new Setting(player.userID);
            playerSetting.PowerInfoPanel = arg == Setting.Visibility.visible.ToString() ? Setting.Visibility.visible : Setting.Visibility.invisible;
            PlayerSetting[player.userID] = playerSetting;

            if (playerSetting.PowerInfoPanel == Setting.Visibility.visible)
            {
                if (player.GetHeldEntity().name == WireToolPrefab)
                {
                    UsePowerInfoPanelPlayers[player.userID] = player;
                }
            }
            else
            {
                PowerInfoPanel.Close(player);
                UsePowerInfoPanelPlayers.Remove(player.userID);
            }

            string setPowerInfoPanelVisibilityMessage = lang.GetMessage("SetPowerInfoPanelVisibility", this, player.UserIDString);
            PrintToChat(player, string.Format(setPowerInfoPanelVisibilityMessage, playerSetting.PowerInfoPanel.ToString()));
        }

        #endregion

        #region Power Info Panel

        private class PowerInfoPanel
        {
            private static readonly string BackgroundPanel_Name = "BackgroundPanelImageElement";
            private static readonly string BackgroundPanel_Color = "0.753 0.753 0.753 1";  // silver: #c0c0c0
            private static readonly string BackgroundPanel_AnchorMin = "0.6 0.4";
            private static readonly string BackgroundPanel_AnchorMax = "0.7 0.45";

            private static readonly string ConsumptionAmountLabel_Name = "ConsumptionAmountLabelTextElement";
            private static readonly string ConsumptionAmountLabel_Color = "0 0 0 1";    // black: #000000
            private static readonly TextAnchor ConsumptionAmountLabel_Align = TextAnchor.MiddleLeft;
            private static readonly int ConsumptionAmountLabel_FontSize = 14;
            private static readonly string ConsumptionAmountLabel_AnchorMin = "0.602 0.425";
            private static readonly string ConsumptionAmountLabel_AnchorMax = "0.68 0.45";

            private static readonly string ConsumptionAmountValue_Name = "ConsumptionAmountValueTextElement";
            private static readonly string ConsumptionAmountValue_Color = "0 0 0 1";    // black: #000000
            private static readonly TextAnchor ConsumptionAmountValue_Align = TextAnchor.MiddleRight;
            private static readonly int ConsumptionAmountValue_FontSize = 14;
            private static readonly string ConsumptionAmountValue_AnchorMin = "0.68 0.425";
            private static readonly string ConsumptionAmountValue_AnchorMax = "0.698 0.45";

            private static readonly string ShortageAmountLabel_Name = "ShortageAmountLabelTextElement";
            private static readonly string ShortageAmountLabel_Color = "0 0 0 1";  // black: #000000
            private static readonly TextAnchor ShortageAmountLabel_Align = TextAnchor.MiddleLeft;
            private static readonly int ShortageAmountLabel_FontSize = 14;
            private static readonly string ShortageAmountLabel_AnchorMin = "0.602 0.4";
            private static readonly string ShortageAmountLabel_AnchorMax = "0.68 0.425";

            private static readonly string ShortageAmountValue_Name = "ShortageAmountValueTextElement";
            private static readonly string ShortageAmountValue_Color = "0 0 0 1";   // black: #000000
            private static readonly TextAnchor ShortageAmountValue_Align = TextAnchor.MiddleRight;
            private static readonly int ShortageAmountValue_FontSize = 14;
            private static readonly string ShortageAmountValue_AnchorMin = "0.68 0.4";
            private static readonly string ShortageAmountValue_AnchorMax = "0.698 0.425";

            public static void Open(BasePlayer player, string consumptionAmountLabel, string consumptionAmountValue, string shortageAmountLabel, string shortageAmountValue)
            {
                var powerInfoPanel = MakePowerInfoPanel(consumptionAmountLabel, consumptionAmountValue, shortageAmountLabel, shortageAmountValue);
                CuiHelper.AddUi(player, powerInfoPanel);
            }

            public static void Close(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, BackgroundPanel_Name);
                CuiHelper.DestroyUi(player, ConsumptionAmountLabel_Name);
                CuiHelper.DestroyUi(player, ConsumptionAmountValue_Name);
                CuiHelper.DestroyUi(player, ShortageAmountLabel_Name);
                CuiHelper.DestroyUi(player, ShortageAmountValue_Name);
            }

            private static CuiElementContainer MakePowerInfoPanel(string consumptionAmountLabel, string consumptionAmountValue, string shortageAmountLabel, string shortageAmountValue)
            {
                return new CuiElementContainer
                {
                    MakeBackgroundPanel(),
                    MakeConsumptionAmountLabelTxt(consumptionAmountLabel),
                    MakeConsumptionAmountValueTxt(consumptionAmountValue),
                    MakeShortageAmountLabelTxt(shortageAmountLabel),
                    MakeShortageAmountValueTxt(shortageAmountValue)
                };
            }

            private static CuiElement MakeBackgroundPanel()
            {
                var imgComp = new CuiImageComponent
                {
                    Color = BackgroundPanel_Color
                };
                var imgRectComp = new CuiRectTransformComponent
                {
                    AnchorMin = BackgroundPanel_AnchorMin,
                    AnchorMax = BackgroundPanel_AnchorMax
                };
                var imgElem = new CuiElement
                {
                    Name = BackgroundPanel_Name,
                    Parent = "Hud",
                };
                imgElem.Components.Add(imgComp);
                imgElem.Components.Add(imgRectComp);

                return imgElem;
            }

            private static CuiElement MakeConsumptionAmountLabelTxt(string consumptionAmountLabel)
            {
                var consumptionAmountLabelTxtComp = new CuiTextComponent
                {
                    Text = consumptionAmountLabel,
                    Color = ConsumptionAmountLabel_Color,
                    Align = ConsumptionAmountLabel_Align,
                    FontSize = ConsumptionAmountLabel_FontSize
                };
                var consumptionAmountLabelRectComp = new CuiRectTransformComponent
                {
                    AnchorMin = ConsumptionAmountLabel_AnchorMin,
                    AnchorMax = ConsumptionAmountLabel_AnchorMax
                };
                var consumptionAmountLabelTxtElem = new CuiElement
                {
                    Name = ConsumptionAmountLabel_Name,
                };
                consumptionAmountLabelTxtElem.Components.Add(consumptionAmountLabelTxtComp);
                consumptionAmountLabelTxtElem.Components.Add(consumptionAmountLabelRectComp);

                return consumptionAmountLabelTxtElem;
            }

            private static CuiElement MakeConsumptionAmountValueTxt(string consumptionAmountValue)
            {
                var consumptionAmountValueTxtComp = new CuiTextComponent
                {
                    Text = consumptionAmountValue,
                    Color = ConsumptionAmountValue_Color,
                    Align = ConsumptionAmountValue_Align,
                    FontSize = ConsumptionAmountValue_FontSize
                };
                var consumptionAmountValueRectComp = new CuiRectTransformComponent
                {
                    AnchorMin = ConsumptionAmountValue_AnchorMin,
                    AnchorMax = ConsumptionAmountValue_AnchorMax
                };
                var consumptionAmountValueTxtElem = new CuiElement
                {
                    Name = ConsumptionAmountValue_Name,
                };
                consumptionAmountValueTxtElem.Components.Add(consumptionAmountValueTxtComp);
                consumptionAmountValueTxtElem.Components.Add(consumptionAmountValueRectComp);

                return consumptionAmountValueTxtElem;
            }

            private static CuiElement MakeShortageAmountLabelTxt(string shortageAmountLabel)
            {
                var shortageAmountLabelTxtComp = new CuiTextComponent
                {
                    Text = shortageAmountLabel,
                    Color = ShortageAmountLabel_Color,
                    Align = ShortageAmountLabel_Align,
                    FontSize = ShortageAmountLabel_FontSize
                };
                var shortageAmountLabelRectComp = new CuiRectTransformComponent
                {
                    AnchorMin = ShortageAmountLabel_AnchorMin,
                    AnchorMax = ShortageAmountLabel_AnchorMax
                };
                var shortageAmountLabelTxtElem = new CuiElement
                {
                    Name = ShortageAmountLabel_Name,
                };
                shortageAmountLabelTxtElem.Components.Add(shortageAmountLabelTxtComp);
                shortageAmountLabelTxtElem.Components.Add(shortageAmountLabelRectComp);

                return shortageAmountLabelTxtElem;
            }

            private static CuiElement MakeShortageAmountValueTxt(string shortageAmountValue)
            {
                var shortageAmountValueTxtComp = new CuiTextComponent
                {
                    Text = shortageAmountValue,
                    Color = ShortageAmountValue_Color,
                    Align = ShortageAmountValue_Align,
                    FontSize = ShortageAmountValue_FontSize
                };
                var shortageAmountValueRectComp = new CuiRectTransformComponent
                {
                    AnchorMin = ShortageAmountValue_AnchorMin,
                    AnchorMax = ShortageAmountValue_AnchorMax
                };
                var shortageAmountValueTxtElem = new CuiElement
                {
                    Name = ShortageAmountValue_Name,
                };
                shortageAmountValueTxtElem.Components.Add(shortageAmountValueTxtComp);
                shortageAmountValueTxtElem.Components.Add(shortageAmountValueRectComp);

                return shortageAmountValueTxtElem;
            }
        }

        #endregion

        #region Helper

        private static IOEntity? GetIOEntity(BaseEntity entity)
        {
            switch (entity.name)
            {
                case ElectricFurnacePrefab:
                    var electricOven = entity as ElectricOven;
                    if (electricOven == null) return null;

                    var ioEntity = electricOven.IoEntity;
                    if (ioEntity == null) return null;

                    return ioEntity.GetEntity() as IOEntity;
                default:
                    return entity as IOEntity;
            }
        }

        private static int InputAmount(IOSlot input)
        {
            var destinationEntity = input.connectedTo.Get();
            if (destinationEntity == null) return 0;

            return destinationEntity.GetPassthroughAmount(input.connectedToSlot);
        }

        private static int ShortageAmount(int consumptionAmount, int inputAmount)
        {
            return Mathf.Clamp(consumptionAmount - inputAmount, 0, consumptionAmount);
        }

        private static BaseEntity? GetLookEntity(BasePlayer player, float maxDistance = 3)
        {
            return Physics.Raycast(player.eyes.HeadRay(), out var hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                ? hit.GetEntity()
                : null;
        }

        #endregion
    }
}

