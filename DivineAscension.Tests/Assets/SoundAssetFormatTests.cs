using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Xunit;

namespace DivineAscension.Tests.Assets;

/// <summary>
///     Guards the on-disk encoding of UI sound assets. Vintage Story's
///     <c>PlaySoundAt</c> silently drops the mono <c>writing.ogg</c> on the shipped
///     client while every stereo clip plays — so the unlock SFX never sounded.
///     Keep the unlock sound stereo (matching the other working clips) so it stays audible.
/// </summary>
[ExcludeFromCodeCoverage]
public class SoundAssetFormatTests
{
    private static string SoundsDir =>
        Path.Combine(AppContext.BaseDirectory, "TestAssets", "sounds");

    [Fact]
    public void WritingUnlockSound_IsStereo()
    {
        var path = Path.Combine(SoundsDir, "writing.ogg");
        Assert.True(File.Exists(path), $"Missing sound asset: {path}");

        var channels = ReadVorbisChannelCount(File.ReadAllBytes(path));

        Assert.True(channels >= 2,
            $"writing.ogg must be stereo to play via PlaySoundAt; found {channels} channel(s).");
    }

    /// <summary>
    ///     Reads the audio channel count from an Ogg Vorbis file by locating the
    ///     Vorbis identification header packet (<c>0x01 "vorbis"</c>); the channel
    ///     byte sits 11 bytes after the packet-type marker.
    /// </summary>
    private static int ReadVorbisChannelCount(byte[] data)
    {
        ReadOnlySpan<byte> signature = "vorbis"u8;
        for (var i = 0; i < data.Length - 12; i++)
        {
            if (data[i] != 0x01) continue;
            if (!data.AsSpan(i + 1, signature.Length).SequenceEqual(signature)) continue;
            // layout: 0x01, "vorbis", vorbis_version(4), audio_channels(1)
            return data[i + 11];
        }

        throw new InvalidOperationException("Vorbis identification header not found.");
    }
}
