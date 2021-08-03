using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileState
{
	None, Child, Parent
}
public struct ItemIndex
{
	public int x;
	public int y;
	public ItemIndex(int x, int y)
	{
		this.x = x;
		this.y = y;
	}
}

public class BoardCell
{
	public TileBehaviour tileBehaviour;
	public int xIndex;
	public int yIndex;
	public Vector2 cellPosition;
	public BoardCell(TileBehaviour tileBehaviour, int xIndex, int yIndex, Vector2 cellPosition)
	{
		this.tileBehaviour = tileBehaviour;
		this.xIndex = xIndex;
		this.yIndex = yIndex;
		this.cellPosition = cellPosition;
	}

	public void UpdateCellTile(TileBehaviour tileBehaviour)
	{
		this.tileBehaviour = tileBehaviour;
	}
}
[System.Serializable]
public enum LevelType
{
	Example1, Example2, ShuffleTest
}
[System.Serializable]
public class LevelProps
{
	public LevelType levelType;
	public int columnItemsCount;
	public int rowItemsCount;
	[Range (1, 6)]
	public int maxColorsCount;
	[SerializeField]
	private int GroupAStartLimit;
	[SerializeField]
	private int GroupBStartLimit;
	[SerializeField]
	private int GroupCStartLimit;
	public TileCategory GetTileCategory(int matchesCount)
	{
		if (matchesCount < GroupAStartLimit)
		{
			return TileCategory.Default;
		}
		else if (matchesCount >= GroupAStartLimit && matchesCount < GroupBStartLimit)
		{
			return TileCategory.GroupA;
		}
		else if (matchesCount >= GroupBStartLimit && matchesCount < GroupCStartLimit)
		{
			return TileCategory.GroupB;
		}
		else
		{
			return TileCategory.GroupC;
		}
	}
}
