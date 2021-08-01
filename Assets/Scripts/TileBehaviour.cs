using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileBehaviour : MonoBehaviour
{
	public int xIndex;
	public int yIndex;
	public GameObject tileItem;
	public TileColor tileColor;
	public List<TileBehaviour> matchedNeighbours;
	public bool isVisited;
	private int gridSize = 5;
	public TileState tileState;
	public Vector2 parentIndex;
	public List<TileBehaviour> childsObeservers;

	public void InitTile(int xIndex, int yIndex, GameObject tileItem, TileColor tileColor)
	{
		this.xIndex = xIndex;
		this.yIndex = yIndex;
		this.tileItem = tileItem;
		this.tileColor = tileColor;
		matchedNeighbours = new List<TileBehaviour> ();
		parentIndex = new Vector2 (-1, -1);
		tileState = TileState.None;
	}
	public void CheckForNeighbours()
	{
		if (isVisited)
		{
			return;
		}
		matchedNeighbours.Clear ();
		var neighboursIndexes = GetNeighboursIndexes ();
		foreach (var item in neighboursIndexes)
		{
			var tileElement = GameGridHandler.gameBoard[(int)item.x, (int)item.y];
			if (tileElement.tileColor == tileColor && tileElement.tileState != TileState.Parent)
			{
				if (tileState == TileState.None && !isVisited)
				{
					tileState = TileState.Parent;
				}
				if (tileState == TileState.Parent)
				{
					tileElement.tileState = TileState.Child;
					tileElement.parentIndex = new Vector2 (xIndex, yIndex);
					if (!childsObeservers.Contains (tileElement))
					{
						childsObeservers.Add (tileElement);
						tileElement.CheckForNeighbours ();
					}
				}
				else if (tileState != TileState.Parent)
				{
					tileElement.tileState = TileState.Child;
					tileElement.parentIndex = new Vector2 (parentIndex.x, parentIndex.y);
					var parentTile = GameGridHandler.gameBoard[(int)parentIndex.x, (int)parentIndex.y];
					if (!parentTile.childsObeservers.Contains (this))
					{
						parentTile.childsObeservers.Add (this);
					}
					if (!parentTile.childsObeservers.Contains (tileElement))
					{
						parentTile.childsObeservers.Add (tileElement);
						tileElement.CheckForNeighbours ();
					}
				}
			}
		}
		//if (tileState == TileState.Parent)
		//{
		//	Debug.LogError ("find" + childsObeservers.Count + "color" + this.name);
		//}
		isVisited = true;
	}
	public List<Vector2> GetNeighboursIndexes()
	{
		var result = new List<Vector2> ();
		if (xIndex > 0)
		{
			result.Add (new Vector2 (xIndex - 1, yIndex));
		}
		if (xIndex < 5 - 1)
		{
			result.Add (new Vector2 (xIndex + 1, yIndex));
		}
		if (yIndex > 0)
		{
			result.Add (new Vector2 (xIndex, yIndex - 1));
		}
		if (yIndex < 8 - 1)
		{
			result.Add (new Vector2 (xIndex, yIndex + 1));
		}
		return result;
	}

	private void OnMouseDown()
	{
		Debug.LogError (name);
		OnClickOnTile ();
	}

	private void OnClickOnTile()
	{
		if (tileState == TileState.Child)
		{
			var parentTile = GameGridHandler.gameBoard[(int)parentIndex.x, (int)parentIndex.y];
			parentTile.NotifyParentToDestroy ();
		}
		else if (tileState == TileState.Parent)
		{
			NotifyParentToDestroy ();
		}
	}

	private void NotifyParentToDestroy()
	{
		childsObeservers.Add (this);
		foreach (var item in childsObeservers)
		{
			Destroy (item.gameObject);
		}
		
	}

}
