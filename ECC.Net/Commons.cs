using System.Security.Cryptography;

namespace ECCStandard
{
	/// <summary>
	/// Common objects to more ECC.NET classes.
	/// </summary>
	internal static class Commons
	{
		internal static RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
	}
}
