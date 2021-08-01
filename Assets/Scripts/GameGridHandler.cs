using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
	private Transform spawnPoint;
	private static float xOffsetPercentage = 10;
	[SerializeField]
	private float yOffsetPercentage = 10;
	[SerializeField]
	private GameObject blockPrefab;
	[SerializeField]
	private AssetsLoader assetCategory;
	public static BoardCell[,] gameBoard;
	public List<TileBehaviour> allTiles;
	private Vector3 tileScale;

	void Awake()
	{
		InitTilesGrid ();
	}

	void Update()
	{
		if (Input.GetKeyDown (KeyCode.M))
		{
			CheckMatches ();
		}

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
				var randomTileRef = assetCategory.GetRandomTile ();
				var tileDefaultSprite = randomTileRef.tileMaps.FirstOrDefault (x => x.tileCategory == TileCategory.Default).tileSprite;
				//randomTileRef = tilesMap[0];
				var tileObject = Instantiate (blockPrefab, this.transform);
				tileObject.GetComponent<SpriteRenderer> ().sprite = tileDefaultSprite;
				tileObject.transform.localScale = new Vector3 (xItemSize, yItemSize, 0);
				var xPos = (x * xItemSize + x * (xItemSize / xOffsetPercentage) + startXPos);
				var yPos = (y * yItemSize + y * (yItemSize / yOffsetPercentage) + startYPos);
				tileObject.transform.position = new Vector3 (xPos, yPos, 0);
				var tile = tileObject.GetComponent<TileBehaviour>();
				tile.InitTile (x, y, tileObject, randomTileRef.tileColor, OnTileDestroyed);
				allTiles.Add (tile);
				tileObject.name = randomTileRef.tileColor.ToString ()+ x + "" + y;
				var cell = new BoardCell (tile, x, y, new Vector2(xPos, yPos));
				gameBoard[x, y] = cell;
			}
		}
		//Debug.LogError (gameBoard[2, 2].GetNeighboursIndexes ());
		CheckMatches ();
	}

	void CheckMatches()
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
			if (!destroyedItemsRow.Contains(destroyedTileCell.xIndex))
			{
				destroyedItemsRow.Add (destroyedTileCell.xIndex);
			}
			destroyedTileCell.tileBehaviour = null;
			for (int y = destroyedTileCell.yIndex + 1; y < columnItemsCount; y++)
			{
				var upperCell = gameBoard[tile.xIndex, y];
				if (upperCell.tileBehaviour != null)
				{
					for (int height = 0; height < y; height++)
					{
						var lowercell = gameBoard[tile.xIndex, height];
						if (lowercell.tileBehaviour == null)
						{
							lowercell.tileBehaviour = upperCell.tileBehaviour;
							upperCell.tileBehaviour = null;
							lowercell.tileBehaviour.ReInitTileAfterMoving ( lowercell.xIndex, lowercell.yIndex, lowercell.cellPosition);
						}
					}
				}
			}
		}
		SpawnTilesToFillEmptyCells (destroyedItemsRow);
		CheckMatches ();
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
					var randomTileRef = assetCategory.GetRandomTile();
					var tileDefaultSprite = randomTileRef.tileMaps.FirstOrDefault (x => x.tileCategory == TileCategory.Default).tileSprite;
					var tileObject = Instantiate (blockPrefab, this.transform);
					tileObject.GetComponent<SpriteRenderer> ().sprite = tileDefaultSprite;
					tileObject.transform.localScale = tileScale;
					tileObject.transform.position = new Vector3 (cell.cellPosition.x, spawnPoint.localPosition.y + tileScale.y * y, 0);
					var tile = tileObject.GetComponent<TileBehaviour> ();
					tile.InitTile (cell.xIndex, cell.yIndex, tileObject, randomTileRef.tileColor, OnTileDestroyed);
					tile.ReInitTileAfterMoving (cell.xIndex, cell.yIndex, cell.cellPosition);
					cell.tileBehaviour = tile;
				}
			}
		}

	}
}
