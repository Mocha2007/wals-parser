using System.Drawing;

namespace WalsParser {

	enum Province : uint {
		EUROPE_EAST = 0xFFFFFFFF,
		BALKANS = 0xFFFF80C0,
		EUROPE_WEST = 0xFFC0FF80,
		EUROPE_NORTH = 0xFF80C0FF,
		CAUCASUS = 0xFF202020,
		SIBERIA = 0xFF404040,
		ASIA_CENTRAL = 0xFF808080,
		ASIA_EAST = 0xFFC0C0C0,
		ASIA_SOUTHWEST = 0xFFFFFF00,
		INDIA = 0xFF00FF00,
		INDOCHINA = 0xFFFFE000,
		AFRICA_NORTH = 0xFF0000FF,
		AFRICA_SUBSAHARAN = 0xFF008000,
		INDONESIA = 0xFF00C080,
		AMERICA_NORTH_NORTH = 0xFFFF0000,
		PLAINS = 0xFFC080FF,
		AMERICA_CENTRAL_PERIPHERAL = 0xFF00FFFF,
		AMERICA_SOUTH_PERIPHERAL = 0xFFFF8000,
		CASCADIA = 0xFFC0FFC0,
		CARRIBEAN = 0xFF80FF00,
		OCEANIA = 0xFFFFC080,
		NEW_GUINEA = 0xFFFF8080,
		AUSTRALIA = 0xFFFFC0FF,
		MESOAMERICA = 0xFF400080,
		AMAZON = 0xFF804080,
		AMERICA_SOUTHWEST = 0xFF804080,
		UNINHABITED = 0xFF000000,
	}
	class Region {
		public static readonly List<Region> regions = new();
		readonly string id;
		public readonly Province[] constituents;
		public Region(string id, Province[] constituents){
			this.id = id;
			this.constituents = constituents;
			regions.Add(this);
		}
		public IEnumerable<Language> languages {
			get {
				return Language.languages.Where(l => constituents.Contains(l.province));
			}
		}
		public override string ToString(){
			return $"<Region {id}>";
		}
		public static Region? FromID(string id){
			return regions.Find(r => r.id == id);
		}
		static readonly Region AFRICA = new("AFRICA", new Province[]{
			Province.AFRICA_NORTH, Province.AFRICA_SUBSAHARAN
		});
		static readonly Region AMERICA_NORTH = new("AMERICA_NORTH", new Province[]{
			Province.AMERICA_NORTH_NORTH, Province.CASCADIA, Province.CARRIBEAN, Province.AMERICA_CENTRAL_PERIPHERAL,
			Province.PLAINS, Province.MESOAMERICA, Province.AMERICA_SOUTHWEST
		});
		static readonly Region AMERICA_CENTRAL = new("AMERICA_CENTRAL", new Province[]{
			Province.AMERICA_CENTRAL_PERIPHERAL, Province.MESOAMERICA
		});
		static readonly Region AMERICA_SOUTH = new("AMERICA_SOUTH", new Province[]{Province.AMERICA_SOUTH_PERIPHERAL, Province.AMAZON});
		static readonly Region ASIA = new("ASIA", new Province[]{
			Province.SIBERIA, Province.CAUCASUS, Province.ASIA_SOUTHWEST,
			Province.ASIA_CENTRAL, Province.INDIA, Province.ASIA_EAST, Province.INDOCHINA, Province.INDONESIA
		});
		static readonly Region EUROPE = new("EUROPE", new Province[]{
			Province.EUROPE_EAST, Province.EUROPE_NORTH, Province.EUROPE_WEST, Province.BALKANS
		});
		static readonly Region OCEANIA = new("OCEANIA", new Province[]{
			Province.AUSTRALIA, Province.NEW_GUINEA, Province.OCEANIA
		});
		public static Region EARTH = regions[0]; // placeholder
	}
	static class Geo {
		const string MAP_FILENAME = "regions.png";
		static readonly Bitmap map = new(MAP_FILENAME);
		static readonly int height = map.Height;
		static readonly int width = map.Width;
		public static Province FromLatLon(double lat, double lon){
			Tuple<int, int> coords = LatLonToXY(lat, lon);
			uint answer = (uint)map.GetPixel(coords.Item1, coords.Item2).ToArgb();
			return (Province)answer;
		}
		static Tuple<int, int> LatLonToXY(double lat, double lon){
			int x = (int)((lon + 180)/360 * width);
			int y = (int)((90 - lat)/180 * height);
			return new Tuple<int, int>(x, y);
		}
	}
}