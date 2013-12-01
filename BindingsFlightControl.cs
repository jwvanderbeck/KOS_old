﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using UnityEngine;

namespace kOS
{
    [kOSBinding("ksp")]
    public class BindingFlightControls : Binding
    {
        private Vessel vessel;
        private CPU cpu;
        private List<LockableControl> controls = new List<LockableControl>();

        public override void AddTo(BindingManager manager)
        {
            cpu = manager.cpu;
            vessel = manager.cpu.Vessel;

            controls.Add(new LockableControl("THROTTLE", "throttle", cpu, manager));
            controls.Add(new LockableControl("STEERING", "steering", cpu, manager));

            controls.Add(new LockableControl("WHEELSTEERING", "wheelsteering", cpu, manager));
            controls.Add(new LockableControl("WHEELTHROTTLE", "wheelthrottle", cpu, manager));

            vessel.OnFlyByWire += OnFlyByWire;
        }
        
        public void OnFlyByWire(FlightCtrlState c)
        {
            foreach (LockableControl control in controls)
            {
                control.OnFlyByWire(ref c);
            }
        }

        public override void Update(float time)
        {
            if (vessel != cpu.Vessel)
            {
                // Try to re-establish connection to vessel
                if (vessel != null)
                {
                    vessel.OnFlyByWire -= OnFlyByWire;
                    vessel = null;
                }

                if (cpu.Vessel != null)
                {
                    vessel = cpu.Vessel;
                    vessel.OnFlyByWire += OnFlyByWire;

                    foreach (LockableControl c in controls)
                    {
                        c.UpdateVessel(vessel);
                    }
                }
            }

            base.Update(time);
        }

        public class LockableControl
        {
            public String name;
            public bool locked;
            public object Value;
            public Vessel vessel;
            String propertyName;
            public CPU cpu;

            public LockableControl(String name, String propertyName, CPU cpu, BindingManager manager)
            {
                this.name = name;
                this.cpu = cpu;
                this.vessel = cpu.Vessel;
                locked = false;
                Value = 0;
                
                manager.AddGetter(name, c => Value);
                manager.AddSetter(name, delegate{});

                this.propertyName = propertyName;
            }

            public void OnFlyByWire(ref FlightCtrlState c)
            {
                Expression e = cpu.GetDeepestChildContext().GetLock(propertyName);
                
                if (e != null)
                {
                    Value = e.GetValue();

                    if (propertyName == "throttle")
                    {
                        c.mainThrottle = (float)e.Double();
                    }

                    if (propertyName == "wheelthrottle")
                    {
                        c.wheelThrottle = (float)Utils.Clamp(e.Double(), -1, 1);
                    }

                    if (propertyName == "steering")
                    {
                        if (Value is String && ((string)Value).ToUpper() == "KILL")
                        {
                            SteeringHelper.KillRotation(c, vessel);
                        }
                        else if (Value is Direction)
                        {
                            SteeringHelper.SteerShipToward((Direction)Value, c, vessel);
                        }
                        else if (Value is Vector)
                        {
                            SteeringHelper.SteerShipToward(((Vector)Value).ToDirection(), c, vessel);
                        }
                        else if (Value is Node)
                        {
                            SteeringHelper.SteerShipToward(((Node)Value).GetBurnVector().ToDirection(), c, vessel);
                        }
                    }

                    if (propertyName == "wheelsteering")
                    {
                        float bearing = 0;

                        if (Value is VesselTarget)
                        {
                            bearing = VesselUtils.GetTargetBearing(vessel, ((VesselTarget)Value).target);
                        }
                        else if (Value is GeoCoordinates)
                        {
                            bearing = ((GeoCoordinates)Value).GetBearing(vessel);
                        }
                        else if (Value is double)
                        {
                            bearing = (float)(Math.Round((double)Value) - Mathf.Round(FlightGlobals.ship_heading));
                        }

                        if (vessel.horizontalSrfSpeed > 0.1f)
                        { 
                            if (Mathf.Abs(VesselUtils.AngleDelta(VesselUtils.GetHeading(vessel), VesselUtils.GetVelocityHeading(vessel))) <= 90)
                            {
                                c.wheelSteer = Mathf.Clamp(bearing / -10, -1, 1);
                            }
                            else
                            {
                                c.wheelSteer = -Mathf.Clamp(bearing / -10, -1, 1);
                            }
                        }
                    }

                    if (cpu.GetLock(name) == null)
                    {
                        locked = false;
                    }
                }
            }

            internal void UpdateVessel(Vessel vessel)
            {
                this.vessel = vessel;
            }
        }
    }
    
    [kOSBinding("ksp")]
    public class BindingActionGroups : Binding
    {
        public override void AddTo(BindingManager manager)
        {
            manager.AddSetter("SAS", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, (bool) val));
            manager.AddSetter("GEAR", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Gear, (bool) val));
            manager.AddSetter("LEGS", (cpu, val) => VesselUtils.LandingLegsCtrl(cpu.Vessel, (bool) val));
            manager.AddSetter("CHUTES", (cpu, val) => VesselUtils.DeployParachutes(cpu.Vessel, (bool) val));
            manager.AddSetter("LIGHTS", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Light, (bool) val));
            manager.AddSetter("PANELS", (cpu, val) => VesselUtils.SolarPanelCtrl(cpu.Vessel, (bool) val));
            manager.AddSetter("BRAKES", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, (bool) val));
            manager.AddSetter("RCS", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, (bool) val));
            manager.AddSetter("ABORT", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Abort, (bool) val));
            manager.AddSetter("AG1", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom01, (bool) val));
            manager.AddSetter("AG2", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom02, (bool) val));
            manager.AddSetter("AG3", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom03, (bool) val));
            manager.AddSetter("AG4", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom04, (bool) val));
            manager.AddSetter("AG5", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom05, (bool) val));
            manager.AddSetter("AG6", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom06, (bool) val));
            manager.AddSetter("AG7", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom07, (bool) val));
            manager.AddSetter("AG8", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom08, (bool) val));
            manager.AddSetter("AG9", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom09, (bool) val));
            manager.AddSetter("AG10", (cpu, val) => cpu.Vessel.ActionGroups.SetGroup(KSPActionGroup.Custom10, (bool) val));

            manager.AddGetter("SAS", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.SAS]);
            manager.AddGetter("GEAR", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Gear]);
            manager.AddGetter("LEGS", cpu => VesselUtils.GetLandingLegStatus(cpu.Vessel));
            manager.AddGetter("CHUTES", cpu => VesselUtils.GetChuteStatus(cpu.Vessel));
            manager.AddGetter("LIGHTS", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Light]);
            manager.AddGetter("PANELS", cpu => VesselUtils.GetSolarPanelStatus(cpu.Vessel));
            manager.AddGetter("BRAKES", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Brakes]);
            manager.AddGetter("RCS", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.RCS]);
            manager.AddGetter("ABORT", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Abort]);
            manager.AddGetter("AG1", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Custom01]);
            manager.AddGetter("AG2", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Custom02]);
            manager.AddGetter("AG3", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Custom03]);
            manager.AddGetter("AG4", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Custom04]);
            manager.AddGetter("AG5", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Custom05]);
            manager.AddGetter("AG6", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Custom06]);
            manager.AddGetter("AG7", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Custom07]);
            manager.AddGetter("AG8", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Custom08]);
            manager.AddGetter("AG9", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Custom09]);
            manager.AddGetter("AG10", cpu => cpu.Vessel.ActionGroups[KSPActionGroup.Custom10]);
        }
    }

    [kOSBinding("ksp")]
    public class BindingFlightSettings : Binding
    {
        public override void AddTo(BindingManager manager)
        {
            manager.AddSetter("TARGET", delegate(CPU cpu, object val) 
            {
                if (val is ITargetable)
                {
                    VesselUtils.SetTarget((ITargetable)val);
                }
                else if (val is VesselTarget)
                {
                    VesselUtils.SetTarget(((VesselTarget)val).target);
                }
                else if (val is BodyTarget)
                {
                    VesselUtils.SetTarget(((BodyTarget)val).target);
                }
                else
                {
                    var body = VesselUtils.GetBodyByName(val.ToString());
                    if (body != null)
                    {
                        VesselUtils.SetTarget(body);
                        return;
                    }

                    var vessel = VesselUtils.GetVesselByName(val.ToString(), cpu.Vessel);
                    if (vessel != null)
                    {
                        VesselUtils.SetTarget(vessel);
                        return;
                    }
                }
            });

            manager.AddGetter("TARGET", delegate(CPU cpu) 
            {
                var currentTarget = FlightGlobals.fetch.VesselTarget;

                if (currentTarget is Vessel)
                {
                    return new VesselTarget((Vessel)currentTarget, cpu);
                }
                else if (currentTarget is CelestialBody)
                {
                    return new BodyTarget((CelestialBody)currentTarget, cpu);
                }

                return null;
            });
        }
    }
}
