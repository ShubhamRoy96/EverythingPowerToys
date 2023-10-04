﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Community.PowerToys.Run.Plugin.Everything.Properties;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Infrastructure;
using Wox.Infrastructure.Storage;
using Wox.Plugin;
using Wox.Plugin.Common;
using Wox.Plugin.Logger;
using static Community.PowerToys.Run.Plugin.Everything.Interop.NativeMethods;

namespace Community.PowerToys.Run.Plugin.Everything
{
    public class Main : IPlugin, IDisposable, IDelayedExecutionPlugin, IContextMenu, ISettingProvider, IPluginI18n, ISavable
    {
        private readonly Settings _setting;
        private readonly PluginJsonStorage<Settings> _storage;
        private IContextMenu _contextMenuLoader;
        private PluginInitContext _context;
        private bool _disposed;
        private Everything _everything;

        public string Name => Resources.plugin_name;

        public string Description => Resources.plugin_description;

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = nameof(Settings.Context),
                DisplayLabel = Resources.Context,
                DisplayDescription = Resources.Context_Description,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                TextValue = _setting.Context,
            },
            new PluginAdditionalOption()
            {
                Key = nameof(Settings.Sort),
                DisplayLabel = Resources.Sort,
                DisplayDescription = Resources.Sort_Description,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Combobox,
                ComboBoxOptions = Enum.GetNames(typeof(Sort)).ToList(),
                ComboBoxValue = (int)_setting.Sort,
            },
            new PluginAdditionalOption()
            {
                Key = nameof(Settings.Max),
                DisplayLabel = Resources.Max,
                DisplayDescription = Resources.Max_Description,
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
                NumberValue = _setting.Max,
            },
            new PluginAdditionalOption()
            {
                Key = nameof(Settings.Copy),
                DisplayLabel = Resources.SwapCopy,
                DisplayDescription = Resources.SwapCopy_Description,
                Value = false,
            },
            new PluginAdditionalOption()
            {
                Key = nameof(Settings.MatchPath),
                DisplayLabel = Resources.Match_path,
                DisplayDescription = Resources.Match_path_Description,
                Value = false,
            },
            new PluginAdditionalOption()
            {
                Key = nameof(Settings.Preview),
                DisplayLabel = Resources.Preview,
                DisplayDescription = Resources.Preview_Description,
                Value = true,
            },
            new PluginAdditionalOption()
            {
                Key = nameof(Settings.QueryText),
                DisplayLabel = Resources.QueryText,
                DisplayDescription = Resources.QueryText_Description,
                Value = false,
            },
            new PluginAdditionalOption()
            {
                Key = nameof(Settings.RegEx),
                DisplayLabel = Resources.RegEx,
                DisplayDescription = Resources.RegEx_Description,
                Value = false,
            },
            new PluginAdditionalOption()
            {
                Key = nameof(Settings.Updates),
                DisplayLabel = Resources.Updates,
                DisplayDescription = $"v{Assembly.GetExecutingAssembly().GetName().Version}",
                Value = true,
            },
        };

        public Main()
        {
            _storage = new PluginJsonStorage<Settings>();
            _setting = _storage.Load();
        }

        public void Init(PluginInitContext context)
        {
            _setting.Getfilters();
            if (_setting.Updates)
                Task.Run(() => new Update(Assembly.GetExecutingAssembly().GetName().Version));
            _context = context;
            _contextMenuLoader = new ContextMenuLoader(context, _setting.Context);
            ((ContextMenuLoader)_contextMenuLoader).Update(_setting);
            _everything = new Everything(_setting);
        }

        public List<Result> Query(Query query)
        {
            List<Result> results = new List<Result>();
            return results;
        }

        public List<Result> Query(Query query, bool delayedExecution)
        {
            List<Result> results = new List<Result>();
            if (!string.IsNullOrEmpty(query.Search))
            {
                var searchQuery = query.Search;

                try
                {
                    results.AddRange(_everything.Query(searchQuery, _setting));
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    results.Add(new Result()
                    {
                        Title = Resources.Everything_not_running,
                        SubTitle = Resources.Everything_ini,
                        IcoPath = "Images/warning.png",
                        Score = int.MaxValue,
                    });
                }
                catch (Exception e)
                {
                    Log.Exception("Everything Exception", e, GetType());
                }
            }

            return results;
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return _contextMenuLoader.LoadContextMenus(selectedResult);
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            if (settings != null && settings.AdditionalOptions != null)
            {
                _setting.Sort = (Sort)(settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Sort))?.ComboBoxValue ?? 13);
                _setting.Max = (uint)(settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Max))?.NumberValue ?? 20);
                _setting.Context = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Context))?.TextValue ?? "012345";
                _setting.RegEx = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.RegEx))?.Value ?? false;
                _setting.Preview = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Preview))?.Value ?? true;
                _setting.MatchPath = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.MatchPath))?.Value ?? false;
                _setting.Copy = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Copy))?.Value ?? false;
                _setting.QueryText = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.QueryText))?.Value ?? false;
                _setting.Updates = settings.AdditionalOptions.FirstOrDefault(x => x.Key == nameof(_setting.Updates))?.Value ?? true;

                if (_contextMenuLoader != null) ((ContextMenuLoader)_contextMenuLoader).Update(_setting);
                if (_contextMenuLoader != null) _everything.UpdateSettings(_setting);

                Save();
            }
        }

        public void Save()
        {
            _storage.Save();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public string GetTranslatedPluginTitle()
        {
            return Resources.plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return Resources.plugin_description;
        }
    }
}
