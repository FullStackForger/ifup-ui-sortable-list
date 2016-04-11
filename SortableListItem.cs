using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ifup.ui
{
    [RequireComponent(typeof(LayoutElement))]
    public class SortableListItem : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler //, IPointerExitHandler
                                               // note: dose interfaces original implemetation colides with scrollRect
                                               // IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public delegate void OnItemPressedEvent(SortableListItem sortableListItem);
        public delegate void OnItemReleasedEvent(SortableListItem sortableListItem);

        public event OnItemPressedEvent OnItemPressed;
        public event OnItemReleasedEvent OnItemReleased;

        public LayoutElement layoutElement;

        protected void Start()
        {
            if (layoutElement == null) {
                layoutElement = GetComponent<LayoutElement>();
                if (layoutElement == null) {
                    throw new MissingComponentException("Layout Element not found. Make sure component is present and set active.");
                }
            }
        }

        #region interfaces implementation

        public void OnPointerDown(PointerEventData eventData)
        {
            if (OnItemPressed != null) {
                OnItemPressed(this);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (OnItemReleased != null) {
                OnItemReleased(this);
            }
        }

        #endregion
    }
}