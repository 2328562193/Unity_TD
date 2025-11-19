public class InputUtils {

    private static Camera mainCamera {
        get {
            if (_mainCamera == null) _mainCamera = Camera.main;
            return _mainCamera;
        }
    }
    private static Camera _mainCamera;
    private static readonly LayerMask interactable2DLayers = LayerMask.GetMask("Interactable");
    private class SortingOrderComparer : IComparer<IInteractable> {
        public int Compare(IInteractable x, IInteractable y) {
            if (x == null && y == null) return 0;
            if (x == null) return 1;
            if (y == null) return -1;

            int orderX = x.Priority;
            int orderY = y.Priority;
            return orderY.CompareTo(orderX); // 高 sortingOrder 优先
        }
    }
    private static readonly IComparer<IInteractable> SortingOrderComparerInstance = new SortingOrderComparer();
    private static readonly Collider2D[] worldResults = new Collider2D[10];
    private static readonly IInteractable[] interactableResults = new IInteractable[10];
    private static readonly PointerEventData eventData = new PointerEventData(null);
    private static readonly List<RaycastResult> uiResults = new List<RaycastResult>();
    private static readonly ContactFilter2D defaultContactFilter = new ContactFilter2D {
        useTriggers = true,
        layerMask = interactable2DLayers
    };

    public static void SetMainCamera(Camera cam){
        _mainCamera = cam;
    }

    public static bool WorldInteractable<T>(out T component) where T : class, IInteractable {
        if (IsPointerOverUI()) return null;
        Vector2 mousePosition = InputManager.GetInput<MousePosition>(InputEnum.MousePosition).value;
        return Physics2DRaycast<T>(mousePosition, out component);
    }

    public static bool GetUIByMouse<T>(out T component) where T : Component, IInteractable {
        Vector2 mousePosition = InputManager.GetInput<MousePosition>(InputEnum.MousePosition).value;
        return GetUIByPosition<T>(mousePosition, out component);
    }

    public static bool GetUIAtPosition<T>(Vector2 screenPos, out T component) where T : Component {
        component = null;
        if (EventSystem.current == null) {
            GameLogger.LogWarning("EventSystem.current is null.");
            return false;
        }
        eventData.position = screenPos;
        uiResults.Clear();
        EventSystem.current.RaycastAll(eventData, uiResults);

        foreach (var result in uiResults) {
            if (result.gameObject.TryGetComponent<T>(out component)) {
                return true;
            }
        }
        return false;
    }

    private static bool IsPointerOverUI() {
        if (EventSystem.current == null) {
            GameLogger.LogWarning("EventSystem.current is null.");
            return false;
        }
        return EventSystem.current.IsPointerOverGameObject();
    }

    private static bool IsPointerOverUI(Vector2 screenPos) {
        if (EventSystem.current == null) {
            GameLogger.LogWarning("EventSystem.current is null.");
            return false;
        }
        eventData.position = screenPos;
        uiResults.Clear();

        EventSystem.current.RaycastAll(eventData, uiResults);
        return uiResults.Count > 0;
    }

    private static bool Physics2DRaycast<T>(Vector2 screenPos, out T component) where T : Component, IInteractable {
        component = null;
        if (mainCamera == null) {
            GameLogger.LogWarning("mainCamera is null.");
            return false;
        }
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(screenPos);

        int hitCount = Physics2D.OverlapPoint(worldPoint, defaultContactFilter, worldResults);
        if (hitCount == 0) return false;

        for (int i = 0; i < hitCount; i++) {
            var col = worldResults[i];
            if (col != null && col.gameObject.TryGetComponent<T>(out component)) {
                interactableResults[i] = component;
            } else {
                interactableResults[i] = null;
            }
        }
        Array.Sort(interactableResults, 0, hitCount, SortingOrderComparerInstance);

        if (interactableResults[0] == null) return false;
        component = interactableResults[0] as T;
        return true;
    }
}