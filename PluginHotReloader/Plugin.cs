using System;
using Exiled.API.Features;

namespace PluginHotReloader
{
    public class Plugin : Plugin<Config>
    {
        public override string Name => "PluginHotReloader";
        public override string Author => "Waenara";
        public override Version Version => new Version(1, 0, 0);

        private HotReloader _reloader;

        public override void OnEnabled()
        {
            _reloader = new HotReloader();
            _reloader.Start();

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            _reloader?.Stop();
            _reloader = null;

            base.OnDisabled();
        }
    }
}