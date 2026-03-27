using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RobloxVault.Services
{
    public static class ClientSettingsService
    {
        public static void ApplyFpsCap(bool enabled, int fps)
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var filePath = Path.Combine(localAppData, "Roblox", "GlobalBasicSettings_13.xml");
                if (!File.Exists(filePath)) return;

                var doc = XDocument.Load(filePath);
                var item = doc.Descendants("Item")
                              .FirstOrDefault(e => e.Attribute("class")?.Value == "UserGameSettings");
                if (item == null) return;

                var properties = item.Element("Properties");
                if (properties == null) return;

                var fpsElement = properties.Elements("int")
                                           .FirstOrDefault(e => e.Attribute("name")?.Value == "FramerateCap");

                if (fpsElement != null)
                    fpsElement.Value = enabled ? fps.ToString() : "240";
                else
                    properties.Add(new XElement("int",
                        new XAttribute("name", "FramerateCap"),
                        enabled ? fps.ToString() : "240"));

                doc.Save(filePath);
            }
            catch { }
        }
    }
}