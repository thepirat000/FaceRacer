#if ANDROID
using Android.Media;
using Android.OS;

namespace FaceRacerLive;

public sealed class MicToSpeakerService : IMicToSpeakerService
{
    private readonly object _gate = new();

    private CancellationTokenSource? _cts;
    private Task? _loopTask;

    private AudioRecord? _audioRecord;
    private AudioTrack? _audioTrack;

    public bool IsRunning { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (IsRunning)
                return;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            IsRunning = true;
        }

        var status = await Permissions.RequestAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
        {
            await StopAsync();
            throw new PermissionException("Microphone permission not granted.");
        }

        _loopTask = Task.Run(() => LoopAsync(_cts!.Token));
    }

    public async Task StopAsync()
    {
        Task? loopTask;
        lock (_gate)
        {
            if (!IsRunning)
                return;

            IsRunning = false;

            _cts?.Cancel();
            loopTask = _loopTask;
        }

        try
        {
            if (loopTask is not null)
                await loopTask;
        }
        finally
        {
            ReleaseAudio();
            lock (_gate)
            {
                _cts?.Dispose();
                _cts = null;
                _loopTask = null;
            }
        }
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        const int sampleRate = 16000;
        const ChannelIn inChannel = ChannelIn.Mono;
        const ChannelOut outChannel = ChannelOut.Mono;
        const Encoding encoding = Encoding.Pcm16bit;

        var minRec = AudioRecord.GetMinBufferSize(sampleRate, inChannel, encoding);
        var minPlay = AudioTrack.GetMinBufferSize(sampleRate, outChannel, encoding);

        var bufferSize = Math.Max(minRec, minPlay);
        if (bufferSize <= 0)
            throw new InvalidOperationException("Unable to determine audio buffer size.");

        var source = Build.VERSION.SdkInt >= BuildVersionCodes.N
            ? AudioSource.VoiceCommunication
            : AudioSource.Mic;

        _audioRecord = new AudioRecord(source, sampleRate, inChannel, encoding, bufferSize);

        _audioTrack = CreateAudioTrack(sampleRate, outChannel, encoding, bufferSize);

        if (_audioRecord.State != State.Initialized || _audioTrack.State != AudioTrackState.Initialized)
            throw new InvalidOperationException("AudioRecord/AudioTrack failed to initialize.");

        var buffer = new byte[bufferSize];

        _audioTrack.Play();
        _audioRecord.StartRecording();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var read = _audioRecord.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    _audioTrack.Write(buffer, 0, read);
                }
                else
                {
                    await Task.Delay(5, ct);
                }
            }
        }
        finally
        {
            try { _audioRecord?.Stop(); } catch { }
            try { _audioTrack?.Stop(); } catch { }
        }
    }

    private static AudioTrack CreateAudioTrack(int sampleRate, ChannelOut outChannel, Encoding encoding, int bufferSize)
    {
        // API 26+: use builder-based API (avoids obsolete constructor warning)
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channelMask = outChannel == ChannelOut.Mono ? ChannelOut.Mono : ChannelOut.Stereo;

            var format = new AudioFormat.Builder()
                .SetEncoding(encoding)
                .SetSampleRate(sampleRate)
                .SetChannelMask(channelMask)
                .Build();

            // VoiceCommunication is typically best for realtime mic loopback
            var attributes = new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.VoiceCommunication)
                .SetContentType(AudioContentType.Speech)
                .Build();

            return new AudioTrack.Builder()
                .SetAudioAttributes(attributes)
                .SetAudioFormat(format)
                .SetBufferSizeInBytes(bufferSize)
                .SetTransferMode(AudioTrackMode.Stream)
                .Build();
        }

#pragma warning disable CA1422 // Call site reaches older Android versions; legacy ctor used intentionally for API < 26
        return new AudioTrack(
            Android.Media.Stream.VoiceCall,
            sampleRate,
            outChannel,
            encoding,
            bufferSize,
            AudioTrackMode.Stream);
#pragma warning restore CA1422
    }

    private void ReleaseAudio()
    {
        try { _audioRecord?.Release(); } catch { }
        try { _audioTrack?.Release(); } catch { }

        _audioRecord?.Dispose();
        _audioTrack?.Dispose();

        _audioRecord = null;
        _audioTrack = null;
    }
}
#endif