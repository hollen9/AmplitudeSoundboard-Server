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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Amplitude.Hubs;
using System;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Options;
using System.Net;
using Microsoft.AspNetCore.SignalR;

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
                    return $"http://{OptionsManager.Instance.Options.ServerIp}:{OptionsManager.Instance.Options.ServerPort}";//Localization.Localizer.Instance["ServerRunning"];
                }
                else
                {
                    return "Server Off";//Localization.Localizer.Instance["ServerStopped"];
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

            try
            {
                Host?.Dispose();
                Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder => webBuilder
                        //.UseUrls($"http://{OptionsManager.Instance.Options.ServerIp}:" + OptionsManager.Instance.Options.ServerPort)

                        // TO DO: 我加入了 UseKestrel 就會沒辦法連，不知道原因是什麼
                        //.UseKestrel(options => //Kestral will override the default UseUrls's port
                        //{
                        //    // Default port
                        //    options.ListenLocalhost(OptionsManager.Instance.Options.ServerPort,
                        //        c =>
                        //        {
                        //            c.UseHub<SaysoundHub>();
                        //        });

                        //    //options.Listen(IPAddress.IPv6Any, 5000);

                        //    // Hub bound to TCP end point
                        //    options.Listen(IPAddress.Any, OptionsManager.Instance.Options.ServerPort, builder =>
                        //    {
                        //        builder.UseHub<SaysoundHub>();
                        //    });
                        //})
                        //.UseKestrel()
                        .UseUrls($"http://{OptionsManager.Instance.Options.ServerIp}:" + OptionsManager.Instance.Options.ServerPort)
                        .ConfigureServices(services =>
                            {
                                services.AddSingleton<Models.Options>(this.OptionsManager.Options);
                                services.AddAuthentication(opt =>
                                {
                                    opt.DefaultAuthenticateScheme = HubTokenAuthenticationDefaults.AuthenticationScheme;
                                    opt.DefaultChallengeScheme = HubTokenAuthenticationDefaults.AuthenticationScheme;
                                }).AddHubTokenAuthenticationScheme();

                                services.AddAuthorization(options =>
                                {
                                    options.AddPolicy("SignalRPolicy", pol => pol.Requirements.Add(new HubRequirement()));
                                });

                                services.AddSignalR(opt =>
                                {
#if DEBUG
                                opt.EnableDetailedErrors = true;
#endif
                                });
                            }
                            )
                        .Configure(app =>
                        {
                            app.UseRouting();

                            app.UseAuthentication();
                            app.UseAuthorization();

                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapHub<SaysoundHub>("/hubs/saysoundHub",
                                    options => options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets);
                            });

                            //app.UseEndpoints(endpoints =>
                            //{
                            //    endpoints.MapHub<SaysoundHub>("/hubs/saysoundHub");
                            //});
                        }))
                   .Build();

                await Host.StartAsync();
                OnPropertyChanged(nameof(ServerStatus));
            }
            catch (Exception ex)
            {
                AmplitudeSoundboard.App.WindowManager.ShowErrorString(ex.Message);
                throw;
            }
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
