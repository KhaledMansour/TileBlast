using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public enum TileColor { Blue, Green, Pink, Purple, Red, Yellow };
[System.Serializable]
public enum TileCategory { Default, GroupA, GroupB, GroupC }

[System.Serializable]
public class TileAsset
{
	public Sprite tileSprite;
	public TileCategory tileCategory;
}
[System.Serializable]
public class TilesMapping
{
	public TileColor tileColor;
	public List<TileAsset> tileMaps;
}


[CreateAssetMenu (fileName = "AssetLoader", menuName = "Assets/AssetLoader", order = 0)]
public class AssetsLoader : ScriptableObject
{
	public List<TilesMapping> tilesMappings;

	public  Sprite GetTileSprite(TileColor tileColor, TileCategory tileCategory)
	{
		var tileAsset = tilesMappings.FirstOrDefault (x => x.tileColor == tileColor);
		if (tileAsset !=null)
		{
			return tileAsset.tileMaps.FirstOrDefault (x => x.tileCategory == tileCategory).tileSprite;
		} else
		{
			Debug.LogError ("Error cant get asset check !!!!");
			return null;
		}
	}

	public TilesMapping GetRandomTile()
	{
		var randomTile = Random.Range (0, GameGridHandler.currentLevelProps.maxColorsCount);
		return tilesMappings[randomTile];
	}
}
