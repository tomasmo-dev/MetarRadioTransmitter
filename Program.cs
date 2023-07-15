using System;
using System.Text;
using NAudio.Wave;

class Program
{
    static List<(BroadcastStatus, DateTime)> BroadcastStatuses = new List<(BroadcastStatus, DateTime)>();
    static BroadcastStatus LastStatus = BroadcastStatus.NotBroadcasting;

    static void Main(string[] args)
    {
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            WaveInCapabilities deviceInfo = WaveInEvent.GetCapabilities(i);
            Console.WriteLine($"Device {i}: {deviceInfo.ProductName}");
        }

        // Prompt the user to select an audio source
        Console.WriteLine("Select an audio source (enter the device number):");
        int selectedAudioSource = int.Parse(Console.ReadLine());

        // Create an instance of the WaveInEvent class
        var waveIn = new WaveInEvent();
        waveIn.DeviceNumber = selectedAudioSource;

        // Set the desired audio format
        waveIn.WaveFormat = new WaveFormat(48000, 1);

        // Set the buffer size and the number of buffers to use
        int bufferSize = 48000; // Adjust this value to suit your needs
        int seconds = 3; // store 3 seconds of audio
        waveIn.BufferMilliseconds = (int)((bufferSize * seconds) / waveIn.WaveFormat.AverageBytesPerSecond);

        // Subscribe to the DataAvailable event
        waveIn.DataAvailable += WaveIn_DataAvailable;

        // Start capturing audio
        waveIn.StartRecording();

        Console.WriteLine("Listening for clicks. Press any key to exit.");
        Console.ReadKey();

        // Stop capturing audio
        waveIn.StopRecording();
    }

    private static void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
    {
        // Convert the byte array to an array of 16-bit samples
        short[] samples = new short[e.BytesRecorded / 2];
        Buffer.BlockCopy(e.Buffer, 0, samples, 0, e.BytesRecorded);

        // Average the signal
        short[] avg_samples = AverageSignal(samples);
        // SaveToCsv(samples, avg_samples);

        // Detect Status
        DetectStatus(avg_samples);

        //if (clickCount == 3)
        //{
        //    Console.WriteLine("Three clicks detected!");
        //    // Perform any desired action to indicate the three clicks
        //}
    }

    private static void SaveToCsv(short[] samples, short[] avgSamples)
    {
        string path = @"test.csv";
        using (FileStream fs = new("test.csv", FileMode.Append))
        {   
            using (StreamWriter sw = new StreamWriter(fs))
            {

                for (int i = 0; i < samples.Length; i++)
                {
                    sw.WriteLine($"{Math.Abs(samples[i])};{Math.Abs(avgSamples[i])}");
                }
            }
        }
    }

    private static void DetectStatus(short[] samples)
    {
        const int threshold = 100; // Adjust this value to suit your needs
        var last_status = LastStatus;

        for (int i = 0; i < samples.Length; i++)
        {
            if (samples[i] >= threshold && (last_status == BroadcastStatus.NotBroadcasting || last_status == BroadcastStatus.StoppedBroadcasting))
            {
                last_status = BroadcastStatus.StartedBroadcasting;
            }else if (samples[i] >= threshold && (last_status == BroadcastStatus.Broadcasting || last_status == BroadcastStatus.StartedBroadcasting))
            {
                last_status = BroadcastStatus.Broadcasting;
            }else if (samples[i] < threshold && (last_status == BroadcastStatus.Broadcasting || last_status == BroadcastStatus.StartedBroadcasting))
            {
                last_status = BroadcastStatus.StoppedBroadcasting;
            }else if (samples[i] < threshold && (last_status == BroadcastStatus.StoppedBroadcasting))
            {
                last_status = BroadcastStatus.NotBroadcasting;
            }
        }
    }

    static short[] AverageSignal(short[] samples)
    {
        const uint kernelWidth = 3;

        short[] avg_samples = new short[samples.Length];

        for (int i = 0; i < samples.Length; i++)
        {
            short sample = samples[i];
            if (sample > 10000)
            {
                Console.WriteLine("CLick");
            }

            int avc = 0;

            for (int j = 0; j < kernelWidth; j++)
            {
                if (i + j < samples.Length)
                {
                    sample += samples[i + j];
                    avc++;
                }
                else
                {
                    for (uint k = kernelWidth; k > 0; k--)
                    {
                        if ((i + j) - k < samples.Length)
                        {
                            sample += samples[(i + j) - k];
                            avc++;
                            break;
                        }
                    }
                }

                sample /= (short)avc;
                avg_samples[i] = Math.Abs(sample);
            }
        }

        return avg_samples;
    }

    private static int DetectClicks(short[] samples)
    {

        const int threshold = 1000; // Adjust this value to suit your needs
        const int clickDuration = 10; // Number of samples to consider as a click
        int clickCount = 0;

        for (int i = 0; i < samples.Length - clickDuration; i++)
        {
            bool isClick = true;

            for (int j = 0; j < clickDuration; j++)
            {
                if (Math.Abs(samples[i + j]) < threshold)
                {
                    isClick = false;
                    break;
                }
            }

            if (isClick)
            {
                clickCount++;
                i += clickDuration - 1; // Skip the detected click duration
            }
        }

        return clickCount;
    }
}

enum BroadcastStatus
{
    StartedBroadcasting,
    StoppedBroadcasting,
    Broadcasting,
    NotBroadcasting
}