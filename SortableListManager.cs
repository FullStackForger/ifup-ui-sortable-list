using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace ifup.ui
{

    // todo: 
    // - override draggingDelay on each list

    public class SortableListManager : Singleton<SortableListManager>
    {
        public float draggingDelay = 0.25f;

        private List<SortableList> m_sortableLists = new List<SortableList>();
        private SortableListItem m_draggedItem;
        private SortableListItem m_mockItem;

        private SortableList m_sourceList;
        private int m_sourceItemIndex = -1;

        private SortableList m_targetList;
        private List<Rect> m_cornersList = new List<Rect>();
        private int m_targetItemIndex = -1;

        private bool m_globalScrollLock = false;
        private bool m_dragPrepping = false;
        private bool m_dragActivated = false;
        private float m_pressTime0 = 0;
        private Vector3 m_pressPosition0 = Vector3.zero;

        protected void Start()
        {
            GameObject go = new GameObject("Mock List Item");
            go.AddComponent<RectTransform>();
            go.AddComponent<CanvasRenderer>();
            go.AddComponent<LayoutElement>();
            m_mockItem = go.AddComponent<SortableListItem>();
            m_mockItem.gameObject.SetActive(false);
        }

        protected void Update()
        {
            if (m_dragPrepping || m_dragActivated) {

                UpdateActiveList();

                if (m_dragPrepping && Time.time - m_pressTime0 > draggingDelay && m_pressPosition0 == Input.mousePosition) {
                    StartDragging();
                }

                if (m_dragActivated) {
                    if (Input.GetMouseButtonUp(0)) {
                        StopDragging();
                        AttachDraggedItem();
                    } else {
                        UpdateDraggingPosition();
                    }
                }
            }
        }

        public void Register(SortableList sortableList)
        {
            if (!m_sortableLists.Contains(sortableList)) {
                m_sortableLists.Add(sortableList);
                sortableList.OnListItemPressed += OnListItemPressed;
            }
        }

        public void Unregister(SortableList sortableList)
        {
            if (m_sortableLists.Contains(sortableList)) {
                m_sortableLists.Remove(sortableList);
                sortableList.OnListItemReleased += OnListItemReleased;
            }
        }

        public bool scrollLock
        {
            get
            {
                return m_globalScrollLock;
            }
        }

        private void OnListItemPressed(SortableList sortableList, SortableListItem sortableListItem)
        {
            m_sourceList = sortableList;
            m_draggedItem = sortableListItem;
            m_sourceItemIndex = m_draggedItem.transform.GetSiblingIndex();
            m_dragPrepping = true;
            m_pressTime0 = Time.time;
            m_pressPosition0 = Input.mousePosition;
        }

        private void OnListItemReleased(SortableList SortableList, SortableListItem sortableListItem)
        {
            m_sourceList = null;
            m_draggedItem = null;
            m_dragPrepping = false;
            m_pressTime0 = 0;
            m_pressPosition0 = Vector3.zero;
        }

        private void UpdateDraggingPosition()
        {
            if (m_draggedItem == null) return;
            m_draggedItem.transform.position = Input.mousePosition;
        }

        private void CacheListItemCorners()
        {
            Debug.Log("caching");
            m_cornersList = new List<Rect>();                     
            foreach (SortableListItem listItem in m_targetList.GetItems()) {
                Vector3[] itemCorners = new Vector3[4];
                (listItem.transform as RectTransform).GetWorldCorners(itemCorners);
                Rect rect = new Rect() {
                    x = itemCorners[0].x,
                    y = itemCorners[0].y,
                    width = itemCorners[2].x - itemCorners[0].x,
                    height = itemCorners[2].y - itemCorners[0].y,
                };
                m_cornersList.Add(rect);
            }
        }

        private void UpdateActiveList()
        {
            if (m_dragActivated == false) return;
    
            foreach (SortableList sortableList in m_sortableLists) {
                if (IsMouseOverRectTransform(sortableList.transform as RectTransform)) {
                    bool listChanged = m_targetList != sortableList;
                    m_targetList = sortableList;
                    if (listChanged) CacheListItemCorners();
                    break;
                }
            }
           
            if (m_targetList == null) return;
        
            m_mockItem.gameObject.SetActive(true);
            if (m_mockItem == null || m_mockItem.layoutElement == null) return;

            int prevIndex = m_targetItemIndex;
            int itemIndex = 0;
            foreach (Rect rect in m_cornersList) {
                Vector3[] itemCorners = new Vector3[4];
              
                if (IsMouseOverRect(rect)) {
                    Debug.Log("new index: " + itemIndex);                   
                    break;
                }

                itemIndex++;          
            }

            m_mockItem.layoutElement.minWidth = m_draggedItem.layoutElement.minWidth;
            m_mockItem.layoutElement.minHeight = m_draggedItem.layoutElement.minHeight;
            m_mockItem.layoutElement.preferredWidth = m_draggedItem.layoutElement.preferredWidth;
            m_mockItem.layoutElement.preferredHeight = m_draggedItem.layoutElement.preferredHeight;

           
            m_targetItemIndex = itemIndex;
           

            if (prevIndex == m_targetItemIndex) return;

            Debug.Log("ataching mock at index: " + m_targetItemIndex);
            m_targetList.AttachItem(m_mockItem, m_targetItemIndex);
            m_sourceList.UpdateContentSize();

        }

        private void StartDragging()
        {
            m_dragActivated = true;
            m_dragPrepping = false;
            ToggleScrollLock(true);
            m_sourceList.DetachItem(m_draggedItem, m_sourceList.canvas.transform);
        }

        private void StopDragging()
        {
            m_dragActivated = false;
            ToggleScrollLock(false);
        }

        private void AttachDraggedItem()
        {
            int itemIndex = m_targetItemIndex;
            SortableList sortableList = m_targetList;

            if (m_targetList == null) {
                m_targetList = m_sourceList;
                itemIndex = m_sourceItemIndex;
            }

            Debug.Log("detaching mock");
            m_targetList.DetachItem(m_mockItem, m_targetList.canvas.transform);
            m_mockItem.gameObject.SetActive(false);

            Debug.Log("ataching item");
            sortableList.AttachItem(m_draggedItem, itemIndex);

            m_draggedItem = null;
            m_targetList = null;
            m_targetItemIndex = -1;
            m_sourceList = null;
            m_sourceItemIndex = -1;
        }

        private bool IsMouseOverRectTransform(RectTransform rt)
        {
            Vector2 mousePosition = Input.mousePosition;
            Vector3[] worldCorners = new Vector3[4];
            rt.GetWorldCorners(worldCorners);

            return IsMouseOverRect(new Rect(worldCorners[0], worldCorners[2] - worldCorners[0]));
        }

        private bool IsMouseOverRect(Rect rect)
        {
            Vector2 mousePosition = Input.mousePosition;
            if (mousePosition.x >= rect.x && mousePosition.x < rect.x + rect.width
               && mousePosition.y >= rect.y && mousePosition.y < rect.y + rect.height) {
                return true;
            }
            return false;
        }

        private void ToggleScrollLock(bool value)
        {
            foreach (SortableList sortableList in m_sortableLists) {
                sortableList.scrollLock = value;
            }
            m_globalScrollLock = value;
        }
    }
}
 