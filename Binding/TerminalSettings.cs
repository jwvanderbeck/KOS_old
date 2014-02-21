﻿namespace kOS.Binding
{
    [KOSBinding]
    public class TerminalSettings : IBinding
    {
        public void BindTo(IBindingManager manager)
        {
            manager.AddGetter("SESSIONTIME", cpu => cpu.SessionTime);
            manager.AddGetter("VERSION", cpu => Core.VersionInfo);
            manager.AddGetter("VOLUME:NAME", cpu => cpu.SelectedVolume.Name);
        }

        public void Update(float time)
        {
        }
    }
}