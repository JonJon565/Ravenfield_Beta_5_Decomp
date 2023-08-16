namespace Pathfinding.Serialization
{
	public class SerializeSettings
	{
		public bool nodes = true;

		public bool prettyPrint;

		public bool editorSettings;

		public static SerializeSettings Settings
		{
			get
			{
				SerializeSettings serializeSettings = new SerializeSettings();
				serializeSettings.nodes = false;
				return serializeSettings;
			}
		}

		public static SerializeSettings All
		{
			get
			{
				SerializeSettings serializeSettings = new SerializeSettings();
				serializeSettings.nodes = true;
				return serializeSettings;
			}
		}
	}
}
