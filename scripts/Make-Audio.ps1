$ErrorActionPreference = 'Stop'
$assetDir = Join-Path (Split-Path -Parent $PSScriptRoot) 'src/Pets/Dragon/Assets/Audio'
New-Item -ItemType Directory -Path $assetDir -Force | Out-Null
Add-Type -TypeDefinition @'
using System;
using System.IO;
public static class DragonAudio {
    public static void Roar(string path) {
        const int rate = 22050;
        const double seconds = 2.3;
        int samples = (int)(rate * seconds);
        using (var w = new BinaryWriter(File.Create(path))) {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + samples * 2);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16);
            w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2);
            w.Write((short)2); w.Write((short)16); w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(samples * 2);
            var random = new Random(17); double phase = 0, low = 0;
            for (int i = 0; i < samples; i++) {
                double t = i / (double)rate;
                double envelope = Math.Min(1, t / .18) * Math.Pow(Math.Max(0, 1 - t / seconds), .65);
                double hz = 72 - 27 * t / seconds + 6 * Math.Sin(t * 14);
                phase += 2 * Math.PI * hz / rate;
                low = .88 * low + .12 * (random.NextDouble() * 2 - 1);
                double growl = .38 * Math.Sin(phase) + .21 * Math.Sin(phase * 2.01) + .14 * Math.Sin(phase * 3) + .08 * Math.Sin(phase * 7) + low * 1.3;
                double flutter = .8 + .2 * Math.Sin(t * 39);
                double value = Math.Tanh(growl * 1.5) * envelope * flutter * .65;
                w.Write((short)(value * short.MaxValue));
            }
        }
    }
    public static void LowerPitch(string path) {
        byte[] bytes = File.ReadAllBytes(path);
        if (System.Text.Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF") throw new Exception("Not a WAV file");
        for (int offset = 12; offset + 8 <= bytes.Length;) {
            int size = BitConverter.ToInt32(bytes, offset + 4);
            if (System.Text.Encoding.ASCII.GetString(bytes, offset, 4) == "fmt ") {
                if (BitConverter.ToInt16(bytes, offset + 8) != 1) throw new Exception("Expected PCM");
                int rate = (int)(BitConverter.ToInt32(bytes, offset + 12) * .84);
                short align = BitConverter.ToInt16(bytes, offset + 20);
                Array.Copy(BitConverter.GetBytes(rate), 0, bytes, offset + 12, 4);
                Array.Copy(BitConverter.GetBytes(rate * align), 0, bytes, offset + 16, 4);
                File.WriteAllBytes(path, bytes); return;
            }
            offset += 8 + size + (size % 2);
        }
        throw new Exception("Missing WAV format");
    }
}
'@
[DragonAudio]::Roar((Join-Path $assetDir 'roar.wav'))
$voice = New-Object -ComObject SAPI.SpVoice
try {
    $voices = $voice.GetVoices()
    for ($index = 0; $index -lt $voices.Count; $index++) {
        $candidate = $voices.Item($index)
        if ($candidate.GetDescription() -match 'David|Mark|Male') { $voice.Voice = $candidate; break }
    }
    $voice.Rate = -1
    $voice.Volume = 90
    $lines = @{
        'water.wav' = 'It is time. Drink some water. Take a breath. Your next task can wait.'
        'stretch.wav' = 'Rise. Stretch your body, and move a little. Return stronger. I will be waiting.'
    }
    foreach ($name in $lines.Keys) {
        $path = Join-Path $assetDir $name
        $stream = New-Object -ComObject SAPI.SpFileStream
        try {
            $stream.Format.Type = 22
            $stream.Open($path, 3, $false)
            $voice.AudioOutputStream = $stream
            $voice.Speak("<pitch absmiddle='-5'>$($lines[$name])</pitch>", 8) | Out-Null
            $stream.Close()
        } finally { [Runtime.InteropServices.Marshal]::FinalReleaseComObject($stream) | Out-Null }
        [DragonAudio]::LowerPitch($path)
    }
    Write-Output "Generated offline voice clips using: $($voice.Voice.GetDescription())"
    Write-Output 'Generated a synthetic bass roar. All audio is stored locally.'
} finally { [Runtime.InteropServices.Marshal]::FinalReleaseComObject($voice) | Out-Null }
