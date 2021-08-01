using System.Collections.Generic;
using UnityEngine;

public enum TileState
{
	None, Child, Parent
}

//[System.Serializable]
//public class Tile
//{
//	public int xIndex;
//	public int yIndex;
//	public GameObject tileItem;
//	public TileColor tileColor;
//	public List<Tile> matchedNeighbours;
//	public bool isVisited;
//	private int gridSize = 5;
//	public TileState tileState;
//	public Vector2 parentIndex;
//	public List<Tile> childsObeservers;
//	public Tile(int xIndex, int yIndex, GameObject tileItem, TileColor tileColor)
//	{
//		this.xIndex = xIndex;
//		this.yIndex = yIndex;
//		this.tileItem = tileItem;
//		this.tileColor = tileColor;
//		matchedNeighbours = new List<Tile> ();
//		parentIndex = new Vector2 (-1, -1);
//		tileState = TileState.None;
//	}

//	public void CheckForNeighbours()
//	{
//		if (isVisited)
//		{
//			return;
//		}
//		matchedNeighbours.Clear ();
//		var neighboursIndexes = GetNeighboursIndexes ();
//		foreach (var item in neighboursIndexes)
//		{
//			var tileElement = GameGridHandler.gameBoard[(int)item.x, (int)item.y];
//			if (tileElement.tileColor == tileColor)
//			{
//				if (tileState == TileState.None && !isVisited)
//				{
//					tileState = TileState.Parent;
//				}
//				if (tileState == TileState.Parent)
//				{
//					tileElement.tileState = TileState.Child;
//					tileElement.parentIndex = new Vector2 (xIndex, yIndex);
//					if (!childsObeservers.Contains(tileElement))
//					{
//						childsObeservers.Add (tileElement);
//						tileElement.CheckForNeighbours ();
//					}
//				}
//				else if (tileState != TileState.Parent)
//				{
//					tileElement.tileState = TileState.Child;
//					tileElement.parentIndex = new Vector2 (parentIndex.x, parentIndex.y);
//					var parentTile = GameGridHandler.gameBoard[(int)parentIndex.x, (int)parentIndex.y];
//					if (!parentTile.childsObeservers.Contains (this))
//					{
//						parentTile.childsObeservers.Add (this);
//					}
//					if (!parentTile.childsObeservers.Contains (tileElement))
//					{
//						parentTile.childsObeservers.Add (tileElement);
//						tileElement.CheckForNeighbours ();
//					}
//				}
//			}
//		}
//		isVisited = true;
//	}

//	public List<Vector2> GetNeighboursIndexes()
//	{
//		var result = new List<Vector2> ();
//		if (xIndex > 0)
//		{
//			//matchedNeighbours.Add (GameGridHandler.gameBoard[xIndex - 1, yIndex]);
//			result.Add (new Vector2 (xIndex - 1, yIndex));
//		}
//		if (xIndex < 5 - 1)
//		{
//			//matchedNeighbours.Add (GameGridHandler.gameBoard[xIndex + 1, yIndex]);
//			result.Add (new Vector2 (xIndex + 1, yIndex));
//		}
//		if (yIndex > 0)
//		{
//			//matchedNeighbours.Add (GameGridHandler.gameBoard[xIndex, yIndex - 1]);
//			result.Add (new Vector2 (xIndex, yIndex - 1));
//		}
//		if (yIndex < 8 - 1)
//		{
//			//matchedNeighbours.Add (GameGridHandler.gameBoard[xIndex, yIndex + 1]);
//			result.Add (new Vector2 (xIndex, yIndex + 1));
//		}
//		return result;
//	}
//}
[System.Serializable]
public enum TileColor { Blue, Green, Pink, Purple, Red, Yellow };
[System.Serializable]
public class TileMap
{
	public Sprite tileSprite;
	public TileColor TileColor;
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
	private static float xOffsetPercentage = 10;
	[SerializeField]
	private float yOffsetPercentage = 10;
	[SerializeField]
	private GameObject blockPrefab;
	[SerializeField]
	List<TileMap> tilesMap;
	public static TileBehaviour[,] gameBoard;
	public List<TileBehaviour> allTiles;

	void Awake()
	{
		InitTilesGrid ();

	}

	void Update()
	{
		if (Input.GetKeyDown (KeyCode.Space))
		{
			CheckMatches ();
		}
	}

	void InitTilesGrid()
	{
		//screenHeight = Camera.main.orthographicSize * 2;
		//screenWidth = screenHeight * Screen.width / Screen.height;
		//screenHeight = 7;
		gameBoard = new TileBehaviour[rowItemsCount, columnItemsCount];
		var xItemSize = screenWidth / (rowItemsCount + (rowItemsCount + 1) / xOffsetPercentage);
		var yItemSize = screenHeight / (columnItemsCount + (columnItemsCount + 1) / yOffsetPercentage);
		var startXPos = -screenWidth / 2 + xItemSize / 2 + xItemSize / xOffsetPercentage;
		var startYPos = -screenHeight / 2 + yItemSize / 2 + yItemSize / yOffsetPercentage;

		for (int y = 0; y < columnItemsCount; y++)
		{
			for (int x = 0; x < rowItemsCount; x++)
			{
				var randomTileRef = tilesMap[Random.Range (0, 3)];
				//randomTileRef = tilesMap[0];
				var tileObject = Instantiate (blockPrefab, this.transform);
				tileObject.GetComponent<SpriteRenderer> ().sprite = randomTileRef.tileSprite;
				tileObject.transform.localScale = new Vector3 (xItemSize, yItemSize, 0);
				var xPos = (x * xItemSize + x * (xItemSize / xOffsetPercentage) + startXPos);
				var yPos = (y * yItemSize + y * (yItemSize / yOffsetPercentage) + startYPos);
				tileObject.transform.position = new Vector3 (xPos, yPos, 0);
				var tile = tileObject.GetComponent<TileBehaviour>();
				tile.InitTile (x, y, tileObject, randomTileRef.TileColor);
				gameBoard[x, y] = tile;
				allTiles.Add (tile);
				tileObject.name = randomTileRef.TileColor.ToString ()+ x + "" + y;
			}
		}
		//Debug.LogError (gameBoard[2, 2].GetNeighboursIndexes ());
		//CheckMatches ();
	}

	void CheckMatches()
	{
		for (int y = 0; y < columnItemsCount; y++)
		{
			for (int x = 0; x < rowItemsCount; x++)
			{
				gameBoard[x, y].CheckForNeighbours ();
			}
		}
	}
}
