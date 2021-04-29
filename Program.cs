using System;
using System.IO.Ports;
using WindowsInput;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace ArduinoCapacitiveKeyboard
{
    class Program
    {
        static SerialPort Arduino = new();
        static InputSimulator Keyboard = new();
        static Thread MainThread;

        static IEnumerable<string> AvailablePorts;
        static List<bool> LastPressed = new();

        // Default keys to use
        private static string Keys = "DFJK";

        static void Main(string[] args)
        {
            // We attempt to use COM3, because Arduino seems to always be there
            Console.WriteLine("Attempting to use COM3");
            bool succeeded = SetupPort("COM3");
            // Otherwise we try to use another port
            while (!succeeded)
            {
                Console.WriteLine("Failed to use given port. Input new port...");
                succeeded = SetupPort(Console.ReadLine());

                if (succeeded)
                    Console.WriteLine($"Success! Now using port {Arduino.PortName}");
            }

            Console.WriteLine($"Using default keys {Keys} (reading {Keys.Length} bits).");
            SetupKeys(Keys);

            // Run loop
            MainThread = new(() =>
            {
                while (true)
                {
                    Loop();
                    Thread.Sleep(1);
                }
            });
            MainThread.Start();
        }

        static void Loop()
        {
            // This will fail if the arduino was disconnected
            try
            {
                // We expect the keys to be sent by bits
                int inputs = Arduino.ReadByte();

                for(int i = 0; i < Keys.Length; i++)
                {
                    // Key is now pressed
                    if ((inputs & (1 << i)) != 0)
                    {
                        // Key wasn't pressed before, so we press it and remember it was pressed
                        if (!LastPressed[i])
                        {
                            LastPressed[i] = true;
                        }
                    }
                    // Key isn't pressed
                    else
                    {
                        // Key was pressed before, release
                        if (LastPressed[i])
                        {
                            LastPressed[i] = false;
                        }
                            
                    }
                }
            }
            catch (Exception)
            {
                Arduino.Close();
                Console.WriteLine($"Port {Arduino.PortName} was closed. Input new port...");
                bool succeeded = SetupPort(Console.ReadLine());

                while (!succeeded)
                {
                    Console.WriteLine("Failed to use given port. Input new port...");
                    succeeded = SetupPort(Console.ReadLine());

                    if (succeeded)
                        Console.WriteLine($"Success! Now using port {Arduino.PortName}");
                }
            }
        }
        static bool SetupPort(string port)
        {
            // Get all ports we can use
            AvailablePorts = from available in SerialPort.GetPortNames() where available.StartsWith("COM") select available;
            // We can't use it
            if (!AvailablePorts.Contains(port))
                return false;

            Arduino.PortName = port;
            // For some reason, if the arduino was disconnected GetPortNames will still return it
            try {
                Arduino.Open();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        static void SetupKeys(string keys)
        {
            LastPressed.Clear();
            // Flag as not pressed (because we just added it)
            LastPressed.AddRange(from char key in keys select false);
        }
    }
}
