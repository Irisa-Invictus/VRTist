using System;

namespace Dada.URig
{
	public class Exception : ApplicationException
	{
		public Exception(string message) : base(message)
		{
		}
	}

	public class MissingOneOfVariantException<T> : Exception
	{
		public MissingOneOfVariantException() : base(GenerateMessage())
		{
		}

		private static string GenerateMessage()
		{
			return $"Missing oneOf variant in type '{typeof(T).Name}'.";
		}
	}

	public class InvalidAttributeNameExeption : Exception
	{
		public InvalidAttributeNameExeption(string name) : base($"Invalid attribute name '{name}'.")
		{
		}
	}
}
