using SkiaSharp;

// image handling
namespace WalsParser {
	static class WPImage {
		const string MAP_FILENAME = "regions.png";
		const string COUNTRYMAP_FILENAME = "countries.png";
		// https://stackoverflow.com/a/65820579/2579798
		static readonly SKBitmap map = SKBitmap.FromImage(SKImage.FromEncodedData(MAP_FILENAME));
		static readonly SKBitmap countrymap = SKBitmap.FromImage(SKImage.FromEncodedData(COUNTRYMAP_FILENAME));
		static readonly int height = map.Height;
		static readonly int width = map.Width;
		public static Country CountryFromLatLon(double lat, double lon){
			Tuple<int, int> coords = LatLonToXY(lat, lon);
			uint answer = (uint)countrymap.GetPixel(coords.Item1, coords.Item2);
			return (Country)answer;
		}
		public static Province FromLatLon(double lat, double lon){
			Tuple<int, int> coords = LatLonToXY(lat, lon);
			uint answer = (uint)map.GetPixel(coords.Item1, coords.Item2);
			return (Province)answer;
		}
		static Tuple<int, int> LatLonToXY(double lat, double lon){
			int x = (int)((lon + 180)/360 * width);
			int y = (int)((90 - lat)/180 * height);
			return new Tuple<int, int>(x, y);
		}
	}
}