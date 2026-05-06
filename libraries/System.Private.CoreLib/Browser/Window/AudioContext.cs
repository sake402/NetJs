using System;

namespace Window
{
    /// <summary>
    /// Web Audio API AudioContext.
    /// </summary>
    [NetJs.External]
    public class AudioContext
    {
        public extern double currentTime { get; }
        public extern AudioBuffer createBuffer(int numberOfChannels, int length, double sampleRate);
        public extern AudioBufferSourceNode createBufferSource();
        public extern GainNode createGain();
        public extern void resume();
        public extern void close();
    }

    [NetJs.External]
    public class AudioBuffer
    {
        public extern int length { get; }
        public extern int sampleRate { get; }
        public extern int numberOfChannels { get; }
    }

    [NetJs.External]
    public class AudioBufferSourceNode : EventTarget
    {
        public extern void start(double when = 0);
        public extern void stop(double when = 0);
        public extern AudioBuffer? buffer { get; set; }
        public extern void connect(AudioNode destination);
    }

    [NetJs.External]
    public class GainNode : AudioNode
    {
        public extern AudioParam gain { get; }
    }

    [NetJs.External]
    public class AudioNode : EventTarget
    {
        public extern void connect(AudioNode destination);
    }

    [NetJs.External]
    public class AudioParam
    {
        public extern double value { get; set; }
    }
}