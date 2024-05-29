using System;

namespace WalsParser
{
	static class Program {
		const string PARAM_FILENAME = "../wals/raw/parameter.csv";
		const string LANG_FILENAME = "../wals/raw/language.csv";
		const string VALUE_FILENAME = "../wals/raw/value.csv";
		static void Main(string[] args){
			Load();
		}
		static void Load(){
			foreach (string row in File.ReadAllLines(PARAM_FILENAME))
				Parameter.FromRow(row);
			foreach (string row in File.ReadAllLines(LANG_FILENAME))
				Language.FromRow(row);
			foreach (string row in File.ReadAllLines(VALUE_FILENAME))
				Value.FromRow(row);
		}

	}
	abstract class WalsCSV {
		readonly short pk;
		readonly byte version;
		readonly string jsondata, id, name, description, markup_description;
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
	}
	class Parameter : WalsCSV {
		public static readonly List<Parameter> parameters = new List<Parameter>();
		Parameter(short pk, string jsondata, string id, string name, string description, string markup_description, byte version)
				: base(pk, jsondata, id, name, description, markup_description, version){
			parameters.Add(this);
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