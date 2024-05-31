using System.Drawing;
using System.Text.RegularExpressions;

namespace WalsParser
{
	static class Program {
		// https://stackoverflow.com/a/18147076
		public const string CSV_SPLIT_REGEX = "(?:^|,)(?=[^\"]|(\")?)\"?((?(1)(?:[^\"]|\"\")*|[^,\"]*))\"?(?=,|$)";
		const string test_lang_id = "eng"; // English
		const string region_id = "EARTH";
		const string DELEM_FILENAME = "../wals/raw/domainelement.csv";
		const string PARAM_FILENAME = "../wals/raw/parameter.csv";
		const string LANG_FILENAME = "../wals/raw/language.csv";
		const string VALUE_FILENAME = "../wals/raw/value.csv";
		static readonly Dictionary<string, Action<string>> arg_actions = new Dictionary<string, Action<string>>(){
			{"dist", TestLangDist},
			{"sprachbund", s => TestRegion(s)},
			{"typicality", TypicalRegionLang},
		};
		static void Main(string[] args){
			Load();
			// TestRegion();
			// TestLangDist();
			ParseArgs(args, arg_actions);
			// await input
			Console.ReadLine();
		}
		// little functions
		static void Debug(object o){
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write("[DEBUG] ");
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine(o);
		}
		static double ETA(long elapsed_ms, double completion){
			return elapsed_ms / completion - elapsed_ms;
		}
		static void ParseArgs(string[] args, Dictionary<string, Action<string>> actions){
			for (int i = 0; i < args.Length; i++){
				if (actions.ContainsKey(args[i]))
					actions[args[i]](args[++i]);
			}
		}
		static long Time(){
			return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
		}
		public static double Wilson(int n_s, int n){
			if (n == 0)
				return 0;
			// https://medium.com/tech-that-works/wilson-lower-bound-score-and-bayesian-approximation-for-k-star-scale-rating-to-rate-products-c67ec6e30060
			double p = (double)n_s / n;
			// st.norm.ppf(1 - (1 - confidence) / 2) where confidence = 0.95
			const double z = 1.959963984540054;
			return (p + z * z / (2 * n) - z * Math.Sqrt((p * (1 - p) + z * z / (4 * n)) / n)) / (1 + z * z / n);
		}
		// big functions
		static void Load(){
			// long t_start = Time();
			// create a new region for each province
			foreach (Province province in Enum.GetValues<Province>())
				new Region(province.ToString(), new Province[]{province});
			// create a new region with EVERY province
			Region.EARTH = new Region("EARTH", Enum.GetValues<Province>());
			// load files
			foreach (string row in File.ReadAllLines(PARAM_FILENAME).Skip(1))
				Parameter.FromRow(row);
			Debug($"Parameters: {Parameter.parameters.Count}");
			foreach (string row in File.ReadAllLines(LANG_FILENAME).Skip(1))
				Language.FromRow(row);
			Debug($"Languages: {Language.languages.Count}");
			foreach (string row in File.ReadAllLines(VALUE_FILENAME).Skip(1))
				Value.FromRow(row);
			Debug($"Values: {Value.values.Count}");
			foreach (string row in File.ReadAllLines(DELEM_FILENAME).Skip(1))
				DomainElement.FromRow(row);
			Debug($"Domain Elements: {DomainElement.domainElements.Count}");
			// Debug($"{Time() - t_start} ms");
			// region printing
			foreach (Region region in Region.regions)
				Debug($"{region} has {region.languages.ToArray().Length} languages.");
		}
		static DomainElement[] TestRegion(string id = region_id){
			Region region = Region.FromID(id) ?? Region.regions[0];
			// list parameter majorities...
			Dictionary<short, int> counts = new Dictionary<short, int>();
			List<Value> valuePopulation = Value.values
				.Where(v => {
					Language? l = v.language;
					return l is not null && region.constituents.Contains(l.province);
				})
				.ToList();
			List<DomainElement?> domainElements = new();
			foreach (Parameter p in Parameter.parameters.OrderBy(p => p.order)){
				// find valid values
				IEnumerable<Value> values = valuePopulation.Where(v => v.id_parameter == p.id);
				int sampleSize = 0;
				counts.Clear();
				foreach(Value v in values){
					sampleSize++;
					if (counts.ContainsKey(v.domainelement_pk))
						counts[v.domainelement_pk]++;
					else
						counts[v.domainelement_pk] = 1;
				}
				short majority_domainelement_pk = -1;
				foreach (short domainelement_pk in counts.Keys)
					if (2 * counts[domainelement_pk] > sampleSize){
						majority_domainelement_pk = domainelement_pk;
						break;
					}
				if (0 <= majority_domainelement_pk){
					DomainElement? d = DomainElement.FromID(majority_domainelement_pk);
					domainElements.Add(d);
					Debug($"{p} => {d} ({counts[majority_domainelement_pk]}/{sampleSize})");
				}
				else
					Debug($"{p} => no majority");
			}
			return domainElements.OfType<DomainElement>().ToArray();
		}
		static void TypicalRegionLang(string id = region_id){
			DomainElement[] domainElements = TestRegion(id);
			short[] depks = domainElements.Select(de => de.pk).ToArray();
			Language[] languages = (Region.FromID(id) ?? Region.EARTH).languages.ToArray();
			List<string> lids = languages.Select(l => l.id).ToList();
			int[] scores = new int[languages.Length];
			int[] totals = new int[languages.Length];
			Value[] values = Value.values.Where(v => lids.Contains(v.id_language)).ToArray();
			for (int i = 0; i < values.Length; i++){
				Value v = values[i];
				int j = lids.IndexOf(v.id_language);
				totals[j]++;
				if (depks.Contains(v.domainelement_pk))
					scores[j]++;
			}
			// print results
			for (int i = 0; i < languages.Length; i++)
				Debug($"Score: {Math.Round(100*Wilson(scores[i], totals[i]), 2)}% ({scores[i]}/{totals[i]}) <= {languages[i].name}");
		}
		static void TestLangDist(string id = test_lang_id){
			Debug("Testing lang dist...");
			// list lang distances from english
			Language ref_lang = Language.FromID(id) ?? Language.languages[0];
			List<Tuple<Language, double>> distances = new();
			int i = 0;
			long t_start = Time();
			Language[] population = Region.EARTH.languages.ToArray();
			foreach (Language l in population){
				Tuple<Language, double> t = new(l, ref_lang.Distance(l));
				distances.Add(t);
				// ETA
				Debug($"{++i}/{population.Length} done; ETA = {Math.Round(ETA(Time() - t_start, (double)i/population.Length)/1000)} s");
			}
			foreach (Tuple<Language, double> t in distances.OrderBy(xy => -xy.Item2))
				Debug($"{t.Item1} => {t.Item2}");
		}

	}
	abstract class WalsCSV {
		public readonly short pk;
		readonly byte version;
		public readonly string jsondata, id, name;
		readonly string description, markup_description;
		public WalsCSV(short pk, string jsondata, string id, string name,
				string description, string markup_description, byte version){
			this.pk = pk;
			this.jsondata = jsondata;
			this.id = id;
			this.name = name;
			this.description = description;
			this.markup_description = markup_description;
			this.version = version;
		}
	}
	class Language : WalsCSV {
		public static readonly List<Language> languages = new List<Language>();
		static readonly Dictionary<string, Language?> cache = new();
		readonly double latitude, longitude;
		Value[]? valueCache;
		Language(short pk, string jsondata, string id, string name,
				string description, string markup_description, double latitude,
				double longitude, byte version) : base(pk, jsondata, id, name, description, markup_description, version){
			this.latitude = latitude;
			this.longitude = longitude;
			languages.Add(this);
		}
		Value[] values {
			get {
				return valueCache ??= Value.values.Where(v => v.id_language == id).ToArray();
			}
		}
		public Province province {
			get {
				return WPImage.FromLatLon(latitude, longitude);
			}
		}
		public double Distance(Language other){
			Value[] values0 = values;
			Value[] values1 = other.values;
			// iterate over smaller array cause it's faster
			Value[] v_min = values0.Length < values1.Length ? values0 : values1;
			Value[] v_max = values0.Length < values1.Length ? values1 : values0;
			int matches = 0;
			int total = 0;
			foreach (Value v in v_min){
				try {
					Value v_ = v_max.First(v2 => v2.id_parameter == v.id_parameter);
					if (v.domainelement_pk == v_.domainelement_pk)
						matches++;
					total++;
				}
				catch (InvalidOperationException){}
			}
			return 0 < total ? Program.Wilson(matches, total) : 0;
		}
		public override string ToString(){
			return $"<Language '{id}': {name}>";
		}
		public static Language FromRow(string s){
			string[] data = s.Split(',');
			string jsondata, id, name, description, markup_description;
			short.TryParse(data[0], out short pk);
			jsondata = data[1];
			id = data[2];
			name = data[3];
			description = data[4];
			markup_description = data[5];
			double.TryParse(data[6], out double latitude);
			double.TryParse(data[7], out double longitude);
			byte.TryParse(data[8], out byte version);
			return new Language(pk, jsondata, id, name, description, markup_description, latitude, longitude, version);
		}
		public static Language? FromID(string id){
			if (cache.ContainsKey(id))
				return cache[id];
			return cache[id] = languages.Find(l => l.id == id);
		}
	}
	class Parameter : WalsCSV {
		public static readonly List<Parameter> parameters = new List<Parameter>();
		Parameter(short pk, string jsondata, string id, string name, string description, string markup_description, byte version)
				: base(pk, jsondata, id, name, description, markup_description, version){
			parameters.Add(this);
		}
		public int order {
			get {
				string[] halves = Regex.Split(id, "(?=[A-Z])");
				byte.TryParse(halves[0], out byte n);
				byte m = (byte)halves[1][0];
				return (n << 8) + m;
			}
		}
		public override string ToString(){
			return $"<Feature {id}: {name}>";
		}
		public static Parameter FromRow(string s){
			string[] data = s.Split(',');
			string jsondata, id, name, description, markup_description;
			short.TryParse(data[0], out short pk);
			jsondata = data[1];
			id = data[2];
			name = data[3];
			description = data[4];
			markup_description = data[5];
			byte.TryParse(data[6], out byte version);
			return new Parameter(pk, jsondata, id, name, description, markup_description, version);
		}
		public static Parameter? FromID(string id){
			return parameters.Find(p => p.id == id);
		}
	}
	class Value : WalsCSV {
		public static readonly List<Value> values = new List<Value>();
		public readonly short domainelement_pk;
		readonly short valueset_pk;
		readonly string frequency, confidence;
		Value(string jsondata, string id, string name, string description,
				string markup_description, short pk, short valueset_pk,
				short domainelement_pk, string frequency, string confidence,
				byte version) : base(pk, jsondata, id, name, description, markup_description, version){
			this.valueset_pk = valueset_pk;
			this.domainelement_pk = domainelement_pk;
			this.frequency = frequency;
			this.confidence = confidence;
			values.Add(this);
		}
		public string id_parameter {
			get {
				return id.Split('-')[0];
			}
		}
		public string id_language {
			get {
				return id.Split('-')[1];
			}
		}
		DomainElement? domainElement {
			get {
				return DomainElement.FromID(domainelement_pk);
			}
		}
		public Language? language {
			get {
				return Language.FromID(id_language);
			}
		}
		Parameter? parameter {
			get {
				return Parameter.FromID(id_parameter);
			}
		}
		public override string ToString(){
			return $"<Value {language} : '{parameter}' : {domainElement}>";
		}
		public static Value FromRow(string s){
			string[] data = s.Split(',');
			string jsondata, id, name, description, markup_description, frequency, confidence;
			jsondata = data[0];
			id = data[1];
			name = data[2];
			description = data[3];
			markup_description = data[4];
			short.TryParse(data[5], out short pk);
			short.TryParse(data[6], out short valueset_pk);
			short.TryParse(data[7], out short domainelement_pk);
			frequency = data[8];
			confidence = data[9];
			byte.TryParse(data[10], out byte version);
			return new Value(jsondata, id, name, description, markup_description,
				pk, valueset_pk, domainelement_pk, frequency, confidence, version);
		}
	}
	class DomainElement : WalsCSV {
		public static readonly List<DomainElement> domainElements = new List<DomainElement>();
		readonly short parameter_pk, number;
		readonly string abbr;
		DomainElement(short pk, string jsondata, string id, string name, string description,
				string markup_description, short parameter_pk, short number,
				string abbr, byte version) : base(pk, jsondata, id, name, description, markup_description, version){
			this.parameter_pk = parameter_pk;
			this.number = number;
			this.abbr = abbr;
			domainElements.Add(this);
		}
		Tuple<char, Color> icon {
			get {
				string s = Regex.Match(jsondata, "(?<= \"\").+?(?=\"\")").Value;
				char c = s[0];
				Color color = ColorTranslator.FromHtml(s[1..]);
				return new(c, color);
			}
		}
		public override string ToString(){
			return $"<{name}>";
		}
		public static DomainElement? FromID(short pk){
			return domainElements.Find(de => de.pk == pk);
		}
		public static DomainElement FromRow(string s){
			string[] data = Regex.Matches(s, Program.CSV_SPLIT_REGEX).Select(match => match.Groups[2].Value).ToArray();
			string abbr, jsondata, id, name, description, markup_description;
			short.TryParse(data[0], out short pk);
			jsondata = data[1];
			id = data[2];
			name = data[3];
			description = data[4];
			markup_description = data[5];
			short.TryParse(data[6], out short parameter_pk);
			short.TryParse(data[7], out short number);
			abbr = data[8];
			byte.TryParse(data[9], out byte version);
			return new DomainElement(pk, jsondata, id, name, description, markup_description, parameter_pk, number, abbr, version);
		}
	}
}