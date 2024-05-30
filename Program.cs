using System.Text.RegularExpressions;

namespace WalsParser
{
	static class Program {
		const string test_lang_id = "eng"; // English
		const Region region = Region.EUROPE;
		const string DELEM_FILENAME = "../wals/raw/domainelement.csv";
		const string PARAM_FILENAME = "../wals/raw/parameter.csv";
		const string LANG_FILENAME = "../wals/raw/language.csv";
		const string VALUE_FILENAME = "../wals/raw/value.csv";
		static void Main(string[] args){
			Load();
			TestLangDist();
		}
		public static void Debug(object o){
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write("[DEBUG] ");
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine(o);
		}
		static double ETA(long elapsed_seconds, double completion){
			return elapsed_seconds / completion - elapsed_seconds;
		}
		static long Time(){
			return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
		}
		static void Load(){
			foreach (string row in File.ReadAllLines(PARAM_FILENAME).Skip(1))
				Parameter.FromRow(row);
			Debug($"Parameters: {Parameter.parameters.Count()}");
			foreach (string row in File.ReadAllLines(LANG_FILENAME).Skip(1))
				Language.FromRow(row);
			Debug($"Languages: {Language.languages.Count()}");
			foreach (string row in File.ReadAllLines(VALUE_FILENAME).Skip(1))
				Value.FromRow(row);
			Debug($"Values: {Value.values.Count()}");
			foreach (string row in File.ReadAllLines(DELEM_FILENAME).Skip(1))
				DomainElement.FromRow(row);
			Debug($"Domain Elements: {DomainElement.domainElements.Count()}");
			// region printing
			foreach (Region region in Enum.GetValues<Region>())
				Debug($"{region} has {Language.GetIn(region).ToArray().Length} languages.");
		}
		static void TestRegion(){
			// get all languages in europe...
			// foreach(Language l in Language.languages.Where(l => l.region == region))
			// 	Debug($"${l} is in {region}");
			// list parameter majorities...
			Dictionary<DomainElement, int> counts = new Dictionary<DomainElement, int>();
			List<Value> valuePopulation = Value.values.Where(v => v.language?.region == region).ToList();
			foreach (Parameter p in Parameter.parameters.OrderBy(p => p.order)){
				// find valid values
				IEnumerable<Value> values = valuePopulation.Where(v => v.id_parameter == p.id);
				int sampleSize = 0;
				counts.Clear();
				foreach(Value v in values){
					sampleSize++;
					DomainElement domainElement = v.domainElement;
					if (counts.ContainsKey(domainElement))
						counts[domainElement]++;
					else
						counts[domainElement] = 1;
				}
				DomainElement? majority = null;
				foreach (DomainElement key in counts.Keys)
					if (2 * counts[key] > sampleSize){
						majority = key;
						break;
					}
				if (majority is not null)
					Debug($"{p} => {majority} ({counts[majority]}/{sampleSize})");
				else
					Debug($"{p} => no majority");
			}
		}
		static void TestLangDist(){
			Debug("Testing lang dist...");
			// list lang distances from english
			Language ref_lang = Language.FromID(test_lang_id) ?? Language.languages[0];
			List<Tuple<Language, double>> distances = new List<Tuple<Language, double>>();
			int i = 0;
			long t_start = Time();
			Language[] population = Language.languages.Where(l => l.region == region).ToArray();
			foreach (Language l in population){
				Tuple<Language, double> t = new Tuple<Language, double>(l, ref_lang.Distance(l));
				distances.Add(t);
				// ETA
				Debug($"{++i}/{population.Length} done; ETA = {Math.Round(ETA(Time() - t_start, (double)i/population.Length))} s");
			}
			foreach (Tuple<Language, double> t in distances.OrderBy(xy => -xy.Item2))
				Debug($"{t.Item1} => {t.Item2}");
			// await input
			Console.ReadLine();
		}

	}
	abstract class WalsCSV {
		public readonly short pk;
		readonly byte version;
		public readonly string jsondata, id, name, description, markup_description;
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
		readonly double latitude, longitude;
		Language(short pk, string jsondata, string id, string name,
				string description, string markup_description, double latitude,
				double longitude, byte version) : base(pk, jsondata, id, name, description, markup_description, version){
			this.latitude = latitude;
			this.longitude = longitude;
			languages.Add(this);
		}
		public IEnumerable<Value> values {
			get {
				return Value.values.Where(v => v.id_language == id);
			}
		}
		public Region region {
			get {
				return Geo.FromLatLon(latitude, longitude);
			}
		}
		public double Distance(Language other){
			Value[] values0 = values.ToArray();
			Value[] values1 = other.values.ToArray();
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
			return 0 < total ? (double)matches / total : 0;
		}
		public override string ToString(){
			return $"<Language '{id}': {name}>";
		}
		public static Language FromRow(string s){
			string[] data = s.Split(',');
			short pk;
			byte version;
			string jsondata, id, name, description, markup_description;
			double latitude, longitude;
			short.TryParse(data[0], out pk);
			jsondata = data[1];
			id = data[2];
			name = data[3];
			description = data[4];
			markup_description = data[5];
			double.TryParse(data[6], out latitude);
			double.TryParse(data[7], out longitude);
			byte.TryParse(data[8], out version);
			return new Language(pk, jsondata, id, name, description, markup_description, latitude, longitude, version);
		}
		public static IEnumerable<Language> GetIn(Region region){
			return languages.Where(l => l.region == region);
		}
		public static Language? FromID(string id){
			return languages.Find(l => l.id == id);
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
				byte n;
				byte.TryParse(halves[0], out n);
				byte m = (byte)halves[1][0];
				return (n << 8) + m;
			}
		}
		public override string ToString(){
			return $"<Feature {id}: {name}>";
		}
		public static Parameter FromRow(string s){
			string[] data = s.Split(',');
			short pk;
			byte version;
			string jsondata, id, name, description, markup_description;
			short.TryParse(data[0], out pk);
			jsondata = data[1];
			id = data[2];
			name = data[3];
			description = data[4];
			markup_description = data[5];
			byte.TryParse(data[6], out version);
			return new Parameter(pk, jsondata, id, name, description, markup_description, version);
		}
		public static Parameter? FromID(string id){
			return parameters.Find(p => p.id == id);
		}
	}
	class Value : WalsCSV {
		public static readonly List<Value> values = new List<Value>();
		public readonly short valueset_pk, domainelement_pk;
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
		public DomainElement? domainElement {
			get {
				return DomainElement.FromID(domainelement_pk);
			}
		}
		public Language? language {
			get {
				return Language.FromID(id_language);
			}
		}
		public Parameter? parameter {
			get {
				return Parameter.FromID(id_parameter);
			}
		}
		public override string ToString(){
			return $"<Value {language} : '{parameter}' : {domainElement}>";
		}
		public static Value FromRow(string s){
			string[] data = s.Split(',');
			short pk, valueset_pk, domainelement_pk;
			byte version;
			string jsondata, id, name, description, markup_description, frequency, confidence;
			jsondata = data[0];
			id = data[1];
			name = data[2];
			description = data[3];
			markup_description = data[4];
			short.TryParse(data[5], out pk);
			short.TryParse(data[6], out valueset_pk);
			short.TryParse(data[7], out domainelement_pk);
			frequency = data[8];
			confidence = data[9];
			byte.TryParse(data[10], out version);
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
		public override string ToString(){
			return $"<{name}>";
		}
		public static DomainElement? FromID(short pk){
			return domainElements.Find(de => de.pk == pk);
		}
		public static DomainElement FromRow(string s){
			string[] data = s.Split(',');
			short pk, parameter_pk, number;
			byte version;
			string abbr, jsondata, id, name, description, markup_description;
			short.TryParse(data[0], out pk);
			jsondata = data[1];
			id = data[2];
			name = data[3];
			description = data[4];
			markup_description = data[5];
			short.TryParse(data[6], out parameter_pk);
			short.TryParse(data[7], out number);
			abbr = data[8];
			byte.TryParse(data[9], out version);
			return new DomainElement(pk, jsondata, id, name, description, markup_description, parameter_pk, number, abbr, version);
		}
	}
}