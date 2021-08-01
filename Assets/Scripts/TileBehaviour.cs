using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileBehaviour : MonoBehaviour
{
	public int xIndex;
	public int yIndex;
	public GameObject tileItem;
	public TileColor tileColor;
	public TileCategory tileCategory;
	public List<TileBehaviour> matchedNeighbours;
	public bool isVisited;
	public TileState tileState;
	public Vector2 parentIndex;
	public List<TileBehaviour> childsObeservers;
	private float movingTime = 2f;
	private Action<List<TileBehaviour>> onDestoryAction;
	private IEnumerator enumerable;
	public void InitTile(int xIndex, int yIndex, GameObject tileItem, TileColor tileColor, Action<List<TileBehaviour>> onDestoryAction)
	{
		this.xIndex = xIndex;
		this.yIndex = yIndex;
		this.tileItem = tileItem;
		this.tileColor = tileColor;
		this.onDestoryAction = onDestoryAction;
		ResetTileProps ();
	}

	public void ReInitTileAfterMoving(int xindex, int yIndex, Vector3 targetCellPos)
	{
		this.xIndex = xindex;
		this.yIndex = yIndex;
		ResetTileProps ();
		if (enumerable != null)
		{
			StopCoroutine (enumerable);
		}
		enumerable =  MoveToCell (targetCellPos);
		StartCoroutine (enumerable);
	}

	public void ResetTileProps()
	{
		tileState = TileState.None;
		parentIndex = new Vector2 (-1, -1);
		isVisited = false;
		tileCategory = TileCategory.Default;
		matchedNeighbours.Clear ();
		childsObeservers.Clear ();
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
			var cell = GameGridHandler.gameBoard[(int)item.x, (int)item.y];
			if (!cell.tileBehaviour)
			{
				continue;
			}
			var tileElement = cell.tileBehaviour;
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
					var parentCell = GameGridHandler.gameBoard[(int)parentIndex.x, (int)parentIndex.y];
					var parentTile = parentCell.tileBehaviour;
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
			var cell = GameGridHandler.gameBoard[(int)parentIndex.x, (int)parentIndex.y];
			var parentTile = cell.tileBehaviour;
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
		onDestoryAction (childsObeservers);
	}

	public IEnumerator MoveToCell(Vector3 pos)
	{
		float timeElapsed = 0;
		while (timeElapsed < movingTime)
		{
			transform.position = Vector3.MoveTowards (transform.position, pos, timeElapsed / movingTime);
			timeElapsed += Time.deltaTime;
			yield return null;
		}
		transform.position = pos;
	}
} 
