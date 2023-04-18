using BH.Model;
using Mirror;

namespace BH.Components
{
	public static class ExtensionsCxIdReaderWriter
	{
		public static void WriteCxId(this NetworkWriter writer, CxId value)
		{
			CxId.WriteTo(writer, ref value);
		}

		public static CxId ReadCxId(this NetworkReader reader)
		{
			return CxId.ReadFrom(reader);
		}
	}
}
