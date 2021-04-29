using System;
using System.IO.Ports;
using WindowsInput;
using System.Linq;
using System.Collections.Generic;

namespace ArduinoCapacitiveKeyboard
{
    class Program
    {
        private static SerialPort Arduino = new();
        private static InputSimulator Keyboard = new();
        private static IEnumerable<string> AvailablePorts;

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
            }
        }

        static bool SetupPort(string port)
        {
            // Get all ports we can use
            AvailablePorts = from available in SerialPort.GetPortNames() where available.StartsWith("COM") select available;
            // We can't use it
            if (!AvailablePorts.Contains(port))
                return false;
            // I don't know how to check if it's an arduino but oh well
            Arduino.PortName = port;
            Arduino.Open();
            return true;
        }
    }
}
