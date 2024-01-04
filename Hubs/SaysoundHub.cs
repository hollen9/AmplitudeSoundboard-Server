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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.IO;
using System.Threading.Tasks;

namespace Amplitude.Hubs
{
    [Authorize("SignalRPolicy")]
    public class SaysoundHub : Hub
    {
        public async Task SendMessage(string user, string message, string fullpath, float pitch = 0f, int volume = 100, int tempo = 0, bool isExclusiveMusic = false, bool isLoop = false)
        {
            var sc = new SoundClip();
            sc.Name = user + ":" + Path.GetFileName(fullpath);
            sc.AudioFilePath = fullpath;
            sc.OutputProfileId = "DEFAULT";
            sc.Volume = volume;
            sc.IsExclusiveMusic = isExclusiveMusic;
            sc.LoopClip = isLoop;

            if (isExclusiveMusic) 
            {
                // Stop all other music
                for (int i = App.SoundEngine.CurrentlyPlaying.Count - 1; i >= 0; i--)
                {
                    var clip = App.SoundEngine.CurrentlyPlaying[i];
                    if (clip.IsExclusiveMusic)
                    {
                        clip.StopPlayback();
                    }
                }
            }

            App.SoundEngine.Play(sc, pitch, tempo);
            //App.SoundEngine.Play(fullpath, 100, 100, "Default", false);
            // await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public void StopAllAudio()
        {
            App.SoundEngine.Reset();
        }

        public void StopAudioWithNameCritia(string criteria)
        {
            for (int i = App.SoundEngine.CurrentlyPlaying.Count - 1; i >= 0; i--)
            {
                var clip = App.SoundEngine.CurrentlyPlaying[i];
                if (clip.Name.Contains(criteria))
                {
                    clip.StopPlayback();
                }
            }
        }

        public void StopExclusiveMusic()
        {
            for (int i = App.SoundEngine.CurrentlyPlaying.Count - 1; i >= 0; i--)
            {
                var clip = App.SoundEngine.CurrentlyPlaying[i];
                if (clip.IsExclusiveMusic)
                {
                    clip.StopPlayback();
                }
            }
        }
    }
}