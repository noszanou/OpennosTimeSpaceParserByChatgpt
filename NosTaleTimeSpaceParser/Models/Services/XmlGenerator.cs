using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;


        namespace NosTaleTimeSpaceParser.Services
    {
        public class XmlGenerator
        {
            public string GenerateXml(ScriptedInstanceModel.Models.ScriptedInstance.ScriptedInstanceModel model)
            {
                try
                {
                    // Configurer la sérialisation pour ignorer certains attributs
                    var overrides = new XmlAttributeOverrides();

                    // Pour Item, ignorer tous les attributs sauf VNum et Amount
                    var itemAttributes = new XmlAttributes();
                    itemAttributes.XmlIgnore = false;

                    var itemType = typeof(ScriptedInstanceModel.Objects.Item);
                    foreach (var prop in itemType.GetProperties())
                    {
                        if (prop.Name != "VNum" && prop.Name != "Amount")
                        {
                            var ignoreAttr = new XmlAttributes();
                            ignoreAttr.XmlIgnore = true;
                            overrides.Add(itemType, prop.Name, ignoreAttr);
                        }
                    }

                    // Pour CreateMap, ignorer HeroXpRate
                    var createMapType = typeof(ScriptedInstanceModel.Objects.CreateMap);
                    var heroXpRateAttr = new XmlAttributes();
                    heroXpRateAttr.XmlIgnore = true;
                    overrides.Add(createMapType, "HeroXpRate", heroXpRateAttr);

                    var serializer = new XmlSerializer(typeof(ScriptedInstanceModel.Models.ScriptedInstance.ScriptedInstanceModel), overrides);

                    var settings = new XmlWriterSettings
                    {
                        Indent = true,
                        IndentChars = "\t",
                        OmitXmlDeclaration = false,
                        Encoding = Encoding.UTF8,
                        NewLineChars = "\r\n"
                    };

                    using (var stringWriter = new StringWriterWithEncoding(Encoding.UTF8))
                    {
                        using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
                        {
                            var namespaces = new XmlSerializerNamespaces();
                            namespaces.Add("", "");

                            serializer.Serialize(xmlWriter, model, namespaces);
                        }

                        return stringWriter.ToString();
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error generating XML: {ex.Message}", ex);
                }
            }

            // Helper class to specify encoding in StringWriter
            private class StringWriterWithEncoding : StringWriter
            {
                private readonly Encoding _encoding;

                public StringWriterWithEncoding(Encoding encoding) : base()
                {
                    _encoding = encoding;
                }

                public override Encoding Encoding => _encoding;
            }

            public void SaveXml(ScriptedInstanceModel.Models.ScriptedInstance.ScriptedInstanceModel model, string filePath)
            {
                var xml = GenerateXml(model);

                // Save with UTF-8 encoding
                var encoding = new UTF8Encoding(false); // false = no BOM

                // Save in both locations: output folder AND project source folder
                File.WriteAllText(filePath, xml, encoding);
                Console.WriteLine($"XML saved to: {filePath}");

                // Also save in project source folder
                string projectPath = Path.Combine("..", "..", "..", Path.GetFileName(filePath));
                try
                {
                    File.WriteAllText(projectPath, xml, encoding);
                    Console.WriteLine($"XML also saved to project: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not save to project folder: {ex.Message}");
                }
            }
        }
    }