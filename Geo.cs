namespace WalsParser {
	enum Region {
		EUROPE,
		CAUCASUS,
		SIBERIA,
		ASIA_CENTRAL,
		ASIA_EAST,
		ASIA_SOUTHWEST,
		INDIA,
		INDOCHINA,
		AFRICA_NORTH,
		AFRICA_SUBSAHARAN,
		INDONESIA,
		OCEANIA,
		AMERICA_NORTH_NORTH,
		AMERICA_CENTRAL,
		AMERICA_SOUTH,
		CARRIBEAN,
	}
	static class Geo {
		public static Region FromLatLon(double lat, double lon){
			// Western Hemisphere
			if (lon < -26){
				if (lat < 14.4){
					if (lon > -82)
						return Region.AMERICA_SOUTH;
					return Region.OCEANIA;
				}
				if (lon > -80 && lat < 25)
					return Region.CARRIBEAN;
				if (lon > -115 && lon < -80 && lat < 30)
					return Region.AMERICA_CENTRAL;
				if (lat > 40 || lon > -115)
					return Region.AMERICA_NORTH_NORTH;
				return Region.OCEANIA;
			}
			// Europe + Africa
			if (lon < 65){
				if (lat < 12)
					return Region.AFRICA_SUBSAHARAN;
				// North Africa + Middle East
				if (lat < 37){
					if (lon < 41)
						return Region.AFRICA_NORTH;
					return Region.ASIA_SOUTHWEST;
				}
				// Europe + Caucasus
				if (lon < 50){
					if (lat < 45 && lon > 45)
						return Region.CAUCASUS;
					return Region.EUROPE;
				}
				if (lat > 55)
					return Region.ASIA_CENTRAL;
				return Region.SIBERIA;
			}
			// India + other half of Central Asia
			if (lon < 93){
				if (lat < 37)
					return Region.INDIA;
				if (lat < 55)
					return Region.ASIA_CENTRAL;
				return Region.SIBERIA;
			}
			// other third of Siberia
			if (lat > 51)
				return Region.SIBERIA;
			// east easia
			if (lat > 22)
				return Region.ASIA_EAST;
			// oceania
			if (lat < -10 || lon > 130)
				return Region.OCEANIA;
			// indonesia
			if (lat < 6 || lon > 113)
				return Region.INDONESIA;
			return Region.INDOCHINA;
		}
	}
}