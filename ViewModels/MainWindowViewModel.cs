/*
    AmplitudeSoundboard
    Copyright (C) 2021-2023 dan0v
    https://git.dan0v.com/AmplitudeSoundboard

    This file is part of AmplitudeSoundboard.

    AmplitudeSoundboard is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    AmplitudeSoundboard is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with AmplitudeSoundboard.  If not, see <https://www.gnu.org/licenses/>.
*/

using Amplitude.Helpers;
using Amplitude.Models;
using System.Collections.ObjectModel;
using System.Linq;


using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Amplitude.Hubs;



namespace Amplitude.ViewModels
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        public IHost Host { get; set; }


        public (int x, int y) WindowPosition = (0, 0);

        private string StopAudioHotkey => string.IsNullOrEmpty(OptionsManager.Options.GlobalKillAudioHotkey) ? Localization.Localizer.Instance["StopAllAudio"] : Localization.Localizer.Instance["StopAllAudio"] + ": " + OptionsManager.Options.GlobalKillAudioHotkey;

        private bool _queueSeperatorVisible = false;
        public bool QueueSeperatorVisible
        {
            get => _queueSeperatorVisible;
            set
            {
                if (_queueSeperatorVisible != value)
                {
                    _queueSeperatorVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public MainWindowViewModel()
        {
            OptionsManager.PropertyChanged += OptionsManager_PropertyChanged;
            SoundEngine.Queued.CollectionChanged += Queued_CollectionChanged;

            GridItemsRows.Clear();
            foreach (GridItemRow temp in OptionsManager.GetGridLayout())
            {
                GridItemsRows.Add(temp);
            }
            OnPropertyChanged(nameof(GridItemsRows));
        }

        private void Queued_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (SoundEngine.CurrentlyPlaying.Any() && SoundEngine.Queued.Any())
            {
                QueueSeperatorVisible = true;
            }
            else
            {
                QueueSeperatorVisible = false;
            }
        }

        private void OptionsManager_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OptionsManager.Options))
            {
                OnPropertyChanged(nameof(StopAudioHotkey));

                GridItemsRows.Clear();
                foreach (GridItemRow temp in OptionsManager.GetGridLayout())
                {
                    GridItemsRows.Add(temp);
                }
                OnPropertyChanged(nameof(GridItemsRows));
            }
        }

        private ObservableCollection<GridItemRow> _gridItemsRows = new();
        private ObservableCollection<GridItemRow> GridItemsRows { get => _gridItemsRows; }

        public string ServerStatus
        {
            get
            {
                if (Host != null)
                {
                    return "On";//Localization.Localizer.Instance["ServerRunning"];
                }
                else
                {
                    return "Off";//Localization.Localizer.Instance["ServerStopped"];
                }
            }
        }

        public void ShowList()
        {
            WindowManager.ShowSoundClipListWindow(new Avalonia.PixelPoint(WindowPosition.x + 200, WindowPosition.y + 200));
        }

        public void ShowGlobalSettings()
        {
            WindowManager.ShowGlobalSettingsWindow(new Avalonia.PixelPoint(WindowPosition.x + 150, WindowPosition.y + 150));
        }

        public void ShowAbout()
        {
            WindowManager.ShowAboutWindow(new Avalonia.PixelPoint(WindowPosition.x + 100, WindowPosition.y + 100));
        }

        public async Task ServerStart()
        {
            Host?.Dispose();
            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder => webBuilder
                    .UseUrls("http://localhost:53353")
                    .ConfigureServices(services => services.AddSignalR())
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapHub<SaysoundHub>("/saysoundHub"));
                    }))
               .Build();

            await Host.StartAsync();
            OnPropertyChanged(nameof(ServerStatus));
        }

        public async Task ServerStop()
        {
            if (Host != null)
            {
                await Host.StopAsync();
                Host.Dispose();
                Host = null;
                OnPropertyChanged(nameof(ServerStatus));
            }
        }

        public void StopAudio()
        {
            SoundEngine.Reset();
        }

        public void RemoveFromQueue(object o)
        {
            if (o is SoundClip clip)
            {
                SoundEngine.RemoveFromQueue(clip);
            }
        }

        public override void Dispose()
        {
            OptionsManager.PropertyChanged -= OptionsManager_PropertyChanged;
            base.Dispose();
        }
    }
}
