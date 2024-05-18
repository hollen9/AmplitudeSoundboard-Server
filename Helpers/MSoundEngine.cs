﻿/*
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
using Avalonia.Logging;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Timers;

namespace Amplitude.Helpers
{
    class MSoundEngine : ISoundEngine
    {
        private static MSoundEngine? _instance;
        public static MSoundEngine Instance { get => _instance ??= new MSoundEngine(); }

        object currentlyPlayingLock = new object();

        private ObservableCollection<PlayingClip> _currentlyPlaying = new ObservableCollection<PlayingClip>();
        public ObservableCollection<PlayingClip> CurrentlyPlaying => _currentlyPlaying;

        object queueLock = new object();

        private ObservableCollection<SoundClip> _queued = new ObservableCollection<SoundClip>();
        public ObservableCollection<SoundClip> Queued => _queued;


        private const long TIMER_MS = 200;
        
        private Timer timer = new Timer(TIMER_MS)
        {
            AutoReset = true,
        };

        private void RefreshPlaybackProgressAndCheckQueue(object? sender, ElapsedEventArgs e)
        {
            lock(currentlyPlayingLock)
            {
                foreach(var track in CurrentlyPlaying)
                {
                    track.CurrentPos += TIMER_MS * 0.001d;
                }
                var toRemove = CurrentlyPlaying.Where(t => t.ProgressPct == 1).ToList();
                toRemove.ForEach(t =>
                {
                    if (t.LoopClip)
                    {
                        t.CurrentPos = 0;
                        // Bass.ChannelPlay(t.BassStreamId, false);
                        /// We need to re-add the track to the list, because the sound won't loop due to the introdution of
                        /// BassFx TempoCreate. This is a workaround. The original code is commented out above.
                        CurrentlyPlaying.Remove(t);
                        this.Play(t.FilePath, t.RawVolume, t.OutputVolumeMultiplier, t.OutputDeviceName, t.LoopClip, t.Name, t.Pitch, t.Tempo, t.IsExclusiveMusic);
                        
                    }
                    else
                    {
                        CurrentlyPlaying.Remove(t);
                    }
                });
            }
            lock (queueLock)
            {
                if (!CurrentlyPlaying.Any() && Queued.Any())
                {
                    var clip = Queued[0];
                    Play(clip);
                    Queued.RemoveAt(0);
                }
            }
        }

        public const int SAMPLE_RATE = 44100;

        private readonly object bass_lock = new object();


        public List<string> OutputDeviceListWithoutGlobal
        {
            get
            {
                var all = OutputDeviceListWithGlobal.ToList();
                all.RemoveAt(0);
                return all;
            }
        }

        public List<string> OutputDeviceListWithGlobal
        {
            get
            {
                List<string> devices = new List<string>();
                // Index 0 is "No Sound", so skip
                for (int dev = 1; dev < Bass.DeviceCount; dev++)
                {
                    var info = Bass.GetDeviceInfo(dev);
                    devices.Add(info.Name);
                }
                return devices;
            }
        }

        private int? GetOutputPlayerByName(string playerDeviceName)
        {
            if (playerDeviceName == ISoundEngine.GLOBAL_DEFAULT_DEVICE_NAME)
            {
                playerDeviceName = ISoundEngine.DEFAULT_DEVICE_NAME;
            }

            if (playerDeviceName == ISoundEngine.DEFAULT_DEVICE_NAME || playerDeviceName == "System default")
            {
                return 1;
            }

            if (OutputDeviceListWithoutGlobal.Contains(playerDeviceName))
            {
                for (int n = 0; n < Bass.DeviceCount; n++)
                {
                    var info = Bass.GetDeviceInfo(n);
                    if (playerDeviceName == info.Name)
                    {
                        return n;
                    }
                }
            }
            return null;
        }

        private MSoundEngine()
        {
            timer.Elapsed += RefreshPlaybackProgressAndCheckQueue;
            timer.Start();
        }

        public void AddToQueue(SoundClip source)
        {
            lock(queueLock)
            {
                Queued.Add(source.ShallowCopy());
            }
        }

        public void Play(SoundClip source, float pitch = 0f, int tempo = 0)
        {
            if (!BrowseIO.ValidAudioFile(source.AudioFilePath, true, source))
            {
                return;
            }

            foreach (OutputSettings settings in source.OutputSettingsFromProfile)
            {
                Play(source.AudioFilePath, settings.Volume, source.Volume, settings.DeviceName, source.LoopClip, source.Name, pitch, tempo, source.IsExclusiveMusic, source.ClientSendTime);
            }
        }

        public void Play(string fileName, int volume, int volumeMultiplier, string playerDeviceName, bool loopClip, string? name = null, float pitch = 0f, int tempo = 0, bool isExclusiveMusic = false, DateTimeOffset? clientSendTime = null)
        {
            double vol = (volume / 100.0) * (volumeMultiplier / 100.0);

            int? devId = GetOutputPlayerByName(playerDeviceName);

            if (!devId.HasValue)
            {
                App.WindowManager.ShowErrorString(string.Format(Localization.Localizer.Instance["MissingDeviceString"], playerDeviceName));
                return;
            }

            bool streamError = false;
            bool bassError = false;

            lock (bass_lock)
            {
                // Init device
                if (Bass.Init(devId.Value, SAMPLE_RATE) || Bass.LastError == Errors.Already)
                {
                    Bass.CurrentDevice = devId.Value;
                    int mixerChannelHandle = BassMix.CreateMixerStream(SAMPLE_RATE, 2, BassFlags.Default);
                    int streamChannelHandle = Bass.CreateStream(fileName, Flags: BassFlags.Decode);
                    
                    //if (!Bass.ChannelSetAttribute(streamChannelHandle, ChannelAttribute.Volume, vol))
                    //{
                    //    App.WindowManager.ShowErrorString($"Volume: {Bass.LastError}");
                    //}


                    int stream_fx_tempo = BassFx.TempoCreate(streamChannelHandle, BassFlags.FxFreeSource | BassFlags.Decode);
                    if (!Bass.ChannelSetAttribute(stream_fx_tempo, ChannelAttribute.Pitch, pitch))
                    {
                        App.WindowManager.ShowErrorString($"TempoFx Pitch: {Bass.LastError}");
                    }
                    if (!Bass.ChannelSetAttribute(stream_fx_tempo, ChannelAttribute.Volume, vol))
                    {
                        App.WindowManager.ShowErrorString($"Volume: {Bass.LastError}");
                    }

                    // speed up or down based on pitch
                    if (tempo != 0)
                    {
                        if (!Bass.ChannelSetAttribute(stream_fx_tempo, ChannelAttribute.Tempo, tempo))
                        {
                            App.WindowManager.ShowErrorString($"TempoFx Tempo: {Bass.LastError}");
                        }
                    }



                    //if (pitch != 0f)
                    //{
                    //    if (!Bass.ChannelSetAttribute(streamChannelHandle, ChannelAttribute.Pitch, pitch))
                    //    {
                    //        App.WindowManager.ShowErrorString($"Pitch: {Bass.LastError}");
                    //    }

                    //}


                    BassMix.MixerAddChannel(mixerChannelHandle, stream_fx_tempo, BassFlags.AutoFree | BassFlags.MixerChanDownMix);
                    Bass.ChannelPlay(mixerChannelHandle);
                    

                    if (stream_fx_tempo != 0)
                    {
                        // Track active streams so they can be stopped
                        try
                        {
                            var len = Bass.ChannelGetLength(stream_fx_tempo, PositionFlags.Bytes);
                            double length = Bass.ChannelBytes2Seconds(stream_fx_tempo, len);
                            PlayingClip track = new PlayingClip(name ?? Path.GetFileNameWithoutExtension(fileName) ?? "", playerDeviceName, stream_fx_tempo, length, loopClip);

                            track.ClientSendTime = clientSendTime;
                            track.IsExclusiveMusic = isExclusiveMusic;
                            track.Pitch = pitch;
                            track.Tempo = tempo;
                            track.RawVolume = volume;
                            track.FilePath = fileName;
                            track.OutputDeviceName = playerDeviceName;
                            track.OutputVolumeMultiplier = volumeMultiplier;
                            
                            lock(currentlyPlayingLock)
                            {
                                CurrentlyPlaying.Add(track);
                            }
                            Bass.ChannelPlay(stream_fx_tempo, false);
                        }
                        catch(Exception e)
                        {
                            App.WindowManager.ShowErrorString(string.Format(Localization.Localizer.Instance["FileBadFormatString"], fileName));
                        }
                    }
                    else
                    {
                        streamError = true;
                    }
                }
                else
                {
                    bassError = true;
                }
            }
            if (streamError)
            {
                App.WindowManager.ShowErrorString($"Stream error: {Bass.LastError}");
            }
            if (bassError)
            {
                App.WindowManager.ShowErrorString($"ManagedBass error: {Bass.LastError}");
            }
        }

        public void CheckDeviceExistsAndGenerateErrors(OutputProfile profile)
        {
            foreach (OutputSettings settings in profile.OutputSettings)
            {
                if (GetOutputPlayerByName(settings.DeviceName) == null)
                {
                    if (profile != null)
                    {
                        App.WindowManager.ShowErrorOutputProfile(profile, ViewModels.ErrorListViewModel.OutputProfileErrorType.MISSING_DEVICE, settings.DeviceName);
                    }
                }
            }
        }

        public void Reset()
        {
            lock(queueLock)
            {
                Queued.Clear();
            }
            lock(currentlyPlayingLock)
            {
                foreach (var stream in CurrentlyPlaying)
                {
                    Bass.StreamFree(stream.BassStreamId);
                }

                CurrentlyPlaying.Clear();
            }
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Elapsed -= RefreshPlaybackProgressAndCheckQueue;
            Reset();
            Bass.Free();
        }

        public void StopPlaying(int bassId)
        {
            lock (currentlyPlayingLock)
            {
                Bass.StreamFree(bassId);
                PlayingClip? track = CurrentlyPlaying.FirstOrDefault(c => c.BassStreamId == bassId);
                if (track != null)
                {
                    CurrentlyPlaying.Remove(track);
                }
            }
        }

        public void RemoveFromQueue(SoundClip clip)
        {
            lock(queueLock)
            {
                Queued.Remove(clip);
            }
        }
    }
}