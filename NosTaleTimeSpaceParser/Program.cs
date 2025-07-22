using System.IO;
using NosTaleTimeSpaceParser.Parsers;
using NosTaleTimeSpaceParser.Models.PacketModels;
using NosTaleTimeSpaceParser.Services;

namespace NosTaleTimeSpaceParser
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("NosTale Time Space Parser v1.0");
            Console.WriteLine("=====================================\n");

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
                Console.WriteLine("ERROR: packet.txt not found in any of these locations:");
                foreach (var path in possiblePaths)
                {
                    Console.WriteLine($"  - {Path.GetFullPath(path)}");
                }
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            TestRealTimeSpaceFile(filePath);

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void TestRealTimeSpaceFile(string filePath)
        {
            Console.WriteLine($"Reading Time Space packets from: {filePath}");
            Console.WriteLine(new string('=', 50));

            var lines = File.ReadAllLines(filePath);
            var rbrPackets = new List<string>();
            var atPackets = new List<string>();
            var gpPackets = new List<string>();
            var inPackets = new List<string>();
            var suPackets = new List<string>();
            var msgPackets = new List<string>();
            var npcReqPackets = new List<string>();
            var walkPackets = new List<string>();
            var effPackets = new List<string>();
            var evntPackets = new List<string>();
            var rsfPackets = new List<string>();
            var preqPackets = new List<string>();
            var outPackets = new List<string>();
            var otherPackets = new List<string>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (RbrPacketParser.CanParse(trimmed))
                    rbrPackets.Add(trimmed);
                else if (AtPacketParser.CanParse(trimmed))
                    atPackets.Add(trimmed);
                else if (GpPacketParser.CanParse(trimmed))
                    gpPackets.Add(trimmed);
                else if (InPacketParser.CanParse(trimmed))
                    inPackets.Add(trimmed);
                else if (SuPacketParser.CanParse(trimmed))
                    suPackets.Add(trimmed);
                else if (MsgPacketParser.CanParse(trimmed))
                    msgPackets.Add(trimmed);
                else if (NpcReqPacketParser.CanParse(trimmed))
                    npcReqPackets.Add(trimmed);
                else if (WalkPacketParser.CanParse(trimmed))
                    walkPackets.Add(trimmed);
                else if (EffPacketParser.CanParse(trimmed))
                    effPackets.Add(trimmed);
                else if (EvntPacketParser.CanParse(trimmed))
                    evntPackets.Add(trimmed);
                else if (RsfPacketParser.CanParse(trimmed))
                    rsfPackets.Add(trimmed);
                else if (PreqPacketParser.CanParse(trimmed))
                    preqPackets.Add(trimmed);
                else if (OutPacketParser.CanParse(trimmed))
                    outPackets.Add(trimmed);
                else
                    otherPackets.Add(trimmed);
            }

            Console.WriteLine($"Found packets:");
            Console.WriteLine($"  RBR (Global info): {rbrPackets.Count}");
            Console.WriteLine($"  AT (Maps): {atPackets.Count}");
            Console.WriteLine($"  GP (Portals): {gpPackets.Count}");
            Console.WriteLine($"  IN (Entities): {inPackets.Count}");
            Console.WriteLine($"  SU (Actions): {suPackets.Count}");
            Console.WriteLine($"  MSG (Messages): {msgPackets.Count}");
            Console.WriteLine($"  NPC_REQ (Dialogs): {npcReqPackets.Count}");
            Console.WriteLine($"  WALK (Movement): {walkPackets.Count}");
            Console.WriteLine($"  EFF (Effects): {effPackets.Count}");
            Console.WriteLine($"  EVNT (Events): {evntPackets.Count}");
            Console.WriteLine($"  RSF (Minimap): {rsfPackets.Count}");
            Console.WriteLine($"  PREQ (Prerequisites): {preqPackets.Count}");
            Console.WriteLine($"  OUT (Disappear): {outPackets.Count}");
            Console.WriteLine($"  Others: {otherPackets.Count}");

            // Show unparsed packets
            if (otherPackets.Count > 0)
            {
                Console.WriteLine($"\nUNPARSED PACKETS:");
                foreach (var packet in otherPackets)
                {
                    Console.WriteLine($"  {packet}");
                }
            }

            Console.WriteLine();

            // Generate XML
            Console.WriteLine("=== GENERATING XML ===");
            var analyzer = new TimeSpaceAnalyzer();
            var model = analyzer.Analyze(lines.ToList());

            var xmlGenerator = new XmlGenerator();
            var xml = xmlGenerator.GenerateXml(model);

            Console.WriteLine("Generated XML:");
            Console.WriteLine(xml);

            // Save to file
            xmlGenerator.SaveXml(model, "generated_timespace.xml");
            Console.WriteLine();

            // Show samples of each packet type
            ShowPacketSamples("NPC_REQ", npcReqPackets, TestNpcReqParser, 5);
            ShowPacketSamples("WALK", walkPackets, TestWalkParser, 5);
            ShowPacketSamples("EFF", effPackets, TestEffParser, 5);
            ShowPacketSamples("EVNT", evntPackets, TestEvntParser, 3);
            ShowPacketSamples("RSF", rsfPackets, TestRsfParser, 8);
            ShowPacketSamples("PREQ", preqPackets, TestPreqParser, 3);
            ShowPacketSamples("OUT", outPackets, TestOutParser, 2);
        }

        static void ShowPacketSamples<T>(string name, List<string> packets, Func<string, T> parser, int count)
        {
            if (packets.Count > 0)
            {
                Console.WriteLine($"=== {name} PACKETS (First {Math.Min(count, packets.Count)}) ===");
                for (int i = 0; i < Math.Min(count, packets.Count); i++)
                {
                    try
                    {
                        var result = parser(packets[i]);
                        Console.WriteLine($"  {result}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ERROR parsing {name}: {ex.Message}");
                    }
                }
                Console.WriteLine();
            }
        }

        static NpcReqPacket TestNpcReqParser(string packet) => NpcReqPacketParser.Parse(packet);
        static WalkPacket TestWalkParser(string packet) => WalkPacketParser.Parse(packet);
        static EffPacket TestEffParser(string packet) => EffPacketParser.Parse(packet);
        static EvntPacket TestEvntParser(string packet) => EvntPacketParser.Parse(packet);
        static RsfPacket TestRsfParser(string packet) => RsfPacketParser.Parse(packet);
        static PreqPacket TestPreqParser(string packet) => PreqPacketParser.Parse(packet);
        static OutPacket TestOutParser(string packet) => OutPacketParser.Parse(packet);
    }
}