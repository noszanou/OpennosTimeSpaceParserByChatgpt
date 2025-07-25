using System.IO;
using NosTaleTimeSpaceParser.Services;

namespace NosTaleTimeSpaceParser
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("NosTale Time Space Parser v2.0");
            Console.WriteLine("================================\n");

            string[] possiblePaths = {
                "packet.txt",
                Path.Combine("..", "..", "..", "packet.txt"),
                Path.Combine(Environment.CurrentDirectory, "packet.txt")
            };

            string? filePath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    filePath = path;
                    break;
                }
            }

            if (filePath == null)
            {
                Console.WriteLine("ERROR: packet.txt not found");
                Console.ReadKey();
                return;
            }

            ParseTimeSpace(filePath);

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void ParseTimeSpace(string filePath)
        {
            var lines = File.ReadAllLines(filePath).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

            var analyzer = new TimeSpaceAnalyzer();
            var model = analyzer.Analyze(lines);

            var xmlGenerator = new XmlGenerator();
            xmlGenerator.SaveXml(model, "timespace.xml");

            Console.WriteLine($"Generated XML for: {model.Globals.Name.Value}");
            Console.WriteLine($"Maps found: {model.InstanceEvents.CreateMaps.Count}");
        }
    }
}