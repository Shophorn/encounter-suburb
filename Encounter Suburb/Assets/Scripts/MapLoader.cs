using System.IO;
using UnityEngine;

public static class MapLoader
{
	/// <summary>
	/// Load all textures in a folder. File names are nicified and set as texture names
	/// </summary>
	/// <returns></returns>
	public static Texture2D[] Load()
	{
		var filePaths = Directory.GetFiles(Application.dataPath + "/Maps", "*.png");
	
		var maps = new Texture2D[filePaths.Length];
		int successCount = 0;
	
		for (int i = 0; i < filePaths.Length; i++)
		{
			Texture2D texture = new Texture2D(2,2);
			bool success = texture.LoadImage(File.ReadAllBytes(filePaths[i]));

			if (!success) continue;
		
			texture.name = ParseNameFromFilePath(filePaths[i]);
		
			maps[i] = texture;
			successCount++;
		}

		return maps;
	}

	private static string ParseNameFromFilePath(string filePath)
	{
		string name = Path.GetFileNameWithoutExtension(filePath);
		
		string [] pieces = name.Split('_');

		if (pieces.Length == 1)
		{
			return pieces[0];
		}

		// Clear name
		name = "";

		// Assume first is number, but if it is not, then add it too
		int trash;
		if (!int.TryParse(pieces[0], out trash))
		{
			name = pieces[0] + " ";
		}

		// Add all other pieces with some nice whitespace in between
		for (int i = 1; i < pieces.Length; i++)
		{
			name += pieces[i] + " ";
		}

		name = name.TrimEnd();

		return name;
	}
}