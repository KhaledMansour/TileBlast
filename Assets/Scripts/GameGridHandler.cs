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
	private Dictionary<TileColor, List<TileBehaviour>> tilesColorsDict;
	[SerializeField]
	private List<TileBehaviour> currentMovingTilesObservers;
	public static bool gameStateMoving { get;private set; }

	void Awake()
	{
		//currentMovingTilesObservers = new List<TileBehaviour> ();
		currentLevelProps = levelProps.First (x => x.levelType == levelType);
		columnItemsCount = currentLevelProps.columnItemsCount;
		rowItemsCount = currentLevelProps.rowItemsCount;
		InitTilesColorsDict ();
		InitTilesGrid ();
	}

	private void InitTilesColorsDict()
	{
		tilesColorsDict = new Dictionary<TileColor, List<TileBehaviour>> ();
		for (int i = 0; i < currentLevelProps.maxColorsCount; i++)
		{
			tilesColorsDict.Add ((TileColor)i, new List<TileBehaviour> ());
		}
	}

	private void AddTileToDict(TileBehaviour tileBehaviour)
	{
		var color = tileBehaviour.tileColor;
		var tiles = tilesColorsDict[color];
		if (!tiles.Contains (tileBehaviour))
		{
			tiles.Add (tileBehaviour);
		}
	}

	private void RemoveTileFromDict(TileBehaviour tileBehaviour)
	{
		var color = tileBehaviour.tileColor;
		var tiles = tilesColorsDict[color];
		tiles.Remove (tileBehaviour);
	}

	void Update()
	{
		if (Input.GetKeyDown (KeyCode.M))
		{
			CheckTileMatches ();
		}
		if (Input.GetKeyDown (KeyCode.S))
		{
			//Debug.LogError ("need shuffle" + CheckNeedShuffle ());
			ShuffleBehaviour ();
		}
	}

	bool CheckNeedShuffle()
	{
		for (int y = 0; y < columnItemsCount; y++)
		{
			for (int x = 0; x < rowItemsCount; x++)
			{
				if (gameBoard[x, y].tileBehaviour)
				{
					if (gameBoard[x, y].tileBehaviour.IfParentTile ())
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	private void ShuffleBehaviour()
	{
		for (int i = 0; i < tilesColorsDict.Count; i++)
		{
			var colorTiles = tilesColorsDict.ElementAt (i);
			if (colorTiles.Value.Count > 1)
			{
				var tileNeighbourIndex = colorTiles.Value[0].GetNeighboursIndexes ()[0];
				var neighbourCell = gameBoard[(int)tileNeighbourIndex.x, (int)tileNeighbourIndex.y];
				var targetTile = colorTiles.Value[1];
				var targetCell = gameBoard[(int)targetTile.xIndex, (int)targetTile.yIndex];
				SwapCellsTile (neighbourCell, targetCell);
			}
		}
		CheckTileMatches ();
	}

	private void SwapCellsTile(BoardCell cell1, BoardCell cell2)
	{
		var tile1 = cell1.tileBehaviour;
		var tile2 = cell2.tileBehaviour;
		var tile1Temp = tile1;
		cell2.tileBehaviour = tile1Temp;
		cell1.tileBehaviour = tile2;
		tile2.InitTile (cell1);
		tile1.InitTile (cell2);
	}

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

				var cellXPos = (x * xItemSize + x * (xItemSize / xOffsetPercentage) + startXPos);
				var cellYPos = (y * yItemSize + y * (yItemSize / yOffsetPercentage) + startYPos);
				var tilePos = new Vector3 (cellXPos, spawnPoint.localPosition.y + tileScale.y * y, 0);

				var tile = SpawnRandomTile (tilePos, x, y);
				var cell = new BoardCell (tile, x, y, new Vector2 (cellXPos, cellYPos));
				gameBoard[x, y] = cell;
				tile.InitTile (cell);
			}
		}
		CheckTileMatches ();
		if (CheckNeedShuffle ())
		{
			ShuffleBehaviour ();
		}
	}

	private void CheckTileMatches()
	{
		ResetTilesProps ();
		for (int y = 0; y < columnItemsCount; y++)
		{
			for (int x = 0; x < rowItemsCount; x++)
			{
				if (gameBoard[x, y].tileBehaviour)
				{
					gameBoard[x, y].tileBehaviour.CheckNeighboursMatches ();
				}
			}
		}

	}

	private void ResetTilesProps()
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
	}

	void OnTileDestroyed(List<TileBehaviour> tiles)
	{
		var destroyedItemsRow = new List<int> ();
		foreach (var tile in tiles)
		{
			var destroyedTileCell = gameBoard[tile.xIndex, tile.yIndex];
			RemoveTileFromDict (destroyedTileCell.tileBehaviour);
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
							lowercell.tileBehaviour.InitTile (lowercell);
						}
					}
				}
			}
		}
		SpawnTilesToFillEmptyCells (destroyedItemsRow);
		CheckTileMatches ();
		if (CheckNeedShuffle ())
		{
			ShuffleBehaviour ();
		}
	}

	private void SpawnTilesToFillEmptyCells(List<int> destroyedItemsRow)
	{
		for (int i = 0; i < destroyedItemsRow.Count; i++)
		{
			var rowIndex = destroyedItemsRow[i];
			var numberOfSpawnsInColumn = 0;
			for (int y = 0; y < columnItemsCount; y++)
			{
				var cell = gameBoard[rowIndex, y];
				if (!cell.tileBehaviour)
				{
					numberOfSpawnsInColumn++;
					var tilePos = new Vector3 (cell.cellPosition.x, spawnPoint.localPosition.y + tileScale.y * numberOfSpawnsInColumn, 0);
					var tile = SpawnRandomTile (tilePos, cell.xIndex, cell.yIndex);
					tile.InitTile (cell);
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
		tile.InitTile (cellXIndex, cellYIndex, randomTileRef.tileColor, OnTileDestroyed, AddChildMovingObserver, RemoveChildMovingObserver, assetCategory.GetTileSprite);
		tileObject.name = randomTileRef.tileColor.ToString () + cellXIndex + "" + cellYIndex;
		AddTileToDict (tile);
		return tile;
	}

	private void AddChildMovingObserver(TileBehaviour tileObserver)
	{
		if (!currentMovingTilesObservers.Contains (tileObserver))
		{
			gameStateMoving = true;
			currentMovingTilesObservers.Add (tileObserver);
		}
	}

	private void RemoveChildMovingObserver(TileBehaviour tileBehaviour)
	{
		currentMovingTilesObservers.Remove (tileBehaviour);
		Debug.LogError (currentMovingTilesObservers.Count);
		if (currentMovingTilesObservers.Count == 0)
		{
			gameStateMoving = false;
			Debug.LogError ("check shuffle");
			if (CheckNeedShuffle ())
			{
				ShuffleBehaviour ();
			}
		}
	}

}
