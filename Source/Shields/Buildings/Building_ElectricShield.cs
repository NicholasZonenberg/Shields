﻿using System.Text;
using FrontierDevelopments.General;
using FrontierDevelopments.Shields.Comps;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FrontierDevelopments.Shields.Buildings
{
    public class Building_ElectricShield : Building, IHeatsink, IEnergySource
    {
        public enum ShieldStatus
        {
            Unpowered,
            ThermalShutdown,
            Online
        }

        private IEnergySource _energySource;
        private IShield _shield;

        private IHeatsink _heatSink;

        private bool _activeLastTick;

        private bool IsActive => _shield?.IsActive() ?? false;
        private float BasePowerConsumption => -_shield?.ProtectedCellCount * Mod.Settings.PowerPerTile ?? 0f;

        public ShieldStatus Status
        {
            get
            {
                if (_heatSink != null && _heatSink.OverTemperature) return ShieldStatus.ThermalShutdown;
                if (_energySource != null && _energySource.IsActive()) return ShieldStatus.Unpowered;
                return ShieldStatus.Online;
            }
        }

        private void Init()
        {
            _energySource = EnergySourceUtility.FindComp(AllComps);
            _shield = ShieldUtility.FindComp(AllComps);
            _heatSink = HeatsinkUtility.FindComp(AllComps);
        }

        public override void PostMake()
        {
            base.PostMake();
            Init();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            _activeLastTick = false;
            Init();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            _activeLastTick = false;
        }

        public override void Tick()
        {
            var active = IsActive;
            _energySource.BaseConsumption = BasePowerConsumption;
            base.Tick();
            if(_activeLastTick && !active && _energySource.WantActive)
                Messages.Message("fd.shields.incident.offline.body".Translate(), new GlobalTargetInfo(Position, Map), MessageTypeDefOf.NegativeEvent);
            _activeLastTick = active;
        }

        public override string GetInspectString()
        {
            var stringBuilder = new StringBuilder();
            switch (Status)
            {
                case ShieldStatus.Unpowered:
                    stringBuilder.AppendLine("shield.status.offline".Translate() + " - " + "shield.status.battery_too_low".Translate());
                    break;
                case ShieldStatus.ThermalShutdown:
                    stringBuilder.AppendLine("shield.status.offline".Translate() + " - " + "shield.status.thermal_safety".Translate());
                    break;
                case ShieldStatus.Online:
                    stringBuilder.AppendLine("shield.status.online".Translate());
                    break;
            }
            stringBuilder.Append(base.GetInspectString());
            return stringBuilder.ToString();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Heatsink
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public void PushHeat(float wattDays)
        {
            _heatSink.PushHeat(wattDays);
        }

        public bool OverTemperature => _heatSink.OverTemperature;

        public float Temp => _heatSink.Temp;
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // Energy Source
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        bool IEnergySource.IsActive()
        {
            return _energySource.IsActive();
        }

        public float Draw(float amount)
        {
            return _energySource.Draw(amount);
        }

        public void Drain(float amount)
        {
            _energySource.Drain(amount);
        }

        public bool WantActive => _energySource.WantActive;

        public float BaseConsumption
        {
            get => _energySource.BaseConsumption;
            set => _energySource.BaseConsumption = value;
        }

        public float EnergyAvailable => _energySource.EnergyAvailable;
    }
}
