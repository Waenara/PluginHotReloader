using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Discord;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Loader;

namespace PluginHotReloader
{
    public class HotReloader
    {
        private FileSystemWatcher _watcher;

        public void Start()
        {
            _watcher = new FileSystemWatcher(Paths.Plugins, "*.dll");
            _watcher.Changed += OnPluginChanged;
            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            _watcher?.Dispose();
        }

        private static void OnPluginChanged(object sender, FileSystemEventArgs e)
        {
            Log.Send($"Plugin changed: {e.FullPath}. Attempting to hot-reload...", LogLevel.Info, ConsoleColor.Green);
            IPlugin<IConfig> plugin = Loader.Plugins.FirstOrDefault(p => p.Assembly == Loader.Locations.FirstOrDefault(x => x.Value == e.FullPath).Key);

            if (plugin != null)
            {
                UnloadPlugin(plugin);
            }

            plugin = LoadPlugin(e.FullPath);
            if (plugin != null)
            {
                EnablePlugin(plugin);
            }

            Log.Send($"Hot-reload completed for plugin: {e.FullPath}", LogLevel.Info, ConsoleColor.Green);
        }

        private static void UnloadPlugin(IPlugin<IConfig> plugin)
        {
            try
            {
                plugin.Config.IsEnabled = false;
                plugin.OnUnregisteringCommands();
                plugin.OnDisabled();

                Loader.Plugins.Remove(plugin);
                Server.PluginAssemblies.Remove(plugin.Assembly);
                Loader.Locations.Remove(plugin.Assembly);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to unload plugin: {plugin.Name}\n{ex}");
            }
        }

        private static IPlugin<IConfig> LoadPlugin(string fullPath)
        {
            try
            {
                Assembly key = Loader.LoadAssembly(fullPath);
                if ((object)key != null)
                {
                    Loader.Locations[key] = fullPath;

                    if (!Loader.Locations[key].Contains("dependencies"))
                    {
                        IPlugin<IConfig> plugin = Loader.CreatePlugin(key);
                        if (plugin != null)
                        {
                            Server.PluginAssemblies.Add(key, plugin);
                            Loader.Plugins.Add(plugin);

                            return plugin;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load plugin: {Path.GetFileName(fullPath)}\n{ex}");
            }

            return null;
        }

        private static void EnablePlugin(IPlugin<IConfig> plugin)
        {
            try
            {
                if (plugin.Config.IsEnabled)
                {
                    plugin.OnEnabled();
                    plugin.OnRegisteringCommands();
                }

                if (plugin.Config.Debug)
                {
                    Log.DebugEnabled.Add(plugin.Assembly);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Plugin \"{plugin.Name}\" threw an exception while enabling: {ex}");
            }
        }
    }
}