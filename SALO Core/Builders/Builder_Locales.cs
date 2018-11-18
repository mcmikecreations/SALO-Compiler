using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;
using SALO_Core.Builders.Settings;
using SALO_Core.Exceptions;

namespace SALO_Core.Builders.Settings
{
	public class NamesTaken
	{
		public string lang { get; set; }
		public List<string> names { get; set; }
		public List<object> cmds { get; set; }
		public List<object> files { get; set; }
	}
	public class NamesTranslated
	{
		public string locale { get; set; }
		public string translated { get; set; }
	}
}

namespace SALO_Core.Builders
{
	public class Builder_Locales
	{
		public List<NamesTaken> names_taken { get; set; }
		public List<NamesTranslated> names_translated { get; set; }
		public static Builder_Locales FromFile(string path)
		{
			if (!File.Exists(path))
			{
				throw new SALO_Exception("LocaleStrings file not found", new FileNotFoundException());
			}
			string res = File.ReadAllText(path);
			Builder_Locales result = null;
			try
			{
				result = JsonConvert.DeserializeObject<Builder_Locales>(res);
			}
			catch(Exception e)
			{
				throw new SALO_Exception("Error parsing locale settings", e);
			}
			return result;
		}
	}
}
