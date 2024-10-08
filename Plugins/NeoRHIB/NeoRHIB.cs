using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Neo RHIB", "st-little", "0.1.0")]
    [Description("This plugin spawns RHIB equipped with a searchlight, locker, and barbeque.")]
    public class NeoRHIB : RustPlugin
    {
        private const string SmallWoodSignPrefab = "assets/prefabs/deployable/signs/sign.small.wood.prefab";
        public const string LockerPrefab = "assets/prefabs/deployable/locker/locker.deployed.prefab";
        public const string BbqPrefab = "assets/bundled/prefabs/static/bbq.static.prefab";
        private const string RHIBPrefab = "assets/content/vehicles/boats/rhib/rhib.prefab";
        private const string SearchLightPrefab = "assets/prefabs/deployable/search light/searchlight.deployed.prefab";
        private bool IsServerInitialized = false;

        [PluginReference]
        Plugin EntityScaleManager;

        private static NeoRHIB _pluginInstance;

        #region Configuration

        private Configuration _configuration;

        private class Configuration
        {
            public bool AddSearchlightAboveTheCockpit;
            public bool AddBbqBetweenTheRearSeats;
            public bool AddLockerOnTheRearHatch;
            public bool AddSmallWoodSignToTheFrontOfTheCockpit;
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                AddSearchlightAboveTheCockpit = true,
                AddBbqBetweenTheRearSeats = true,
                AddLockerOnTheRearHatch = true,
                AddSmallWoodSignToTheFrontOfTheCockpit = true,
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
            _pluginInstance = this;
        }
        private void OnServerInitialized()
        {
            IsServerInitialized = true;
        }

        private void Unload()
        {
            _pluginInstance = null;
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!IsServerInitialized) return;

            switch (entity.PrefabName)
            {
                case RHIBPrefab:
                    if (_configuration.AddSearchlightAboveTheCockpit) AddSearchlightAboveTheCockpit((BaseVehicle)entity);
                    if (_configuration.AddBbqBetweenTheRearSeats) AddBbqBetweenTheRearSeats((BaseVehicle)entity);
                    if (_configuration.AddLockerOnTheRearHatch) AddLockerOnTheRearHatch((BaseVehicle)entity);
                    if (_configuration.AddSmallWoodSignToTheFrontOfTheCockpit) AddSmallWoodSignToTheFrontOfTheCockpit((BaseVehicle)entity);
                    break;
            }
        }

        // void OnEngineStarted(BaseVehicle vehicle, BasePlayer driver)
        // {
        //     if (vehicle.PrefabName != RHIBPrefab) return;

        //     foreach (var vehicleChild in vehicle.children)
        //     {
        //         if (vehicleChild.PrefabName != SpherePrefab) continue;

        //         foreach (var sphereChild in vehicleChild.children)
        //         {
        //             if (sphereChild.PrefabName != SearchLightPrefab) continue;

        //             sphereChild.SetFlag(BaseEntity.Flags.Reserved8, true);
        //         }
        //     }
        // }

        // void OnEngineStopped(BaseVehicle vehicle)
        // {
        //     if (vehicle.PrefabName != RHIBPrefab) return;

        //     foreach (var vehicleChild in vehicle.children)
        //     {
        //         if (vehicleChild.PrefabName != SpherePrefab) continue;

        //         foreach (var sphereChild in vehicleChild.children)
        //         {
        //             if (sphereChild.PrefabName != SearchLightPrefab) continue;

        //             sphereChild.SetFlag(BaseEntity.Flags.Reserved8, false);
        //         }
        //     }
        // }

        #endregion

        private class EntityTransform
        {
            public Vector3 LocalPosition;
            public float LocalScale;
            public Vector3 LocalEulerAngles;

        }

        private static void AddSearchlight(BaseVehicle vehicle, EntityTransform entityTransform)
        {
            var searchLight = GameManager.server.CreateEntity(SearchLightPrefab, entityTransform.LocalPosition) as SearchLight;
            if (searchLight == null) return;

            searchLight.SetParent(vehicle);
            searchLight.SetFlag(BaseEntity.Flags.Reserved8, true);
            searchLight.SetFlag(BaseEntity.Flags.Busy, true);
            searchLight.pickup.enabled = false;
            searchLight.transform.localEulerAngles = entityTransform.LocalEulerAngles;

            RemoveColliderProtection(searchLight);

            searchLight.Spawn();

            _pluginInstance.EntityScaleManager.CallHook("API_ScaleEntity", searchLight, entityTransform.LocalScale);
        }

        private static void AddBbq(BaseVehicle vehicle, EntityTransform entityTransform)
        {
            var bbqEntity = GameManager.server.CreateEntity(BbqPrefab, entityTransform.LocalPosition) as BaseOven;
            if (bbqEntity == null) return;

            bbqEntity.dropsLoot = true;
            bbqEntity.SetParent(vehicle);
            bbqEntity.pickup.enabled = false;
            bbqEntity.transform.localEulerAngles = entityTransform.LocalEulerAngles;

            RemoveColliderProtection(bbqEntity);

            bbqEntity.Spawn();

            _pluginInstance.EntityScaleManager.CallHook("API_ScaleEntity", bbqEntity, entityTransform.LocalScale);
        }

        private static void AddLocker(BaseVehicle vehicle, EntityTransform entityTransform)
        {
            var locker = GameManager.server?.CreateEntity(LockerPrefab, entityTransform.LocalPosition) as Locker;
            if (locker == null) return;

            locker.SetParent(vehicle);
            locker.pickup.enabled = false;
            locker.transform.localEulerAngles = entityTransform.LocalEulerAngles;

            RemoveColliderProtection(locker);

            locker.Spawn();

            _pluginInstance.EntityScaleManager.CallHook("API_ScaleEntity", locker, entityTransform.LocalScale);
        }

        private static void AddSmallWoodSign(BaseVehicle vehicle, EntityTransform entityTransform)
        {
            var smallWoodSign = GameManager.server.CreateEntity(SmallWoodSignPrefab, entityTransform.LocalPosition) as Signage;
            if (smallWoodSign == null) return;

            smallWoodSign.SetFlag(BaseEntity.Flags.Reserved8, true);
            smallWoodSign.SetParent(vehicle);
            smallWoodSign.pickup.enabled = false;
            smallWoodSign.transform.localEulerAngles = entityTransform.LocalEulerAngles;

            RemoveColliderProtection(smallWoodSign);

            smallWoodSign.Spawn();

            _pluginInstance.EntityScaleManager.CallHook("API_ScaleEntity", smallWoodSign, entityTransform.LocalScale);
        }

        private static void AddSearchlightAboveTheCockpit(BaseVehicle vehicle)
        {
            AddSearchlight(vehicle, new EntityTransform
            {
                LocalScale = 0.4f,
                LocalPosition = new Vector3(0.27f, 3.2f, 0.85f),
                LocalEulerAngles = new Vector3(0f, 180.0f, 0f)
            });
        }

        private static void AddBbqBetweenTheRearSeats(BaseVehicle vehicle)
        {
            AddBbq(vehicle, new EntityTransform
            {
                LocalScale = 0.4f,
                LocalPosition = new Vector3(0f, 1.5f, -1.3f),
                LocalEulerAngles = new Vector3(0f, -90.0f, 0f)
            });
        }

        private static void AddLockerOnTheRearHatch(BaseVehicle vehicle)
        {
            AddLocker(vehicle, new EntityTransform
            {
                LocalScale = 0.6f,
                LocalPosition = new Vector3(0f, 1.15f, -1.6f),
                LocalEulerAngles = new Vector3(-90.0f, 0f, 0f)
            });
        }

        private static void AddSmallWoodSignToTheFrontOfTheCockpit(BaseVehicle vehicle)
        {
            AddSmallWoodSign(vehicle, new EntityTransform
            {
                LocalScale = 1f,
                LocalPosition = new Vector3(0f, 1.5f, 1.3f),
                LocalEulerAngles = new Vector3(-30f, 0f, 0f)
            });
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
