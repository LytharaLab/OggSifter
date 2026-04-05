// MIT License
// 
// Copyright (c) 2026 LytharaLab
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using NAudio.Wave;
using NAudio.Vorbis;

namespace OggSifter
{
    public sealed class OggPlayer : IDisposable
    {
        private IWavePlayer? _output;
        private WaveStream? _reader;

        public bool IsPlaying { get; private set; }

        public event EventHandler<string>? PlaybackError;

        public void Play(string filePath)
        {
            StopInternal();

            try
            {
                // VorbisWaveReader provides a WaveStream for .ogg Vorbis.
                _reader = new VorbisWaveReader(filePath);

                _output = new WaveOutEvent
                {
                    DesiredLatency = 100
                };
                _output.PlaybackStopped += (_, __) =>
                {
                    IsPlaying = false;
                };
                _output.Init(_reader);
                _output.Play();
                IsPlaying = true;
            }
            catch (Exception ex)
            {
                StopInternal();
                PlaybackError?.Invoke(this, ex.Message);
                throw;
            }
        }

        public void Stop()
        {
            StopInternal();
        }

        private void StopInternal()
        {
            try
            {
                if (_output != null)
                {
                    _output.Stop();
                    _output.Dispose();
                }
            }
            catch { /* ignore */ }
            finally
            {
                _output = null;
            }

            try
            {
                _reader?.Dispose();
            }
            catch { /* ignore */ }
            finally
            {
                _reader = null;
            }

            IsPlaying = false;
        }

        public void Dispose()
        {
            StopInternal();
        }
    }
}
