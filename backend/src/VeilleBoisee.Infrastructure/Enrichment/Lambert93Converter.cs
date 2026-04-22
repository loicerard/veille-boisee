using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace VeilleBoisee.Infrastructure.Enrichment;

internal static class Lambert93Converter
{
    // EPSG:2154 — used by IGN Géoplateforme layers stored in Lambert 93
    private const string Wkt = """
        PROJCS["RGF93 / Lambert-93",
            GEOGCS["RGF93",
                DATUM["Reseau_Geodesique_Francais_1993",
                    SPHEROID["GRS 1980",6378137,298.2572221]],
                PRIMEM["Greenwich",0],
                UNIT["degree",0.0174532925199433]],
            PROJECTION["Lambert_Conformal_Conic_2SP"],
            PARAMETER["standard_parallel_1",49],
            PARAMETER["standard_parallel_2",44],
            PARAMETER["latitude_of_origin",46.5],
            PARAMETER["central_meridian",3],
            PARAMETER["false_easting",700000],
            PARAMETER["false_northing",6600000],
            UNIT["metre",1]]
        """;

    private static readonly MathTransform Transform = BuildTransform();

    private static MathTransform BuildTransform()
    {
        var csFactory = new CoordinateSystemFactory();
        var ctFactory = new CoordinateTransformationFactory();
        var lambert93 = csFactory.CreateFromWkt(Wkt);
        return ctFactory.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, lambert93)
            .MathTransform;
    }

    public static (double X, double Y) FromWgs84(double longitude, double latitude)
    {
        var result = Transform.Transform(longitude, latitude);
        return (result.x, result.y);
    }
}
