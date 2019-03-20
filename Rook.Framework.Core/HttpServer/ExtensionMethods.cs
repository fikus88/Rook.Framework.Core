using System;

namespace Rook.Framework.Core.HttpServer
{
	internal static class ExtensionMethods
	{
		internal static int FindPattern<T>(this T[] candidate, params T[] pattern)
		{
			int j = 0;

			for (int i = 0; i < candidate.Length; i++)
			{
				if (candidate[i].Equals(pattern[j]))
					j++;
				else
					j = 0;

				if (j == pattern.Length)
					return i - j;
			}

			return -1;
		}

		internal static T[] SubArray<T>(this T[] candidate, int length)
		{
			T[] result = new T[length];
			Array.Copy(candidate, result, length);
			return result;
		}
	}
}
