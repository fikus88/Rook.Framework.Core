namespace Rook.Framework.Core.Common
{
	public static class StringExtensions
	{
		public static string ToCamelCase(this string str)
		{
			var strCharArr = str.ToCharArray();
			string camelCase = "";

			for (int i = 0; i < strCharArr.Length; i++)
			{
				var currentChar = strCharArr[i].ToString();

				camelCase += i == 0 ? currentChar.ToLower() : currentChar;
			}

			return camelCase;
		}
	}
}