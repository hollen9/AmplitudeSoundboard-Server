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

using Avalonia;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using System.Net.NetworkInformation;

namespace AmplitudeSoundboard
{
    class Program
    {
        public class AdvancedSettings
        {
            public bool Disabled { get; set; }
            public string Font { get; set; }
        }
        
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            // Read advanced settings
            string jsonFilename = "advancedSettings.json";
            if (System.IO.File.Exists("advancedSettings.jsonc"))
            {
                jsonFilename = "advancedSettings.jsonc";
            }
            var advSettings = new AdvancedSettings();
            if (System.IO.File.Exists(jsonFilename))
            {
                // use ConfigurationBuilder to read settings
                var config = new ConfigurationBuilder()
                    .AddJsonFile(jsonFilename)
                    .Build();
                config.Bind("advancedSettings", advSettings);
                if (advSettings.Disabled)
                {
                    advSettings = null;
                }
            }
            BuildAvaloniaApp(advSettings).StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnMainWindowClose);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp(AdvancedSettings advSettings)
        {
            var app = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();

            if (advSettings?.Font != null)
            {
                app.With(new FontManagerOptions
                {
                    DefaultFamilyName = advSettings.Font
                });
            }
            
            return app;
        }
    }
}
