using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using static BaseMountable;

namespace Oxide.Plugins
{
    [Info("Where is my ride", "st-little", "0.1.0")]
    [Description("This plugin tells you the location of the last ride the player disembarked from.")]
    public class WhereIsMyRide : RustPlugin
    {
        private const string Permission = "whereismyride.allow";

        private readonly Dictionary<ulong, BaseMountable> Rides = new Dictionary<ulong, BaseMountable>();

        [PluginReference]
        Plugin MarkerAPI;

        private static WhereIsMyRide _pluginInstance;

        #region Configuration

        private Configuration _configuration;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Add a marker on the map with the location of the ride")]
            public bool AddAMarkerOnTheMapWithTheLocationOfTheRide;
            [JsonProperty(PropertyName = "Send the location of the ride to chat")]
            public bool SendTheLocationOfTheRideToChat;

            [JsonProperty(PropertyName = "Map Marker Display Name")]
            public string MapMarkerDisplayName;
            [JsonProperty(PropertyName = "Map Marker Radius")]
            public float MapMarkerRadius;
            [JsonProperty(PropertyName = "Map Marker Color")]
            public string MapMarkerColor;
            [JsonProperty(PropertyName = "Map Marker Color Outline")]
            public string MapMarkerColorOutline;

            [JsonProperty(PropertyName = "Time to display the marker on the map")]
            public int TimeToDisplayTheMarkerOnTheMap;
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                AddAMarkerOnTheMapWithTheLocationOfTheRide = true,
                SendTheLocationOfTheRideToChat = true,

                MapMarkerDisplayName = "Your ride.",
                MapMarkerRadius = 0.4f,
                MapMarkerColor = "EE82EE",
                MapMarkerColorOutline = "6A5ACD",

                TimeToDisplayTheMarkerOnTheMap = 30
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

        #region Localization

        protected override void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MiscLocation"] = "Your ride location is {0}.",
                ["BoatLocation"] = "Your boat location is {0}.",
                ["AircraftLocation"] = "Your aircraft location is {0}.",
                ["GroundVehicleLocation"] = "Your ground vehicle location is {0}.",
                ["HorseLocation"] = "Your horse location is {0}.",
                ["DoNotKnowWhereItIs"] = "Do not know where your ride is located."
            }, this);

            // Japanese
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MiscLocation"] = "あなたの乗り物は {0} にあります。",
                ["BoatLocation"] = "あなたのボートは {0} にあります。",
                ["AircraftLocation"] = "あなたの航空機は {0} にあります。",
                ["GroundVehicleLocation"] = "あなたの陸上車両は {0} にあります。",
                ["HorseLocation"] = "あなたの馬は {0} にいます。",
                ["DoNotKnowWhereItIs"] = "あなたの乗り物がどこにあるかわかりません。"
            }, this, "ja");
        }

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(Permission, this);

            _pluginInstance = this;
        }

        private void Unload()
        {
            _pluginInstance = null;
        }

        private object OnPlayerWantsDismount(BasePlayer player, BaseMountable instance)
        {
            if (!permission.UserHasPermission(player.UserIDString, Permission)) return null;
            if (instance.dismountHoldType == DismountConvarType.Misc) return null;

            Rides[player.userID] = instance;

            return null;
        }

        #endregion

        #region Commands

        [ChatCommand("where")]
        private void WhereCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, Permission))
            {
                PrintToChat(player, "You don't have permission to use this command!");
                return;
            }

            if (Rides.ContainsKey(player.userID))
            {
                var ride = Rides[player.userID];

                if (_configuration.AddAMarkerOnTheMapWithTheLocationOfTheRide)
                {
                    Marker marker = new Marker(ride.transform.position, player.userID, _configuration.TimeToDisplayTheMarkerOnTheMap, _configuration.MapMarkerDisplayName, _configuration.MapMarkerRadius, _configuration.MapMarkerColor, _configuration.MapMarkerColorOutline, 1f);
                    marker.SpawnMarker();
                }
                if (_configuration.SendTheLocationOfTheRideToChat)
                {
                    var rideLocationMessage = MakeRideLocationMessage(player, ride);
                    PrintToChat(player, rideLocationMessage);
                }
            }
            else
            {
                string doNotKnowWhereItIsMessage = lang.GetMessage("DoNotKnowWhereItIs", this, player.UserIDString);
                PrintToChat(player, doNotKnowWhereItIsMessage);
            }
        }

        #endregion

        class Marker
        {
            public Vector3 Position;
            public readonly string Uname;
            public ulong OwnerID;
            public int Duration;
            public string DisplayName;
            public float Radius;
            public string ColorMarker;
            public string ColorOutline;
            public float Refresh;

            public Marker(Vector3 position, ulong ownerId, int duration, string displayName, float radius, string colorMarker, string colorOutline, float refresh)
            {
                Position = position;
                Uname = $"whereismyride.{ownerId}";
                OwnerID = ownerId;
                Duration = duration;
                DisplayName = displayName;
                Radius = radius;
                ColorMarker = colorMarker;
                ColorOutline = colorOutline;
                Refresh = refresh;
            }

            public void SpawnMarker()
            {
                if (!_pluginInstance.MarkerAPI) return;

                bool result = CreateMarkerPrivate();
                if (!result)
                {
                    RemoveMarker();
                    CreateMarkerPrivate();
                }
            }

            private bool CreateMarkerPrivate()
            {
                return _pluginInstance.MarkerAPI.Call<bool>("API_CreateMarkerPrivate", Position, Uname, OwnerID, Duration, Refresh, Radius, DisplayName, ColorMarker, ColorOutline);
            }

            private void RemoveMarker()
            {
                _pluginInstance.MarkerAPI.Call("API_RemoveCachedMarker", Uname);
            }
        }

        private string MakeRideLocationMessage(BasePlayer player, BaseMountable ride)
        {
            string rideGridCoord = PhoneController.PositionToGridCoord(ride.transform.position);

            switch (ride.dismountHoldType)
            {
                case DismountConvarType.Boating:
                    string boatLocationMessage = lang.GetMessage("BoatLocation", this, player.UserIDString);
                    return string.Format(boatLocationMessage, rideGridCoord);
                case DismountConvarType.Flying:
                    string aircraftLocationMessage = lang.GetMessage("AircraftLocation", this, player.UserIDString);
                    return string.Format(aircraftLocationMessage, rideGridCoord);
                case DismountConvarType.GroundVehicle:
                    string groundVehicleLocationMessage = lang.GetMessage("GroundVehicleLocation", this, player.UserIDString);
                    return string.Format(groundVehicleLocationMessage, rideGridCoord);
                case DismountConvarType.Horse:
                    string horseLocationMessage = lang.GetMessage("HorseLocation", this, player.UserIDString);
                    return string.Format(horseLocationMessage, rideGridCoord);
                default:
                    string miscLocationMessage = lang.GetMessage("MiscLocation", this, player.UserIDString);
                    return string.Format(miscLocationMessage, rideGridCoord);
            }
        }
    }
}

