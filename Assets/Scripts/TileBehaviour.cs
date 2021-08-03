using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileBehaviour : MonoBehaviour
{
	public int xIndex;
	public int yIndex;
	public TileColor tileColor;
	public TileCategory tileCategory;
	public List<TileBehaviour> matchedNeighbours;
	public bool isVisited;
	public TileState tileState;
	public Vector2 parentIndex;
	public List<TileBehaviour> childsObeservers;
	private float movingTime = 5f;
	private Action<List<TileBehaviour>> onDestoryAction;
	private Func<TileColor, TileCategory, Sprite> getTileSprite;
	private Action<TileBehaviour> notifyFinishMoving;
	private Action<TileBehaviour> notifyStartMoving;
	private IEnumerator moveEnumerable;
	private SpriteRenderer spriteRenderer;
	private float yScale;
	public void InitTile(int xIndex, int yIndex, TileColor tileColor, Action<List<TileBehaviour>> onDestoryAction, Action<TileBehaviour> notifyStartMoving, Action<TileBehaviour> notifyFinishMoving, Func<TileColor, TileCategory, Sprite> getTileSprite)
	{
		this.xIndex = xIndex;
		this.yIndex = yIndex;
		this.tileColor = tileColor;
		this.onDestoryAction = onDestoryAction;
		this.notifyFinishMoving = notifyFinishMoving;
		this.notifyStartMoving = notifyStartMoving;
		this.getTileSprite = getTileSprite;
		yScale = transform.localScale.y;
		spriteRenderer = GetComponent<SpriteRenderer> ();
		ResetTileProps ();
	}

	public void InitTile(BoardCell cell)
	{
		this.xIndex = cell.xIndex;
		this.yIndex = cell.yIndex;
		ResetTileProps ();
		if (moveEnumerable != null)
		{
			StopCoroutine (moveEnumerable);
		}
		//notifyStartMoving (this);
		moveEnumerable = MoveToCell (cell.cellPosition);
		StartCoroutine (moveEnumerable);
	}

	public void ResetTileProps()
	{
		tileState = TileState.None;
		parentIndex = new Vector2 (-1, -1);
		isVisited = false;
		UpdateTileCategory (TileCategory.Default);
		matchedNeighbours.Clear ();
		childsObeservers.Clear ();
	}

	public void CheckNeighboursMatches()
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
						tileElement.CheckNeighboursMatches ();
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
						tileElement.CheckNeighboursMatches ();
					}
				}
			}
		}
		if (tileState == TileState.Parent)
		{
			var tileCategory = GameGridHandler.currentLevelProps.GetTileCategory (childsObeservers.Count + 1);
			UpdateTileCategory (tileCategory);
			childsObeservers.ForEach (x => x.UpdateTileCategory (tileCategory));
		}
		isVisited = true;
	}

	private void UpdateTileCategory(TileCategory tileCategory)
	{
		this.tileCategory = tileCategory;
		UpdateTileSprite ();
	}

	public List<Vector2> GetNeighboursIndexes()
	{
		var result = new List<Vector2> ();
		if (xIndex > 0)
		{
			result.Add (new Vector2 (xIndex - 1, yIndex));
		}
		if (xIndex < GameGridHandler.currentLevelProps.rowItemsCount - 1)
		{
			result.Add (new Vector2 (xIndex + 1, yIndex));
		}
		if (yIndex > 0)
		{
			result.Add (new Vector2 (xIndex, yIndex - 1));
		}
		if (yIndex < GameGridHandler.currentLevelProps.columnItemsCount - 1)
		{
			result.Add (new Vector2 (xIndex, yIndex + 1));
		}
		return result;
	}

	private void OnMouseDown()
	{
		if (GameGridHandler.gameStateMoving)
		{
			return;
		}
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

	private IEnumerator MoveToCell(Vector3 pos)
	{
		float timeElapsed = 0;
		var offset = pos + Vector3.down * yScale * 0.1f;
		while (transform.position != offset)
		{
			yield return null;
			transform.position = Vector3.MoveTowards (transform.position, offset, timeElapsed / movingTime);
			timeElapsed += Time.deltaTime;
		}
		//OnFinishMoving (tileBehaviour);
		offset = pos + Vector3.up * transform.localScale.y * 0.1f;
		while (transform.position != offset)
		{
			yield return null;
			transform.position = Vector3.MoveTowards (transform.position, offset, timeElapsed / (movingTime*2));
			timeElapsed += Time.deltaTime;
		}
		offset = pos;
		while (transform.position != offset)
		{
			yield return null;
			transform.position = Vector3.MoveTowards (transform.position, offset, timeElapsed / (movingTime * 2));
			timeElapsed += Time.deltaTime;
		}
		transform.position = pos;
	}

	public void UpdateTileSprite()
	{
		var tileSprite = getTileSprite (tileColor, tileCategory);
		spriteRenderer.sprite = tileSprite;
	}

	public bool IfParentTile()
	{
		return tileState == TileState.Parent;
	}
}
