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

using Amplitude.Models;
using AmplitudeSoundboard;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Amplitude.Hubs
{
    public class SaysoundHub : Hub
    {
        public async Task SendMessage(string user, string message, string fullpath, float pitch = 0f, int volume = 100, int tempo = 0)
        {
            var sc = new SoundClip();
            sc.AudioFilePath = fullpath;
            sc.OutputProfileId = "DEFAULT";
            sc.Volume = volume;
            App.SoundEngine.Play(sc, pitch, tempo);
            //App.SoundEngine.Play(fullpath, 100, 100, "Default", false);
            // await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public void StopAllAudio()
        {
            App.SoundEngine.Reset();
        }
    }
}