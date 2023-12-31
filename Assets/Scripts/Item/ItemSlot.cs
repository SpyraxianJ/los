using UnityEngine;

public class ItemSlot : MonoBehaviour
{
    public bool unavailable;
    public Item item; // The item currently in the slot
    public IHaveInventory linkedInventoryObject;

    public ItemSlot Init(IHaveInventory linkedInventoryObject)
    {
        this.linkedInventoryObject = linkedInventoryObject;

        return this;
    }
    public ItemSlot AssignItemIcon(ItemIcon itemIcon)
    {
        itemIcon.transform.SetParent(transform);
        itemIcon.rectTransform.localPosition = Vector3.zero;
        itemIcon.rectTransform.sizeDelta = Vector2.zero;
        itemIcon.rectTransform.localScale = Vector2.one;

        // Set the item in the target slot
        item = itemIcon.item;

        return this;
    }
    public void ClearItemIcon()
    {
        foreach(Transform child in transform)
            if (child.name != "Blocked")
                Destroy(child.gameObject);

        item = null;
    }
    private void Update()
    {
        transform.Find("Blocked").gameObject.SetActive(unavailable);
    }
}