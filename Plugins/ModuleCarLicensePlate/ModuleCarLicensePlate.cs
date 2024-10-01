using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("Module Car License Plate", "st-little", "0.1.0")]
    [Description("This plugin spawns a module car with a license plate.")]
    public class ModuleCarLicensePlate : RustPlugin
    {
        private const string SmallWoodSignPrefab = "assets/prefabs/deployable/signs/sign.small.wood.prefab";
        private const string TwoModuleCarPrefab = "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab";
        private const string ThreeModuleCarPrefab = "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab";
        private const string FourModuleCarPrefab = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab";

        #region Configuration

        private Configuration _configuration;

        private class Configuration
        {
            public bool EnableTwoModuleCar;
            public bool EnableThreeModuleCar;
            public bool EnableFourModuleCar;

            public float TwoModuleCarPositionX;
            public float TwoModuleCarPositionY;
            public float TwoModuleCarPositionZ;

            public float ThreeModuleCarPositionX;
            public float ThreeModuleCarPositionY;
            public float ThreeModuleCarPositionZ;

            public float FourModuleCarPositionX;
            public float FourModuleCarPositionY;
            public float FourModuleCarPositionZ;
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                EnableTwoModuleCar = true,
                EnableThreeModuleCar = true,
                EnableFourModuleCar = true,

                TwoModuleCarPositionX = 0f,
                TwoModuleCarPositionY = 0.25f,
                TwoModuleCarPositionZ = -1.80f,

                ThreeModuleCarPositionX = 0f,
                ThreeModuleCarPositionY = 0.25f,
                ThreeModuleCarPositionZ = -2.55f,

                FourModuleCarPositionX = 0f,
                FourModuleCarPositionY = 0.25f,
                FourModuleCarPositionZ = -3.30f,
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

        #region Data Files

        private class StoredData
        {
            // Cars with added license plate.
            public List<ulong> addedLicensePlateCars = new List<ulong>();

            public StoredData()
            {
            }
        }

        private const string DataFileName = "ModuleCarLicensePlate";
        private StoredData storedData;

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            storedData = Core.Interface.Oxide.DataFileSystem.ReadObject<StoredData>(DataFileName);
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            switch (entity.PrefabName)
            {
                case TwoModuleCarPrefab:
                    if (_configuration.EnableTwoModuleCar)
                    {
                        AddLicensePlate(entity as BaseVehicle, new Vector3(_configuration.TwoModuleCarPositionX, _configuration.TwoModuleCarPositionY, _configuration.TwoModuleCarPositionZ));
                    }
                    break;
                case ThreeModuleCarPrefab:
                    if (_configuration.EnableThreeModuleCar)
                    {
                        AddLicensePlate(entity as BaseVehicle, new Vector3(_configuration.ThreeModuleCarPositionX, _configuration.ThreeModuleCarPositionY, _configuration.ThreeModuleCarPositionZ));

                    }
                    break;
                case FourModuleCarPrefab:
                    if (_configuration.EnableFourModuleCar)
                    {
                        AddLicensePlate(entity as BaseVehicle, new Vector3(_configuration.FourModuleCarPositionX, _configuration.FourModuleCarPositionY, _configuration.FourModuleCarPositionZ));

                    }
                    break;
            }
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (!IsModuleCar(entity)) return;
            if (!storedData.addedLicensePlateCars.Contains(entity.net.ID.Value)) return;

            storedData.addedLicensePlateCars.Remove(entity.net.ID.Value);
            Core.Interface.Oxide.DataFileSystem.WriteObject(DataFileName, storedData);
        }

        #endregion

        private void AddLicensePlate(BaseVehicle vehicle, Vector3 localPosition)
        {
            if (storedData.addedLicensePlateCars.Contains(vehicle.net.ID.Value)) return;

            var licensePlate = GameManager.server.CreateEntity(SmallWoodSignPrefab, vehicle.transform.position) as Signage;
            if (licensePlate == null) return;

            licensePlate.SetFlag(BaseEntity.Flags.Reserved8, true);
            licensePlate.SetParent(vehicle);
            licensePlate.pickup.enabled = false;
            licensePlate.transform.localPosition = localPosition;
            Vector3 licensePlateAngles = licensePlate.transform.localEulerAngles;
            licensePlateAngles.x = 180.0f;
            licensePlateAngles.z = 180.0f;
            licensePlate.transform.localEulerAngles = licensePlateAngles;

            RemoveColliderProtection(licensePlate);

            licensePlate.Spawn();
            licensePlate.SendNetworkUpdateImmediate(true);

            storedData.addedLicensePlateCars.Add(vehicle.net.ID.Value);
            Core.Interface.Oxide.DataFileSystem.WriteObject(DataFileName, storedData);
        }

        private static bool IsModuleCar(BaseNetworkable entity)
        {
            return entity.PrefabName == TwoModuleCarPrefab || entity.PrefabName == ThreeModuleCarPrefab || entity.PrefabName == FourModuleCarPrefab;
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
