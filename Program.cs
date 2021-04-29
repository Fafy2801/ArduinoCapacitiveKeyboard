using System;
using System.IO.Ports;
using WindowsInput;
using WindowsInput.Native;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ArduinoCapacitiveKeyboard
{
    class Program
    {
        static SerialPort Arduino = new();
        static InputSimulator Simulator = new();
        static Thread MainThread;
        enum InputTypes
        {
            Command, Port
        }
        static Enum CurrentInput = InputTypes.Command;

        static IEnumerable<string> AvailablePorts;
        static List<bool> LastPressed = new();
        static List<VirtualKeyCode> Keys = new();

        // Default keys to use
        static string DefaultKeys = "DFJK";

        static void Main()
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

            SetupKeys(DefaultKeys);
            Console.WriteLine("Commands: setkeys setport");

            // Run loops
            MainThread = new(() =>
            {
                while (true)
                {
                    Loop();
                    Thread.Sleep(1);
                }
            });
            MainThread.Start();

            Task.Run(() =>
            {
                while (true)
				{
                    ConsoleLoop();
				}
            });
        }

        static void Loop()
        {
            if (!Arduino.IsOpen)
                return;

            // This will fail if the arduino was disconnected
            try
            {
                // Don't read buffer when expecting a port input
                if (CurrentInput is InputTypes.Port)
                    return;

                // We expect the keys to be sent by bits
                int inputs = Arduino.ReadByte();

                // Console is doing stuff (we're still reading buffer)
                if (CurrentInput is not InputTypes.Command)
                    return;

                for (int i = 0; i < Keys.Count; i++)
                {
                    // Key is now pressed
                    if ((inputs & (1 << i)) != 0)
                    {
                        // Key wasn't pressed before, so we press it and remember it was pressed
                        if (!LastPressed[i])
                        {
                            LastPressed[i] = true;
                            Simulator.Keyboard.KeyDown(Keys[i]);
                        }
                    }
                    // Key isn't pressed
                    else
                    {
                        // Key was pressed before, release
                        if (LastPressed[i])
                        {
                            LastPressed[i] = false;
                            Simulator.Keyboard.KeyUp(Keys[i]);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Arduino.Close();
                Console.WriteLine($"Port {Arduino.PortName} was closed. Input new port...");
                CurrentInput = InputTypes.Port;
            }
        }
        static bool SetupPort(string port)
        {
            // Get all ports we can use
            //AvailablePorts = from available in SerialPort.GetPortNames() where available.StartsWith("COM") select available;
            AvailablePorts = SerialPort.GetPortNames().Where(available => available.StartsWith("COM"));
            // We can't use it
            if (!AvailablePorts.Contains(port))
                return false;

            Arduino.Close();

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
            Keys.Clear();
            DefaultKeys = "";
 

            for (int i = 0; i < keys.Length; i++)
            {
                if (Enum.TryParse<VirtualKeyCode>($"VK_{keys[i]}".ToUpper(), out VirtualKeyCode key))
                {
                    Keys.Add(key);
                    LastPressed.Add(false);
                    DefaultKeys += keys[i];
                }
                else
                    Console.WriteLine($"Failed to parse key {keys[i]}.");
            }

            Console.WriteLine($"Now using keys {DefaultKeys} (reading {DefaultKeys.Length} bits).");
        }

        static void ConsoleLoop()
		{
            string input = Console.ReadLine();
            // We are expecting a port
            if (CurrentInput is InputTypes.Port)
			{
                bool success = SetupPort(input);
                if (success)
				{
                    Console.WriteLine($"Success! Now using port {Arduino.PortName}");
                    CurrentInput = InputTypes.Command;
                }
				else
                    Console.WriteLine("Failed to use given port. Input new port...");

                return;
			}

            string[] args = input.Split(" ");

            if (args.Length < 1)
                return;

            switch (args[0])
			{
                case "setkeys" when args.Length > 1:
                    SetupKeys(args[1]);
                    break;
                case "setkeys":
                    Console.WriteLine("Failed to setup keys. No keys given.");
                    break;
                case "setport" when args.Length > 1:
                    bool success = SetupPort(args[1]);
                    if (success)
                        Console.WriteLine("Failed to setup port.");
                    else
                        Console.WriteLine($"Success! Now using port {Arduino.PortName}");

                    break;
                case "setport":
                    Console.WriteLine("Failed to setup port. No keys given.");
                    break;
                case "help":
                    Console.WriteLine("Commands: setkeys setport");
                    break;
            }
        }
    }
}