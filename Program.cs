using System;

namespace WalsParser
{
	static class Program {
		const string PARAM_FILENAME = "../wals/raw/parameter.csv";
		const string LANG_FILENAME = "../wals/raw/language.csv";
		const string VALUE_FILENAME = "../wals/raw/value.csv";
		static void Main(string[] args){
			Load();
			Test();
		}
		static void Load(){
			foreach (string row in File.ReadAllLines(PARAM_FILENAME).Skip(1))
				Parameter.FromRow(row);
			Debug(Parameter.parameters.Count());
			foreach (string row in File.ReadAllLines(LANG_FILENAME).Skip(1))
				Language.FromRow(row);
			Debug(Language.languages.Count());
			foreach (string row in File.ReadAllLines(VALUE_FILENAME).Skip(1))
				Value.FromRow(row);
			Debug(Value.values.Count());
			foreach (Region region in Enum.GetValues<Region>())
				Debug($"{region} has {Language.GetIn(region).ToArray().Length} languages.");
		}
		public static void Debug(object o){
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write("[DEBUG] ");
			Console.WriteLine(o);
			Console.ForegroundColor = ConsoleColor.Gray;
		}
		static void Test(){
			foreach (Value parameter in Language.languages[0].parameters)
				Debug(parameter);
			Console.ReadKey();
		}

	}
	abstract class WalsCSV {
		readonly short pk;
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
		public IEnumerable<Value> parameters {
			get {
				return Value.values.Where(v => v.language == this);
			}
		}
		Region region {
			get {
				return Geo.FromLatLon(latitude, longitude);
			}
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
		public override string ToString(){
			return $"<Parameter '{id}' {name}>";
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
		readonly short valueset_pk, domainelement_pk;
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
		string id_parameter {
			get {
				return id.Split('-')[0];
			}
		}
		string id_language {
			get {
				return id.Split('-')[1];
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
			return $"<Value {language} : '{parameter}' : {valueset_pk}>";
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
}