using ColossalFramework;
using HarmonyLib;
using ICities;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using UnityEngine;

namespace ElevatedStopsEnabler
{
    public class Mod : BasePatcherMod<Mod>
    {
        #region PROPERTIES

        protected override ulong StableWorkshopId => 2862992091;
        protected override ulong BetaWorkshopId => 0;

        public override string NameRaw => "Elevated Stops Enabler Revisited";
        public override string Description => !IsBeta ? Localize.Mod_Description : CommonLocalize.Mod_DescriptionBeta;
        public override List<ModVersion> Versions => new List<ModVersion>()
        {
            new ModVersion(new Version("2.0"), new DateTime(2022,9,14)),
        };
        protected override Version RequiredGameVersion => new Version(1, 16, 0, 3);

        protected override string IdRaw => nameof(ElevatedStopsEnabler);
        protected override List<BaseDependencyInfo> DependencyInfos
        {
            get
            {
                var infos = base.DependencyInfos;

                var oldLocalSearcher = IdSearcher.Invalid & new UserModNameSearcher("Elevated Stops Enabler", BaseMatchSearcher.Option.None);
                var oldIdSearcher = new IdSearcher(634913093u);
                infos.Add(new ConflictDependencyInfo(DependencyState.Unsubscribe, oldLocalSearcher | oldIdSearcher));

                return infos;
            }
        }

#if BETA
        public override bool IsBeta => true;
#else
        public override bool IsBeta => false;
#endif
        protected override LocalizeManager LocalizeManager => Localize.LocaleManager;

        #endregion

        protected override void GetSettings(UIHelperBase helper)
        {
            var settings = new Settings();
            settings.OnSettingsUI(helper);
        }
        protected override void SetCulture(CultureInfo culture) => Localize.Culture = culture;

        #region PATCHER

        protected override bool PatchProcess()
        {
            var success = true;

            success &= AddPrefix(typeof(Patcher), nameof(Patcher.NetSegmentGetClosestLanePositionPrefix), typeof(NetSegment), nameof(NetSegment.GetClosestLanePosition), new[] { typeof(Vector3), typeof(NetInfo.LaneType), typeof(VehicleInfo.VehicleType), typeof(VehicleInfo.VehicleCategory), typeof(VehicleInfo.VehicleType), typeof(bool), typeof(Vector3).MakeByRefType(), typeof(int).MakeByRefType(), typeof(float).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(int).MakeByRefType(), typeof(float).MakeByRefType() });

            success &= AddPrefix(typeof(Patcher), nameof(Patcher.TransportLineAIAddLaneConnectionPrefix), typeof(TransportLineAI), "AddLaneConnection");
            success &= AddPrefix(typeof(Patcher), nameof(Patcher.TransportLineAIRemoveLaneConnectionPrefix), typeof(TransportLineAI), "RemoveLaneConnection");

            return success;
        }

        #endregion
    }

    public static class Patcher
    {
        public static void NetSegmentGetClosestLanePositionPrefix(ref NetSegment __instance, ref bool requireConnect)
        {
            if (requireConnect && __instance.Info.m_netAI is RoadBridgeAI)
                requireConnect = false;
        }

        public static void TransportLineAIAddLaneConnectionPrefix(NetLane.Flags ___m_stopFlag, VehicleInfo.VehicleType ___m_vehicleType, ushort nodeID, uint laneID)
        {
            if (nodeID == 0 || !___m_vehicleType.IsValidTransport())
                return;

            var segment = Singleton<NetManager>.instance.m_lanes.m_buffer[laneID].m_segment;
            var roadAi = Singleton<NetManager>.instance.m_segments.m_buffer[segment].Info.m_netAI;
            if (roadAi is not RoadBridgeAI)
                return;

            var flags = (NetLane.Flags)Singleton<NetManager>.instance.m_lanes.m_buffer[laneID].m_flags;
            flags |= ___m_stopFlag;
            Singleton<NetManager>.instance.m_lanes.m_buffer[laneID].m_flags = (ushort)flags;
            if (roadAi is RoadBridgeAI roadBridgeAI)
                roadBridgeAI.UpdateSegmentStopFlags(segment, ref Singleton<NetManager>.instance.m_segments.m_buffer[segment]);
        }

        public static void TransportLineAIRemoveLaneConnectionPrefix(NetLane.Flags ___m_stopFlag, VehicleInfo.VehicleType ___m_vehicleType, ushort nodeID, ref NetNode data)
        {
            if (nodeID == 0 || !___m_vehicleType.IsValidTransport())
                return;

            var segment = Singleton<NetManager>.instance.m_lanes.m_buffer[data.m_lane].m_segment;
            var roadAi = Singleton<NetManager>.instance.m_segments.m_buffer[segment].Info.m_netAI;
            if (roadAi is not RoadBridgeAI)
                return;

            var flags = (NetLane.Flags)Singleton<NetManager>.instance.m_lanes.m_buffer[data.m_lane].m_flags;
            flags &= ~___m_stopFlag;
            Singleton<NetManager>.instance.m_lanes.m_buffer[data.m_lane].m_flags = (ushort)flags;
            if (roadAi is RoadBridgeAI roadBridgeAI)
                roadBridgeAI.UpdateSegmentStopFlags(segment, ref Singleton<NetManager>.instance.m_segments.m_buffer[segment]);
        }
    }

    public class LoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame || mode == LoadMode.NewGameFromScenario)
            {
                try
                {
                    SingletonMod<Mod>.Logger.Debug("Start adding stops to roads");
                    ElevatedStops.AddElevatedStoptypes();
                    ElevatedStops.AllowStreetLightsOnElevatedStops();
                    SingletonMod<Mod>.Logger.Debug("Stops added to networks");
                }
                catch (Exception erroe)
                {
                    SingletonMod<Mod>.Logger.Error("Error while adding stops to networks", erroe);
                }
            }
        }
    }

    public class Settings : BaseSettings<Mod>
    {
        protected override void FillSettings()
        {
            base.FillSettings();
            AddNotifications(GeneralTab);
        }
    }
}
