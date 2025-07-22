using System.Text;
using System.Xml;
using System.Xml.Serialization;
using NosTaleTimeSpaceParser.Models.XmlModels;

namespace NosTaleTimeSpaceParser.Services
{
    public class XmlGenerator
    {
        public string GenerateXml(ScriptedInstanceModel model)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(ScriptedInstanceModel));

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "\t",
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = false
                };

                using (var stream = new MemoryStream())
                {
                    using (var writer = XmlWriter.Create(stream, settings))
                    {
                        var namespaces = new XmlSerializerNamespaces();
                        namespaces.Add("", ""); // Remove default namespaces

                        serializer.Serialize(writer, model, namespaces);
                    }

                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating XML: {ex.Message}", ex);
            }
        }

        public void SaveXml(ScriptedInstanceModel model, string filePath)
        {
            var xml = GenerateXml(model);

            // Save in both locations: output folder AND project source folder
            File.WriteAllText(filePath, xml, Encoding.UTF8);
            Console.WriteLine($"XML saved to: {filePath}");

            // Also save in project source folder
            string projectPath = Path.Combine("..", "..", "..", Path.GetFileName(filePath));
            try
            {
                File.WriteAllText(projectPath, xml, Encoding.UTF8);
                Console.WriteLine($"XML also saved to project: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not save to project folder: {ex.Message}");
            }
        }
    }
}