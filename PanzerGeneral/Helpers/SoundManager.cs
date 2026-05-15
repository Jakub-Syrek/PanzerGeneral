using System;
using System.IO;
using System.Media;

namespace PanzerGeneral
{
    public static class SoundManager
    {
        private static readonly SoundPlayer _marchPlayer   = Build(MarchSamples());
        private static readonly SoundPlayer _vehiclePlayer = Build(VehicleSamples());
        private static readonly SoundPlayer _attackPlayer  = Build(AttackSamples());

        public static void PlayMarch()   => _marchPlayer.Play();
        public static void PlayVehicle() => _vehiclePlayer.Play();
        public static void PlayAttack()  => _attackPlayer.Play();

        // ---------- sample generators ----------

        // Dwa krótkie uderzenia stopy — "left, right"
        private static short[] MarchSamples()
        {
            const int sr = 22050;
            const int len = sr * 28 / 100; // 280ms
            var s = new short[len];

            foreach (int off in new[] { 0, sr * 13 / 100 })
            {
                int dur = sr * 9 / 100;
                for (int i = 0; i < dur && off + i < len; i++)
                {
                    double t = (double)i / sr;
                    double env = Math.Exp(-t * 35);
                    double f   = 160 * Math.Exp(-t * 25); // pitch drop
                    double v   = env * Math.Sin(2 * Math.PI * f * t) * 0.65;
                    // krótki klik na początku uderzenia
                    double click = i < 4 ? 0.25 * (1 - i / 4.0) : 0;
                    s[off + i] = Pcm(v + click);
                }
            }
            return s;
        }

        // Niski warkot silnika z fluktuacją obrotów
        private static short[] VehicleSamples()
        {
            const int sr = 22050;
            const int len = sr * 45 / 100; // 450ms
            var s = new short[len];
            var rng = new Random(7);

            for (int i = 0; i < len; i++)
            {
                double t   = (double)i / sr;
                double env = Math.Min(1.0, t * 8) * Math.Max(0.0, 1 - t * 2.2);

                // obracający się silnik z lekką modulacją
                double rpm = 80 + 12 * Math.Sin(2 * Math.PI * 6 * t);
                double v   = 0.38 * Math.Sin(2 * Math.PI * rpm * t)
                           + 0.22 * Math.Sin(2 * Math.PI * rpm * 2 * t)
                           + 0.12 * Math.Sin(2 * Math.PI * rpm * 3 * t)
                           + 0.08 * (rng.NextDouble() * 2 - 1); // szum gąsienic

                s[i] = Pcm(env * v * 0.75);
            }
            return s;
        }

        // Strzał: głośny trzask (szum + wysoki sweep) + niski boom
        private static short[] AttackSamples()
        {
            const int sr = 22050;
            const int len = sr * 32 / 100; // 320ms
            var s = new short[len];
            var rng = new Random(42);

            for (int i = 0; i < len; i++)
            {
                double t = (double)i / sr;

                // Trzask - biały szum z bardzo szybkim zanikiem
                double crack     = (rng.NextDouble() * 2 - 1) * Math.Exp(-t * 60) * 0.5;
                // Wysokie "whip" — sweep w dół
                double whipFreq  = 2400 * Math.Exp(-t * 30);
                double whip      = Math.Sin(2 * Math.PI * whipFreq * t) * Math.Exp(-t * 35) * 0.35;
                // Niski boom
                double boom      = Math.Sin(2 * Math.PI * 72 * t) * Math.Exp(-t * 18) * 0.45;
                // Średni "body" strzału
                double body      = (rng.NextDouble() * 2 - 1) * Math.Exp(-t * 20) * 0.25;

                s[i] = Pcm(crack + whip + boom + body);
            }
            return s;
        }

        // ---------- helpers ----------

        private static SoundPlayer Build(short[] samples)
        {
            var stream = new MemoryStream(ToWav(samples));
            var player = new SoundPlayer(stream);
            player.Load();
            return player;
        }

        private static short Pcm(double v) =>
            (short)(Math.Max(-1.0, Math.Min(1.0, v)) * short.MaxValue);

        private static byte[] ToWav(short[] samples, int sr = 22050)
        {
            using var ms = new MemoryStream();
            using var w  = new BinaryWriter(ms);
            int data = samples.Length * 2;

            w.Write(new byte[] { 0x52,0x49,0x46,0x46 }); // "RIFF"
            w.Write(36 + data);
            w.Write(new byte[] { 0x57,0x41,0x56,0x45 }); // "WAVE"
            w.Write(new byte[] { 0x66,0x6D,0x74,0x20 }); // "fmt "
            w.Write(16);
            w.Write((short)1);      // PCM
            w.Write((short)1);      // mono
            w.Write(sr);
            w.Write(sr * 2);        // byte rate
            w.Write((short)2);      // block align
            w.Write((short)16);     // bits per sample
            w.Write(new byte[] { 0x64,0x61,0x74,0x61 }); // "data"
            w.Write(data);
            foreach (var s in samples) w.Write(s);

            return ms.ToArray();
        }
    }
}
