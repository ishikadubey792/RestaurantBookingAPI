
using Microsoft.Data.Sqlite;

namespace RestaurantBookingAPI.Data
{
    public class OsmDataInspector
    {
        public void InspectTables()
        {
            Console.WriteLine("OSM INSPECTOR STARTED");
            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Data",
                "OSM",
                "chennai_data.gpkg");

            Console.WriteLine($"GeoPackage path: {filePath}");

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return;
            }

            var connectionString =
                new SqliteConnectionStringBuilder
                {
                    DataSource = filePath,
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString();

            using var connection =
                new SqliteConnection(connectionString);

            connection.Open();

            var query = """
                SELECT name
                FROM sqlite_master
                WHERE type = 'table'
                ORDER BY name;
                """;

            using var command = new SqliteCommand(query, connection);
            using var reader = command.ExecuteReader();

            Console.WriteLine("Tables in GeoPackage:");

            while (reader.Read())
            {
                Console.WriteLine(reader.GetString(0));
            }


            Console.WriteLine("\nColumns in points table:");

            var columnQuery = "PRAGMA table_info(points);";

            using var columnCommand =
                new SqliteCommand(columnQuery, connection);

            using var columnReader = columnCommand.ExecuteReader();

            while (columnReader.Read())
            {
                Console.WriteLine(columnReader["name"]);
            }

        }
    }
}
