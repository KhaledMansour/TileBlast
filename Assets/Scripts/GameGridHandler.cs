using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TileState
{
	None, Child, Parent
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
	[Range (0, 6)]
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

public class GameGridHandler : MonoBehaviour
{
	[SerializeField]
	private float screenWidth;
	[SerializeField]
	private float screenHeight;
	[SerializeField]
	private int columnItemsCount;
	[SerializeField]
	private int rowItemsCount;
	[SerializeField]
	private List<LevelProps> levelProps;
	[SerializeField]
	private LevelType levelType;
	public static LevelProps currentLevelProps;
	[SerializeField]
	private Transform spawnPoint;
	[SerializeField]
	private float xOffsetPercentage = 40;
	[SerializeField]
	private float yOffsetPercentage = 10;
	[SerializeField]
	private GameObject blockPrefab;
	[SerializeField]
	private AssetsLoader assetCategory;
	public static BoardCell[,] gameBoard;
	private Vector3 tileScale;

	void Awake()
	{
		currentLevelProps = levelProps.First (x => x.levelType == levelType);
		columnItemsCount = currentLevelProps.columnItemsCount;
		rowItemsCount = currentLevelProps.rowItemsCount;
		InitTilesGrid ();
	}

	void Update()
	{
		if (Input.GetKeyDown (KeyCode.M))
		{
			CheckTileMatches ();
		}
		//if (Input.GetKeyDown (KeyCode.S))
		//{
		//	Debug.LogError ("need shuffle" + Shuffle ());
		//}

	}

	//bool Shuffle()
	//{
	//	for (int y = 0; y < columnItemsCount; y++)
	//	{
	//		for (int x = 0; x < rowItemsCount; x++)
	//		{
	//			if (gameBoard[x, y].tileBehaviour)
	//			{
	//				if (gameBoard[x, y].tileBehaviour.IfParentTile())
	//				{
	//					return false;
	//				}
	//			}
	//		}
	//	}
	//	return true;
	//}

	void InitTilesGrid()
	{
		//screenHeight = Camera.main.orthographicSize * 2;
		//screenWidth = screenHeight * Screen.width / Screen.height;
		//screenHeight = 7;
		gameBoard = new BoardCell[rowItemsCount, columnItemsCount];
		var xItemSize = screenWidth / (rowItemsCount + (rowItemsCount + 1) / xOffsetPercentage);
		var yItemSize = screenHeight / (columnItemsCount + (columnItemsCount + 1) / yOffsetPercentage);
		tileScale = new Vector3 (xItemSize, yItemSize, 1);
		var startXPos = -screenWidth / 2 + xItemSize / 2 + xItemSize / xOffsetPercentage;
		var startYPos = -screenHeight / 2 + yItemSize / 2 + yItemSize / yOffsetPercentage;

		for (int y = 0; y < columnItemsCount; y++)
		{
			for (int x = 0; x < rowItemsCount; x++)
			{

				var xPos = (x * xItemSize + x * (xItemSize / xOffsetPercentage) + startXPos);
				var yPos = (y * yItemSize + y * (yItemSize / yOffsetPercentage) + startYPos);
				var tile = SpawnRandomTile (new Vector3 (xPos, yPos, 0), x, y);
				var cell = new BoardCell (tile, x, y, new Vector2 (xPos, yPos));
				gameBoard[x, y] = cell;
			}
		}
		CheckTileMatches ();
	}

	private void CheckTileMatches()
	{
		for (int y = 0; y < columnItemsCount; y++)
		{
			for (int x = 0; x < rowItemsCount; x++)
			{
				if (gameBoard[x, y].tileBehaviour)
				{
					gameBoard[x, y].tileBehaviour.ResetTileProps ();
				}
			}
		}
		for (int y = 0; y < columnItemsCount; y++)
		{
			for (int x = 0; x < rowItemsCount; x++)
			{
				if (gameBoard[x, y].tileBehaviour)
				{
					gameBoard[x, y].tileBehaviour.CheckForNeighbours ();
				}
			}
		}
	}

	void OnTileDestroyed(List<TileBehaviour> tiles)
	{
		var destroyedItemsRow = new List<int> ();
		foreach (var tile in tiles)
		{
			var destroyedTileCell = gameBoard[tile.xIndex, tile.yIndex];
			Destroy (destroyedTileCell.tileBehaviour.gameObject);
			if (!destroyedItemsRow.Contains (destroyedTileCell.xIndex))
			{
				destroyedItemsRow.Add (destroyedTileCell.xIndex);
			}
			destroyedTileCell.tileBehaviour = null;
			for (int y = destroyedTileCell.yIndex + 1; y < columnItemsCount; y++)
			{
				var upperCell = gameBoard[destroyedTileCell.xIndex, y];
				if (upperCell.tileBehaviour != null)
				{
					for (int height = 0; height < y; height++)
					{
						var lowercell = gameBoard[destroyedTileCell.xIndex, height];
						if (lowercell.tileBehaviour == null)
						{
							lowercell.tileBehaviour = upperCell.tileBehaviour;
							upperCell.tileBehaviour = null;
							lowercell.tileBehaviour.ReInitTileAfterMoving (lowercell);
						}
					}
				}
			}
		}
		SpawnTilesToFillEmptyCells (destroyedItemsRow);
		CheckTileMatches ();
	}

	private void SpawnTilesToFillEmptyCells(List<int> destroyedItemsRow)
	{
		for (int i = 0; i < destroyedItemsRow.Count; i++)
		{
			var rowIndex = destroyedItemsRow[i];
			for (int y = 0; y < columnItemsCount; y++)
			{
				var cell = gameBoard[rowIndex, y];
				if (!cell.tileBehaviour)
				{
					var tilePos = new Vector3 (cell.cellPosition.x, spawnPoint.localPosition.y + tileScale.y * y, 0);
					var tile = SpawnRandomTile (tilePos, cell.xIndex, cell.yIndex);
					tile.ReInitTileAfterMoving (cell);
					cell.tileBehaviour = tile;
				}
			}
		}
	}

	private TileBehaviour SpawnRandomTile(Vector3 pos, int cellXIndex, int cellYIndex)
	{
		var randomTileRef = assetCategory.GetRandomTile ();
		var tileDefaultSprite = randomTileRef.tileMaps.FirstOrDefault (x => x.tileCategory == TileCategory.Default).tileSprite;
		var tileObject = Instantiate (blockPrefab, this.transform);
		tileObject.transform.localScale = tileScale;
		tileObject.transform.position = pos;
		var tile = tileObject.GetComponent<TileBehaviour> ();
		tile.InitTile (cellXIndex, cellYIndex, randomTileRef.tileColor, OnTileDestroyed, assetCategory.GetTileSprite);
		tileObject.name = randomTileRef.tileColor.ToString () + cellXIndex + "" + cellYIndex;
		return tile;
	}

}
