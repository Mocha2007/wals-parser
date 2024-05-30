using System.Drawing;

namespace WalsParser {

	enum Region : uint {
		EUROPE = 0xFFFFFFFF,
		CAUCASUS = 0xFF000000,
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
		AMERICA_CENTRAL = 0xFF00FFFF,
		AMERICA_SOUTH = 0xFFFF8000,
		CARRIBEAN = 0xFF80FF00,
		OCEANIA = 0xFFFFC080,
		NEW_GUINEA = 0xFFFF8080,
		AUSTRALIA = 0xFFFFC0FF,
	}
	static class Geo {
		const string MAP_FILENAME = "regions.png";
		static readonly Bitmap map = new Bitmap(MAP_FILENAME);
		static readonly int height = map.Height;
		static readonly int width = map.Width;
		public static Region FromLatLon(double lat, double lon){
			Tuple<int, int> coords = LatLonToXY(lat, lon);
			uint answer = (uint)map.GetPixel(coords.Item1, coords.Item2).ToArgb();
			return (Region)answer;
		}
		static Tuple<int, int> LatLonToXY(double lat, double lon){
			int x = (int)((lon + 180)/360 * width);
			int y = (int)((90 - lat)/180 * height);
			return new Tuple<int, int>(x, y);
		}
	}
}