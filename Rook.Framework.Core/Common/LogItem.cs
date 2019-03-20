using System;

namespace Rook.Framework.Core.Common
{
	public class LogItem
	{
		public string Key;
		public Func<string> Value;

		public LogItem(string key, Func<string> value)
		{
			Key = key;
			Value = value;
		}

		public LogItem(string key, string value)
		{
			Key = key;
			Value = () => value;
		}

		public LogItem(string key, long value)
		{
			Key = key;
			Value = value.ToString;
		}

		public LogItem(string key, double value)
		{
			Key = key;
			Value = value.ToString;
		}

		public LogItem(string key, decimal value)
		{
			Key = key;
			Value = value.ToString;
		}

		public LogItem(string key, ulong value)
		{
			Key = key;
			Value = value.ToString;
		}
	}
}