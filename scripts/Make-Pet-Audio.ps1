$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Add-Type -TypeDefinition @"
using System;
using System.IO;
public static class PetCueAudio {
    public static void Write(string path, bool owl) {
        const int rate = 22050;
        double duration = owl ? 1.35 : .95;
        int count = (int)(rate * duration);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var file = new BinaryWriter(File.Create(path))) {
            file.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); file.Write(36 + count * 2);
            file.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); file.Write(16);
            file.Write((short)1); file.Write((short)1); file.Write(rate); file.Write(rate * 2);
            file.Write((short)2); file.Write((short)16); file.Write(System.Text.Encoding.ASCII.GetBytes("data")); file.Write(count * 2);
            double phase = 0;
            for (int i = 0; i < count; i++) {
                double t = i / (double)rate;
                double local = owl && t > .48 ? t - .58 : t;
                double length = owl ? (t > .48 ? .7 : .42) : duration;
                bool gap = owl && t > .42 && t < .58;
                double envelope = gap || local < 0 ? 0 : Math.Pow(Math.Sin(Math.PI * Math.Min(1, local / length)), 1.7);
                double hz = owl ? (t > .48 ? 365 : 420) + Math.Sin(t * 8) * 12 : 520 - 210 * t + Math.Sin(t * 11) * 40;
                phase += 2 * Math.PI * hz / rate;
                double tone = owl ? Math.Sin(phase) + .12 * Math.Sin(phase * 2) : Math.Sin(phase) + .30 * Math.Sin(phase * 2) + .10 * Math.Sin(phase * 3);
                file.Write((short)(tone * envelope * .22 * short.MaxValue));
            }
        }
    }
}
"@
[PetCueAudio]::Write((Join-Path $repo 'src/Pets/Owl/Assets/Audio/hoot.wav'), $true)
[PetCueAudio]::Write((Join-Path $repo 'src/Pets/Cat/Assets/Audio/mew.wav'), $false)
Write-Output 'Generated original synthesized owl and cat cues (offline PCM WAV).'