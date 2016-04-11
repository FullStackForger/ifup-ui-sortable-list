using UnityEngine;
using UnityEngine.UI;

public class SortableList : MonoBehaviour
{
    public delegate void OnListItemPressedEvent(SortableList sortableList, SortableListItem sortableListItem);
    public delegate void OnListItemReleasedEvent(SortableList SortableList, SortableListItem sortableListItem);

    public event OnListItemPressedEvent OnListItemPressed;
    public event OnListItemReleasedEvent OnListItemReleased;

    public enum ListType { Horizontal, Vertical }

    public bool scrollLock {
        get {
            return m_scrollLock;
        }
        set {
            m_scrollLock = value;
            UpdateScrollRect();
        }
    }

    public ListType type = ListType.Vertical;
    public Canvas canvas;
    public ScrollRect scrollRect;
    public Vector2 defaultSize = new Vector2(100, 100);
    public bool clearContent = false;

    private SortableListManager Manager { get { return SortableListManager.Instance; } }
    private SortableListItem[] m_listItems;

    private bool m_scrollLock = false;
    private bool m_initDelayed = false;
    private bool m_ready = false;

    private Transform contentTransform { get { return scrollRect.content.transform; } }

    protected void Start()
    {
        if (canvas == null) {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null) {
                throw new System.Exception("Canvas is missing. Are you trying to use UGUI Sortable List outside of canvas?");
            }
        }

        if (scrollRect == null) {
            throw new System.Exception("ScrollRect component missing. Try to assign UGUI ListView object to scrollRect property");
        }
        StartListeningItemEvents();
        ClearContentLayoutGroup();
    }

    protected void OnDestroy()
    {
        StopListeningItemEvents();
        try {
            Manager.Unregister(this);
        } catch (System.Exception e) {
            Debug.LogWarning("Looks like List Item has already been destroyed. No need to worry though, all is well.");
        }
    }

    protected void Update()
    {
        if (scrollRect == null) return;

        if (!m_ready) {
            if (m_initDelayed) {
                m_initDelayed = false;
            } else {
                UpdateScrollRect();
                UpdateContentLayoutGroup();
                UpdateContentSize();
                m_ready = true;
                Manager.Register(this);
            }
        }
    }

    /// <summary>
    /// Returns array of Layout Elements.
    /// </summary>
    /// <returns>The Sortable List Item array.</returns>
    public SortableListItem[] GetItems()
    {
        if (m_listItems == null || m_listItems.Length != scrollRect.content.transform.childCount) {
            m_listItems = scrollRect.content.transform.GetComponentsInChildren<SortableListItem>();
        }
        return m_listItems;
    }

    /// <summary>
    /// Clears Content Layout Group
    /// 
    /// IMORTANT: 
    /// This method has to be called before UpdateContentLaoutGroup
    /// due twice due to Unity idiotic Destroy() method limitation
    /// Why?
    /// 	- Destroy() is postponed until render phase
    /// 	- and so 2nd LaoutGroup can not be attached before 1st one is destroyed
    /// </summary>
    private void ClearContentLayoutGroup()
    {
        // destroy items if needed
        if (clearContent) {
            foreach (Transform transform in contentTransform) {
                Destroy(transform.gameObject);
            }
        }

        // destroy LayoutGroup component if needed
        switch (type) {
        case ListType.Vertical:
            HorizontalLayoutGroup hGroup = contentTransform.GetComponent<HorizontalLayoutGroup>();
            if (hGroup != null) {
                Destroy(hGroup);
                m_initDelayed = true;
            }
            break;
        case ListType.Horizontal:
            VerticalLayoutGroup vGroup = contentTransform.GetComponent<VerticalLayoutGroup>();
            if (vGroup != null) {
                Destroy(vGroup);
                m_initDelayed = true;
            }
            break;
        }
    }

    public void AttachItem(SortableListItem listItem, int index)
    {
        listItem.transform.SetParent(contentTransform);
        listItem.transform.SetSiblingIndex(index);
        UpdateContentSize();
    }
    
    public void DetachItem(SortableListItem listItem, Transform newParentTransform)
    {
        listItem.transform.SetParent(newParentTransform);
        UpdateContentSize();
    }

    public void RemoveItem(SortableListItem listItem)
    {
        Destroy(listItem);
        UpdateContentSize();
    }

    private void UpdateScrollRect()
    {
        if (scrollLock) {
            scrollRect.horizontal = false;
            scrollRect.vertical = false;
            return;
        }

        switch (type) {
        case ListType.Vertical:
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            break;
        case ListType.Horizontal:
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            break;
        }
    }

    public void UpdateContentSize()
    {
        float width = 0;
        float height = 0;
        float tmpSize = 0;
        SortableListItem[] items = GetItems();
        LayoutElement element;
        RectTransform rectTransform = scrollRect.content.GetComponent<RectTransform>();

        // enforce anchor
        switch (type) {
        case ListType.Vertical:
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            break;
        case ListType.Horizontal:
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 1);
            break;
        }

        foreach (SortableListItem item in items) {
            element = item.layoutElement;
            switch (type) {
            case ListType.Horizontal:
                tmpSize = element.preferredWidth > element.minWidth ? element.preferredWidth : element.minWidth;
                if (tmpSize <= 0) {
                    tmpSize = defaultSize.x;
                    element.minWidth = element.preferredWidth = defaultSize.x;
                }

                width += tmpSize;
                break;

            case ListType.Vertical:
                tmpSize = element.preferredHeight > element.minHeight ? element.preferredHeight : element.minHeight;
                if (tmpSize <= 0) {
                    tmpSize = defaultSize.y;
                    element.minHeight = element.preferredHeight = defaultSize.y;
                }
                height += tmpSize;
                break;
            }
        }

        rectTransform.sizeDelta = new Vector2(width, height);
    }

    private void UpdateContentLayoutGroup()
    {
		switch (type) {
		case ListType.Vertical:
			VerticalLayoutGroup vGroup = contentTransform.GetOrAddComponent<VerticalLayoutGroup>();
			vGroup.childForceExpandWidth = true;
			vGroup.childForceExpandHeight = false;
			break;
		case ListType.Horizontal:
			HorizontalLayoutGroup hGroup = contentTransform.GetOrAddComponent<HorizontalLayoutGroup>();
			hGroup.childForceExpandWidth = false;
			hGroup.childForceExpandHeight = true;
			break;
		}	
	}

    private void StartListeningItemEvents()
    {
        SortableListItem[] items = GetItems();
        foreach (SortableListItem item in items) {
            item.OnItemPressed += OnItemPressed;
        }
    }

    private void StopListeningItemEvents()
    {
        SortableListItem[] items = GetItems();
        foreach (SortableListItem item in items) {
            item.OnItemReleased += OnItemReleased;
        }
    }

    private void OnItemPressed(SortableListItem sortableListItem)
    {
        if (Input.GetMouseButton(0) && OnListItemPressed != null) {
            OnListItemPressed(this, sortableListItem);
        }
    }

    private void OnItemReleased(SortableListItem sortableListItem)
    {
        if (OnListItemReleased != null) {
            OnListItemReleased(this, sortableListItem);
        }
    }
}