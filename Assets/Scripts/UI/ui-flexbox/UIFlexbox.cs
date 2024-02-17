using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityPackages.UI {
	public enum FlexDirection {
		Row,
		Column
	}

	public enum AlignSelf {
		Stretch,
		Start,
		Center,
		End
	}

	public enum JustifyContent {
		Stretch,
		Start,
		Center,
		End,
	}

	public enum ContentSpacing {
		None,
		SpaceAround,
		SpaceBetween
	}

	public enum BasisType {
		Percent,
		Pixels
	}

	[System.Serializable]
	public class FlexItem {
		public int grow = 1;
		public int basis = 0;
		public BasisType basisType = BasisType.Percent;
		public AlignSelf alignSelf = AlignSelf.Stretch;

		[System.NonSerialized]
		public float runtimeItemSize = 0;

		[System.NonSerialized]
		public RectTransform runtimeRectTransform = null;
	}

	[ExecuteInEditMode]
	[AddComponentMenu ("UI/Flexbox")]
	public class UIFlexbox : MonoBehaviour {

		/// The direction of the flex layout
		public FlexDirection flexDirection = FlexDirection.Row;

		// Defines how to justify the content.
		public JustifyContent justifyContent = JustifyContent.Stretch;

		/// The size of justified items.
		public int itemSize;

		// The type of content spacing
		public ContentSpacing contentSpacing = ContentSpacing.None;

		/// The spacing for Justify Content "space between" and "-around"
		public int spacing;

		public bool includeInactiveChildren = false;

		[Tooltip("Expand container to fit children with sizing. Custom, not very well tested, and probably incompatible with a lot of other options")]
		public bool expandContainer = false;

		/// Flex item to use if the list doesn't contain an item for the child
		public FlexItem defaultFlexItem = new FlexItem();

		//// The list of flex items.
		public List<FlexItem> flexItems = new List<FlexItem> ();

		private RectTransform _rectTransform;
		RectTransform rectTransform {
			get{
				if(_rectTransform == null){
					_rectTransform = this.GetComponent<RectTransform> ();
				}
				return _rectTransform;
			}
		}

		private float _containerSize;
		private int _childCount;
		private int _redrawCount = 0;

		private void Awake () {
			// this.Draw ();
		}

		private void LateUpdate () {
			if (NeedsUpdate()){
				_redrawCount++;

				//When exiting play mode in the editor the first time it gets updated the container size is
				//A negative wrong number.  Added this hack to avoid redrawing that first time with the wrong size
				//To prevent unncessary saving of the scene
				if(!Application.isPlaying && _redrawCount <= 1){
					return;
				}
				this.Draw ();
			}
		}

		private bool NeedsUpdate(){
			var newContainerSize = this.GetContainerSize (rectTransform);
			if(_containerSize != newContainerSize){
				// Debug.Log($"Container size changed. Old {_containerSize} New {newContainerSize}");
				return true;
			}
			if(_childCount != GetChildCount()){
				// Debug.Log("Child count changed");
				return true;
			}
			return false;
		}


		/// Formats the child components
		public void Draw () {
			_containerSize = this.GetContainerSize (rectTransform);
			_childCount = GetChildCount();
			// Debug.Log($"Draw. Container size {_containerSize} Child Count {_childCount} Frame {Time.frameCount}");
			var _flexItemRelativeSize = 0f;
			var _spacing = 0f;
			var _sizing = 0f;
			var _cursor = 0f;
			var _index = -1;
			var growTotal = EnumerateFlexItems().Sum(x => x.grow);
			var maxItemRelativeSize = expandContainer ? itemSize : _containerSize / growTotal;

			// Calculates the flex item relative size according to
			// the current justify Content mode.
			switch (this.justifyContent) {

				// ... Stretching
				case JustifyContent.Stretch:
					_flexItemRelativeSize = maxItemRelativeSize;
					break;

					// ... Start
				case JustifyContent.Start:
					_flexItemRelativeSize = Mathf.Clamp(this.itemSize, this.itemSize, maxItemRelativeSize);
					break;

					// ... End
				case JustifyContent.End:
					_flexItemRelativeSize = Mathf.Clamp(this.itemSize, this.itemSize, maxItemRelativeSize);
					_cursor = (_containerSize - (_flexItemRelativeSize * _childCount));
					if (this.flexDirection == FlexDirection.Column) _cursor *= -1;
					break;

					// ... Center
				case JustifyContent.Center:
					_flexItemRelativeSize = Mathf.Clamp(this.itemSize, this.itemSize, maxItemRelativeSize);
					_cursor = (_containerSize - (_flexItemRelativeSize * _childCount)) * .5f;
					if (this.flexDirection == FlexDirection.Column) _cursor *= -1;
					break;
			}

			// Adds spacing to the items
			switch (this.contentSpacing) {
				// ... Space Between
				case ContentSpacing.SpaceBetween:
					_spacing = this.spacing;
					_spacing -= (float) this.spacing / (float) _childCount;
					_sizing = this.spacing;
					_sizing -= ((_sizing + _spacing) / (float) _childCount);
					break;

					// ... Space Between
				case ContentSpacing.SpaceAround:
					_spacing = this.spacing;
					_spacing -= (float) this.spacing / (float) _childCount;
					_sizing = this.spacing;
					_sizing -= ((_sizing - _spacing) / (float) _childCount);
					_cursor += this.flexDirection == FlexDirection.Column ? -_spacing : _spacing;
					break;
			}

			// Now we're going to set each flex item
			foreach (var _child in EnumerateChildren()) {
				_index++;
				var _flexItem = GetFlexItemForChild(_index);
				_flexItem.runtimeItemSize = _flexItemRelativeSize * _flexItem.grow;
				_flexItem.runtimeRectTransform = _child.GetComponent<RectTransform> ();

				// Disabled both of these for tweening in the stage buttons
				// _flexItem.runtimeRectTransform.hideFlags = HideFlags.NotEditable;
				// _flexItem.runtimeRectTransform.localScale = Vector3.one;

				// Flex Direction
				switch (this.flexDirection) {

					// ... Column
					case FlexDirection.Column:
						switch (_flexItem.alignSelf) {
							case AlignSelf.Stretch:
							case AlignSelf.Start:
								this.SetAnchorPreset (_flexItem, 0, 1);
								break;
							case AlignSelf.End:
								this.SetAnchorPreset (_flexItem, 1, 1);
								break;
							case AlignSelf.Center:
								this.SetAnchorPreset (_flexItem, .5f, 1);
								break;
						}
						_flexItem.runtimeRectTransform.anchoredPosition = new Vector3 (0, _cursor);
						_flexItem.runtimeRectTransform.SetSizeWithCurrentAnchors (
							RectTransform.Axis.Vertical,
							_flexItem.runtimeItemSize - _sizing);
						if (_flexItem.alignSelf == AlignSelf.Stretch) {
							_flexItem.runtimeRectTransform.SetSizeWithCurrentAnchors (
								RectTransform.Axis.Horizontal,
								rectTransform.rect.width);
						} else if (_flexItem.basisType == BasisType.Percent) {
							_flexItem.runtimeRectTransform.SetSizeWithCurrentAnchors (
								RectTransform.Axis.Horizontal,
								rectTransform.rect.width * (_flexItem.basis / 100f));
						} else if (_flexItem.basisType == BasisType.Pixels) {
							_flexItem.runtimeRectTransform.SetSizeWithCurrentAnchors (
								RectTransform.Axis.Horizontal,
								_flexItem.basis);
						}
						_cursor -= _flexItem.runtimeItemSize - _sizing;
						if (this.contentSpacing != ContentSpacing.None)
							_cursor -= _spacing;
						break;

						// ... Row
					case FlexDirection.Row:
						switch (_flexItem.alignSelf) {
							case AlignSelf.Stretch:
							case AlignSelf.Start:
								this.SetAnchorPreset (_flexItem, 0, 1);
								break;
							case AlignSelf.End:
								this.SetAnchorPreset (_flexItem, 0, 0);
								break;
							case AlignSelf.Center:
								this.SetAnchorPreset (_flexItem, 0, .5f);
								break;
						}
						_flexItem.runtimeRectTransform.anchoredPosition = new Vector3 (_cursor, 0);
						_flexItem.runtimeRectTransform.SetSizeWithCurrentAnchors (
							RectTransform.Axis.Horizontal,
							_flexItem.runtimeItemSize - _sizing
						);

						if (_flexItem.alignSelf == AlignSelf.Stretch) {
							_flexItem.runtimeRectTransform.SetSizeWithCurrentAnchors (
								RectTransform.Axis.Vertical,
								rectTransform.rect.height);
						} else if (_flexItem.basisType == BasisType.Percent) {
							_flexItem.runtimeRectTransform.SetSizeWithCurrentAnchors (
								RectTransform.Axis.Vertical,
								rectTransform.rect.height * (_flexItem.basis / 100f));
						} else if (_flexItem.basisType == BasisType.Pixels) {
							_flexItem.runtimeRectTransform.SetSizeWithCurrentAnchors (
								RectTransform.Axis.Vertical,
								_flexItem.basis);
						}
						_cursor += _flexItem.runtimeItemSize - _sizing;
						if (this.contentSpacing != ContentSpacing.None)
							_cursor += _spacing;
						break;
				}
			}

			if(expandContainer){
				var newContainerSize = -_cursor;
				switch (this.flexDirection) {
					case FlexDirection.Column:
						rectTransform.SetSizeWithCurrentAnchors (
							RectTransform.Axis.Vertical,
							newContainerSize);
						break;
					case FlexDirection.Row:
						rectTransform.SetSizeWithCurrentAnchors (
							RectTransform.Axis.Horizontal,
							newContainerSize);
						break;
				}
			}
		}

		private float GetContainerSize (RectTransform rectTransform) {
			switch (this.flexDirection) {
				case FlexDirection.Row:
					return rectTransform.rect.width;
				default:
				case FlexDirection.Column:
					return rectTransform.rect.height;
			}
		}

		private void SetAnchorPreset (FlexItem flexItem, float x, float y) {
			flexItem.runtimeRectTransform.pivot =
				flexItem.runtimeRectTransform.anchorMax =
				flexItem.runtimeRectTransform.anchorMin = new Vector2 (x, y);
		}

		private void OnDrawGizmosSelected () {
			if (Application.isPlaying == false)
				this.Draw ();
			var _corners = new Vector3[4];
			foreach (var _flexItem in EnumerateFlexItems()) {
				if (_flexItem.runtimeRectTransform == null)
					continue;
				_flexItem.runtimeRectTransform.GetWorldCorners (_corners);
				Gizmos.color = new Color (1, 0, 1);
				Gizmos.DrawLine (_corners[0], _corners[1]);
				Gizmos.DrawLine (_corners[1], _corners[2]);
				Gizmos.DrawLine (_corners[2], _corners[3]);
				Gizmos.DrawLine (_corners[3], _corners[0]);
			}
			this.GetComponent<RectTransform> ().GetWorldCorners (_corners);
			Gizmos.color = new Color (1, 1, 0);
			Gizmos.DrawLine (_corners[0], _corners[1]);
			Gizmos.DrawLine (_corners[1], _corners[2]);
			Gizmos.DrawLine (_corners[2], _corners[3]);
			Gizmos.DrawLine (_corners[3], _corners[0]);
		}

		private FlexItem GetFlexItemForChild(int index){
			if(flexItems.Count > index){
				return flexItems[index];
			}
			return defaultFlexItem;
		}

		private IEnumerable<Transform> EnumerateChildren(){
			for(int i = 0; i < this.transform.childCount; i++){
				var child = this.transform.GetChild(i);
				if(child.gameObject.activeInHierarchy || includeInactiveChildren){
					yield return child;
				}
			}
		}

		private int GetChildCount(){
			// Really should do the below, but that allocates memory every frame, so duplicate logic
			// return EnumerateChildren().Count();
			var count = 0;
			for(int i = 0; i < this.transform.childCount; i++){
				var child = this.transform.GetChild(i);
				if(child.gameObject.activeInHierarchy || includeInactiveChildren){
					count++;
				}
			}
			return count;
		}

		private IEnumerable<FlexItem> EnumerateFlexItems(){
			var i = 0;
			foreach(var child in EnumerateChildren()){
				yield return GetFlexItemForChild(i);
				i++;
			}
		}
	}
}