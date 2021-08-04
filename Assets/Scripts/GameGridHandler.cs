using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
	private SpriteRenderer bg;
	[SerializeField]
	private List<LevelProps> levelProps;
	[SerializeField]
	private LevelType levelType;
	public static LevelProps currentLevelProps;
	[SerializeField]
	private Transform spawnPoint;
	[SerializeField]
	private float xOffsetPercentage;
	[SerializeField]
	private float yOffsetPercentage;
	[SerializeField]
	private GameObject tilePrefab;
	[SerializeField]
	private AssetsLoader assetCategory;
	public static BoardCell[,] gameBoard;
	public static GameState gameState { get; private set; }
	private TileBehaviour lastSpawnedTile;
	private Vector3 tileScale;
	private Dictionary<TileColor, List<TileBehaviour>> tilesColorsDict;
	private Stack<TileBehaviour> tilePool;

	void Awake()
	{
		tilePool = new Stack<TileBehaviour> ();
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
			ShuffleBehaviour ();
		}
	}

	bool CheckNeedShuffle()
	{
		for (int y = 0; y < columnItemsCount; y++)
		{
			for (int x = 0; x < rowItemsCount; x++)
			{
				if (gameBoard[x, y].tileReference)
				{
					if (gameBoard[x, y].tileReference.IfParentTile ())
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
				var neighbourCell = gameBoard[tileNeighbourIndex.x, tileNeighbourIndex.y];
				var targetTile = colorTiles.Value[1];
				var targetCell = gameBoard[targetTile.xIndex, targetTile.yIndex];
				SwapCellsTile (neighbourCell, targetCell);
			}
		}
		CheckTileMatches ();
	}

	private void SwapCellsTile(BoardCell cell1, BoardCell cell2)
	{
		var tile1 = cell1.tileReference;
		var tile2 = cell2.tileReference;
		var tile1Temp = tile1;
		cell2.tileReference = tile1Temp;
		cell1.tileReference = tile2;
		tile2.MoveTileToCell (cell1);
		tile1.MoveTileToCell (cell2);
	}

	void InitTilesGrid()
	{
		gameState = GameState.Moving;
		var reduceHeightPercentage = 20;
		screenHeight = Camera.main.orthographicSize * 2;
		screenWidth = screenHeight * Screen.width / Screen.height;
		screenHeight -= (screenHeight * reduceHeightPercentage) / 100;
		bg.size = new Vector2 (screenWidth, screenHeight);
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
				var cell = new BoardCell (null, x, y, new Vector2 (cellXPos, cellYPos));
				gameBoard[x, y] = cell;
				var tile = SpawnRandomTile (tilePos, cell);
				lastSpawnedTile = tile;
			}
		}
		lastSpawnedTile.NotifyFinishMoving (OnLastTileFinishMoving);
	}

	private void CheckTileMatches()
	{
		ResetTilesProps ();
		for (int y = 0; y < columnItemsCount; y++)
		{
			for (int x = 0; x < rowItemsCount; x++)
			{
				if (gameBoard[x, y].tileReference)
				{
					gameBoard[x, y].tileReference.CheckNeighboursMatches ();
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
				if (gameBoard[x, y].tileReference)
				{
					gameBoard[x, y].tileReference.ResetTileProps ();
				}
			}
		}
	}

	void OnTileDestroyed(List<TileBehaviour> tiles)
	{
		var destroyedItemsRow = new List<int> ();
		gameState = GameState.Moving;
		foreach (var tile in tiles)
		{
			TileBehaviour destroyedTile;
			var destroyedTileCell = gameBoard[tile.xIndex, tile.yIndex];
			RemoveTileFromDict (destroyedTileCell.tileReference);
			destroyedTile = destroyedTileCell.tileReference;
			AddTileToPool (destroyedTile);
			destroyedTile.gameObject.SetActive (false);
			destroyedTile.OnPushInPool ();
			destroyedTileCell.tileReference = null;
			if (!destroyedItemsRow.Contains (destroyedTileCell.xIndex))
			{
				destroyedItemsRow.Add (destroyedTileCell.xIndex);
			}
			for (int y = destroyedTileCell.yIndex + 1; y < columnItemsCount; y++)
			{
				var upperCell = gameBoard[destroyedTileCell.xIndex, y];
				if (upperCell.tileReference && upperCell.tileReference.gameObject.activeSelf)
				{
					for (int height = 0; height < y; height++)
					{
						var lowercell = gameBoard[destroyedTileCell.xIndex, height];
						if (!lowercell.tileReference || !lowercell.tileReference.gameObject.activeSelf)
						{
							lowercell.tileReference = upperCell.tileReference;
							upperCell.tileReference = null;
							lowercell.tileReference.MoveTileToCell (lowercell);
						}
					}
				}
			}
		}
		SpawnTilesToFillEmptyCells (destroyedItemsRow);
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
				if (cell.tileReference == null || !cell.tileReference.gameObject.activeSelf)
				{
					numberOfSpawnsInColumn++;
					var tilePos = new Vector3 (cell.cellPosition.x, spawnPoint.localPosition.y + tileScale.y * numberOfSpawnsInColumn, 0);
					var tile = SpawnRandomTile (tilePos, cell);
					cell.tileReference = tile;
					lastSpawnedTile = tile;
				}
			}
		}
		lastSpawnedTile.NotifyFinishMoving (OnLastTileFinishMoving);
	}

	private TileBehaviour SpawnRandomTile(Vector3 pos, BoardCell cell)
	{
		var randomTileRef = assetCategory.GetRandomTile ();
		var tileDefaultSprite = randomTileRef.tileMaps.FirstOrDefault (x => x.tileCategory == TileCategory.Default).tileSprite;
		var tile = GetTileFromPool ();
		tile.transform.position = pos;
		cell.tileReference = tile;
		tile.InitTile (cell.xIndex, cell.yIndex, randomTileRef.tileColor, OnTileDestroyed, assetCategory.GetTileSprite);
		tile.MoveTileToCell (cell);
		tile.name = randomTileRef.tileColor.ToString () + cell.xIndex + "" + cell.yIndex;
		AddTileToDict (tile);
		return tile;
	}

	private void OnLastTileFinishMoving()
	{
		CheckTileMatches ();
		if (CheckNeedShuffle ())
		{
			ShuffleBehaviour ();
		}
		gameState = GameState.Idle;
	}

	private void AddTileToPool(TileBehaviour tile = null)
	{
		var addedTile = tile;
		if (addedTile == null)
		{
			var tileObject = Instantiate (tilePrefab, this.transform);
			tileObject.transform.localScale = tileScale;
			addedTile = tileObject.GetComponent<TileBehaviour> ();
		}

		tilePool.Push (addedTile);
	}

	private TileBehaviour GetTileFromPool()
	{
		if (tilePool.Count == 0)
		{
			AddTileToPool ();
		}
		var tile = tilePool.Pop ();
		tile.gameObject.SetActive (true);
		return tile;
	}
}
