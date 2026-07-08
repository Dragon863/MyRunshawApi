using MyRunshaw.Application.Buses.Services;

namespace MyRunshaw.Application.Buses;

public interface IKmlParserService
{
    KmlParserService.ParsedRouteResult ParseKml(string kmlContent);
}