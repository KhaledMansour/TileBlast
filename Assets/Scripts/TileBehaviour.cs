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
	public ItemIndex parentIndex;
	public List<TileBehaviour> childsObeservers;
	private float movingTime = 2f;
	private Action<List<TileBehaviour>> onDestoryAction;
	private Func<TileColor, TileCategory, Sprite> getTileSprite;
	private IEnumerator moveEnumerable;
	private SpriteRenderer spriteRenderer;
	private float yScale;
	private Action onFinishMoving;
	public void InitTile(int xIndex, int yIndex, TileColor tileColor, Action<List<TileBehaviour>> onDestoryAction, Func<TileColor, TileCategory, Sprite> getTileSprite)
	{
		this.xIndex = xIndex;
		this.yIndex = yIndex;
		this.tileColor = tileColor;
		this.onDestoryAction = onDestoryAction;
		this.getTileSprite = getTileSprite;
		yScale = transform.localScale.y;
		spriteRenderer = GetComponent<SpriteRenderer> ();
		ResetTileProps ();
	}

	public void MoveTileToCell(BoardCell cell)
	{
		this.xIndex = cell.xIndex;
		this.yIndex = cell.yIndex;
		ResetTileProps ();
		if (moveEnumerable != null)
		{
			StopCoroutine (moveEnumerable);
		}
		moveEnumerable = MoveToCell (cell.cellPosition);
		StartCoroutine (moveEnumerable);
	}

	public void ResetTileProps()
	{
		tileState = TileState.None;
		parentIndex = new ItemIndex (-1, -1);
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
			var cell = GameGridHandler.gameBoard[item.x, item.y];
			if (!cell.tileReference)
			{
				continue;
			}
			var tileElement = cell.tileReference;
			if (tileElement.tileColor == tileColor && tileElement.tileState != TileState.Parent)
			{
				if (tileState == TileState.None && !isVisited)
				{
					tileState = TileState.Parent;
				}
				if (tileState == TileState.Parent)
				{
					tileElement.tileState = TileState.Child;
					tileElement.parentIndex = new ItemIndex (xIndex, yIndex);
					if (!childsObeservers.Contains (tileElement))
					{
						childsObeservers.Add (tileElement);
						tileElement.CheckNeighboursMatches ();
					}
				}
				else if (tileState != TileState.Parent)
				{
					tileElement.tileState = TileState.Child;
					tileElement.parentIndex = new ItemIndex (parentIndex.x, parentIndex.y);
					var parentCell = GameGridHandler.gameBoard[parentIndex.x, parentIndex.y];
					var parentTile = parentCell.tileReference;
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

	public List<ItemIndex> GetNeighboursIndexes()
	{
		var result = new List<ItemIndex> ();
		if (xIndex > 0)
		{
			result.Add (new ItemIndex (xIndex - 1, yIndex));
		}
		if (xIndex < GameGridHandler.currentLevelProps.rowItemsCount - 1)
		{
			result.Add (new ItemIndex (xIndex + 1, yIndex));
		}
		if (yIndex > 0)
		{
			result.Add (new ItemIndex (xIndex, yIndex - 1));
		}
		if (yIndex < GameGridHandler.currentLevelProps.columnItemsCount - 1)
		{
			result.Add (new ItemIndex (xIndex, yIndex + 1));
		}
		return result;
	}

	private void OnMouseDown()
	{
		if (GameGridHandler.gameState == GameState.Moving)
		{
			return;
		}
		OnClickOnTile ();
	}

	private void OnClickOnTile()
	{
		if (tileState == TileState.Child)
		{
			var cell = GameGridHandler.gameBoard[parentIndex.x, parentIndex.y];
			var parentTile = cell.tileReference;
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
		var tilePos = pos + Vector3.down * yScale * 0.2f;
		while (transform.position != tilePos)
		{
			yield return null;
			transform.position = Vector3.MoveTowards (transform.position, tilePos, timeElapsed / movingTime);
			timeElapsed += Time.deltaTime;
		}
		tilePos = pos + Vector3.up * transform.localScale.y * 0.1f;
		while (transform.position != tilePos)
		{
			yield return null;
			transform.position = Vector3.MoveTowards (transform.position, tilePos, timeElapsed / (movingTime * 2));
			timeElapsed += Time.deltaTime;
		}
		tilePos = pos;
		while (transform.position != tilePos)
		{
			yield return null;
			transform.position = Vector3.MoveTowards (transform.position, tilePos, timeElapsed / (movingTime * 2));
			timeElapsed += Time.deltaTime;
		}
		transform.position = pos;
		if (onFinishMoving != null)
		{
			onFinishMoving ();
			onFinishMoving = null;
		}
	}

	public void UpdateTileSprite()
	{
		if (getTileSprite == null)
		{
			return;
		}
		var tileSprite = getTileSprite (tileColor, tileCategory);
		spriteRenderer.sprite = tileSprite;
	}

	public bool IfParentTile()
	{
		return tileState == TileState.Parent;
	}

	public void NotifyFinishMoving(Action finishMoving)
	{
		onFinishMoving = finishMoving;
	}

	public void OnPushInPool()
	{
		if (moveEnumerable != null)
		{
			StopCoroutine (moveEnumerable);
			moveEnumerable = null;
		}
	}
}
