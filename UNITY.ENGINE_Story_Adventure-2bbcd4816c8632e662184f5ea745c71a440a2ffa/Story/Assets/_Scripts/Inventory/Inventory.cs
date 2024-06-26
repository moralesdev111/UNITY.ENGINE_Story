using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
	private const int inventorySpace = 6;
	public int InventorySpace {get {return inventorySpace;} }
	private List<Item> items = new List<Item>();
	public List<Item> Items { get { return items;} }
	public delegate void OnItemChanged();
	public OnItemChanged uiChangeTriggered;
	[SerializeField] private InventoryUI inventoryUI;
	public InventoryUI InventoryUI { get {  return inventoryUI; } }

	private void Awake()
	{
		if(DataManager.Instance != null)
		{
			DataManager.Instance.SetInventory(this);
		}		
	}

	public bool AddItemToInventory(Item item)
	{
		if (items.Count >= inventorySpace)
		{
			Debug.Log("Inventory Full");
			return false;
		}
		items.Add(item);
		uiChangeTriggered.Invoke();
		return true;
	}

	public void RemoveItemFromInventory(Item item)
	{
		if(items.Contains(item))
		{
			items.Remove(item);
			uiChangeTriggered.Invoke();
		}
	}

	public bool CheckIfHasItem(Item item)
	{
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i] == item)
			{
				return true;
			}
		}
		return false;
	}
}