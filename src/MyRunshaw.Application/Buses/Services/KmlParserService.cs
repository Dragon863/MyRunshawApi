using System.Xml.Linq;
using MyRunshaw.Domain.Entities;

namespace MyRunshaw.Application.Buses.Services;

public class KmlParserService : IKmlParserService
{
    public class ParsedRouteResult
    {
        public string RouteDescription { get; set; } = string.Empty;
        public List<BusStop> Stops { get; set; } = new();
    }

    public ParsedRouteResult ParseKml(string kmlContent)
    {
        var result = new ParsedRouteResult();

        var doc = XDocument.Parse(kmlContent);

        XNamespace ns = "http://www.opengis.net/kml/2.2";

        var documentNode = doc.Root?.Element(ns + "Document");
        if (documentNode == null) throw new Exception("Invalid KML: Missing Document node.");

        result.RouteDescription = documentNode.Element(ns + "description")?.Value ?? "Unknown Route";

        // each route has a folder named "Bus Stops" or "Untitled layer" (thanks runshaw!) that contains the stops
        var folders = documentNode.Elements(ns + "Folder");

        // to solve this I just look for the first folder named "Bus Stops" or "Untitled layer" that ALSO contains at least one Placemark
        var busStopsFolder = folders.FirstOrDefault(f =>
            (f.Element(ns + "name")?.Value == "Bus Stops" || f.Element(ns + "name")?.Value == "Untitled layer")
            && f.Elements(ns + "Placemark").Any());

        if (busStopsFolder == null)
        {
            throw new Exception("Invalid KML: Missing Bus Stops folder or folder is empty.");
        }

        // iterate through each Placemark
        var placemarks = busStopsFolder.Elements(ns + "Placemark");

        foreach (var placemark in placemarks)
        {
            var name = placemark.Element(ns + "name")?.Value ?? "Unknown Stop";

            // coordinates are in <Point><coordinates>
            var coordinatesStr = placemark.Element(ns + "Point")?.Element(ns + "coordinates")?.Value;

            if (!string.IsNullOrWhiteSpace(coordinatesStr))
            {
                // longitude,latitude,altitude: "-3.0144,53.642799,0"
                var parts = coordinatesStr.Trim().Split(',');

                if (parts.Length >= 2 &&
                    double.TryParse(parts[0], out double longitude) &&
                    double.TryParse(parts[1], out double latitude))
                {
                    var stop = new BusStop
                    {
                        Name = name,
                        Longitude = longitude,
                        Latitude = latitude
                    };

                    result.Stops.Add(stop);
                }
            }
        }

        return result;
    }
}