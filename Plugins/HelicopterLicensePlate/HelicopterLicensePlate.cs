using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Helicopter License Plate", "st-little", "0.1.0")]
    [Description("This plugin spawns a helicopter with a license plate.")]
    public class HelicopterLicensePlate : RustPlugin
    {
        private const string SmallWoodSignPrefab = "assets/prefabs/deployable/signs/sign.small.wood.prefab";
        private const string MinicopterPrefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        private const string AttackhelicopterPrefab = "assets/content/vehicles/attackhelicopter/attackhelicopter.entity.prefab";
        private const string ScraptransporthelicopterPrefab = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab";

        private bool IsServerInitialized = false;

        #region Configuration

        private Configuration _configuration;

        private class Configuration
        {
            public bool EnableMinicopter;
            public bool EnableAttackhelicopter;
            public bool EnableScraptransporthelicopter;

            public float MinicopterPositionX;
            public float MinicopterPositionY;
            public float MinicopterPositionZ;

            public float AttackhelicopterPositionX;
            public float AttackhelicopterPositionY;
            public float AttackhelicopterPositionZ;

            public float ScraptransporthelicopterPositionX;
            public float ScraptransporthelicopterPositionY;
            public float ScraptransporthelicopterPositionZ;
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                EnableMinicopter = true,
                EnableAttackhelicopter = true,
                EnableScraptransporthelicopter = true,

                MinicopterPositionX = 0f,
                MinicopterPositionY = 0.1f,
                MinicopterPositionZ = -0.8f,

                AttackhelicopterPositionX = 0f,
                AttackhelicopterPositionY = 0.4f,
                AttackhelicopterPositionZ = -1.45f,

                ScraptransporthelicopterPositionX = 0f,
                ScraptransporthelicopterPositionY = 0.3f,
                ScraptransporthelicopterPositionZ = -3.15f,
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

        #region Oxide Hooks

        private void Init()
        {
            IsServerInitialized = false;
        }
        private void OnServerInitialized()
        {
            IsServerInitialized = true;
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!IsServerInitialized) return;

            switch (entity.PrefabName)
            {
                case MinicopterPrefab:
                    if (_configuration.EnableMinicopter)
                    {
                        AddLicensePlate((BaseVehicle)entity, new Vector3(_configuration.MinicopterPositionX, _configuration.MinicopterPositionY, _configuration.MinicopterPositionZ));
                    }
                    break;
                case AttackhelicopterPrefab:
                    if (_configuration.EnableAttackhelicopter)
                    {
                        AddLicensePlate((BaseVehicle)entity, new Vector3(_configuration.AttackhelicopterPositionX, _configuration.AttackhelicopterPositionY, _configuration.AttackhelicopterPositionZ));
                    }
                    break;
                case ScraptransporthelicopterPrefab:
                    if (_configuration.EnableScraptransporthelicopter)
                    {
                        AddLicensePlate((BaseVehicle)entity, new Vector3(_configuration.ScraptransporthelicopterPositionX, _configuration.ScraptransporthelicopterPositionY, _configuration.ScraptransporthelicopterPositionZ));
                    }
                    break;
            }
        }

        #endregion

        private void AddLicensePlate(BaseVehicle mini, Vector3 localPosition)
        {
            var licensePlate = GameManager.server.CreateEntity(SmallWoodSignPrefab, mini.transform.position) as Signage;
            if (licensePlate == null) return;

            licensePlate.SetFlag(BaseEntity.Flags.Reserved8, true);
            licensePlate.SetParent(mini);
            licensePlate.pickup.enabled = false;
            licensePlate.transform.localPosition = localPosition;
            Vector3 licensePlateAngles = licensePlate.transform.localEulerAngles;
            licensePlateAngles.x = 180.0f;
            licensePlateAngles.z = 180.0f;
            licensePlate.transform.localEulerAngles = licensePlateAngles;

            RemoveColliderProtection(licensePlate);

            licensePlate.Spawn();
            licensePlate.SendNetworkUpdateImmediate(true);
        }

        private static void RemoveColliderProtection(BaseEntity colliderEntity)
        {
            foreach (var meshCollider in colliderEntity.GetComponentsInChildren<MeshCollider>())
            {
                UnityEngine.Object.DestroyImmediate(meshCollider);
            }

            UnityEngine.Object.DestroyImmediate(colliderEntity.GetComponent<GroundWatch>());
        }
    }
}
