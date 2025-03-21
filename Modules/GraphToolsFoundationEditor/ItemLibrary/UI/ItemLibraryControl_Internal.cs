// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// Main Item Library element: search bar, list view and preview/details panel
    /// </summary>
    class ItemLibraryControl_Internal : VisualElement
    {
        const string k_TemplateFileName = "UXML/ItemLibrary/ItemLibraryWindow.uxml";
        const string k_StylesheetName = "StyleSheets/ItemLibrary/ItemLibrary.uss";

        // Window constants.
        const string k_WindowTitleContainer = "windowTitleContainer";
        const string k_DetailsPanelToggleName = "detailsPanelToggle";
        const string k_WindowTitleLabel = "windowTitleLabel";
        const string k_SearchBoxContainerName = "windowSearchBoxVisualContainer";
        const string k_WindowDetailsPanel = "windowDetailsVisualContainer";
        const string k_WindowResultsScrollViewName = "windowResultsScrollView";
        const string k_WindowSearchTextFieldName = "searchBox";
        const string k_WindowAutoCompleteLabelName = "autoCompleteLabel";
        const string k_SearchPlaceholderLabelName = "searchPlaceholderLabel";
        const string k_WindowResizerName = "windowResizer";
        const string k_LibraryPanel = "libraryViewContainer";
        const string k_ConfirmButtonName = "confirmButton";
        const string k_StatusLabelName = "statusLabel";
        const int k_TabCharacter = 9;

        public static float DefaultSearchPanelWidth => 300f;
        public static float DefaultDetailsPanelWidth => 200f;
        public static float DefaultHeight => 300f;

        const int k_DefaultExtraWidthForDetailsPanel = 12;

        const string k_ControlClassName = "unity-item-library-control";
        const string k_ControlMultiSelectClassName = k_ControlClassName + "--multiselect";
        const string k_PreviewToggleClassName = "unity-item-library-preview-toggle";
        const string k_DetailsToggleCheckedClassName = k_PreviewToggleClassName + "--checked";

        const string k_HideDetailsTooltip = "Hide Preview";
        const string k_ShowDetailsTooltip = "Show Preview";
        const string k_DetailsPanelHiddenClassName = "hidden";
        const string k_SearchplaceholderlabelHiddenClassName = "searchPlaceholderLabel--hidden";

        const string k_IndexingDatabaseStatusText = "Indexing Databases...";

        // internal accessors for tests
        internal ItemLibraryLibrary_Internal Library_Internal => m_Library;
        internal VisualElement DetailsPanel_Internal => m_DetailsPanel;
        internal VisualElement Resizer_Internal => m_Resizer;
        internal VisualElement TitleContainer_Internal => m_TitleContainer;
        internal Label TitleLabel_Internal => m_TitleLabel;
        internal Toggle DetailsToggle_Internal => m_DetailsToggle;
        internal TextField SearchTextField_Internal => m_SearchTextField;
        internal static string DetailsPanelHiddenClassName_Internal => k_DetailsPanelHiddenClassName;
        internal bool IsIndexing_Internal { get; private set; }
        internal static string IndexingDatabaseStatusText_Internal => k_IndexingDatabaseStatusText;

        string m_PendingStatusBarText;
        public string StatusBarText
        {
            get => m_StatusLabel.text;
            set
            {
                if (IsIndexing_Internal)
                    m_PendingStatusBarText = value;
                else
                    m_StatusLabel.text = value;
            }
        }

        float m_DetailsPanelExtraWidth;
        Label m_AutoCompleteLabel;
        Label m_SearchPlaceholderLabel;
        Label m_StatusLabel;
        ItemLibraryLibrary_Internal m_Library;
        string m_SuggestedCompletion;
        string m_Text = string.Empty;

        internal event Action<ItemLibraryAnalyticsEvent_Internal> analyticsEventTriggered_Internal;
        internal event Action<ItemLibraryItem> itemChosen_Internal;

        internal event Action<float> detailsPanelWidthChanged_Internal;

        VisualElement m_DetailsPanel;
        LibraryTreeView_Internal m_TreeView;
        TextField m_SearchTextField;
        VisualElement m_TitleContainer;
        VisualElement m_SearchTextInput;
        VisualElement m_LibraryPanel;
        Toggle m_DetailsToggle;
        VisualElement m_Resizer;
        Label m_TitleLabel;

        public ItemLibraryControl_Internal()
        {
            IsIndexing_Internal = true;
            this.AddStylesheetResourceWithSkinVariant_Internal(k_StylesheetName);

            var windowUxmlTemplate = EditorGUIUtility.Load(k_TemplateFileName) as VisualTreeAsset;
            VisualElement rootElement = windowUxmlTemplate.CloneTree();
            rootElement.AddToClassList("content");
            rootElement.AddToClassList("unity-theme-env-variables");
            rootElement.AddToClassList("item-library-theme");
            rootElement.StretchToParentSize();
            Add(rootElement);

            var listView = this.Q<ListView>(k_WindowResultsScrollViewName);

            if (listView != null)
            {
                m_TreeView = new LibraryTreeView_Internal
                {
                    fixedItemHeight = 25,
                    focusable = true,
                    tabIndex = 1
                };
                m_TreeView.OnModelViewSelectionChange += OnTreeviewSelectionChange;
                var listViewParent = listView.parent;
                listViewParent.Insert(0, m_TreeView);
                listView.RemoveFromHierarchy();
            }

            m_TitleContainer = this.Q(k_WindowTitleContainer);

            var searchBox = this.Q(k_SearchBoxContainerName);
            searchBox.AddToClassList(SearchFieldBase<TextField, string>.ussClassName);

            m_DetailsPanel = this.Q(k_WindowDetailsPanel);

            m_TitleLabel = this.Q<Label>(k_WindowTitleLabel);

            m_LibraryPanel = this.Q(k_LibraryPanel);

            m_DetailsToggle = this.Q<Toggle>(k_DetailsPanelToggleName);
            m_DetailsToggle.AddToClassList(k_PreviewToggleClassName);

            m_SearchTextField = this.Q<TextField>(k_WindowSearchTextFieldName);
            if (m_SearchTextField != null)
            {
                m_SearchTextField.focusable = true;
                m_SearchTextField.RegisterCallback<InputEvent>(OnSearchTextFieldTextChanged);

                m_SearchTextInput = m_SearchTextField.Q(TextInputBaseField<string>.textInputUssName);
                m_SearchTextInput.RegisterCallback<KeyDownEvent>(OnSearchTextFieldKeyDown, TrickleDown.TrickleDown);

                m_SearchTextField.AddToClassList(TextInputBaseField<string>.ussClassName);
            }

            m_AutoCompleteLabel = this.Q<Label>(k_WindowAutoCompleteLabelName);
            m_SearchPlaceholderLabel = this.Q<Label>(k_SearchPlaceholderLabelName);
            m_StatusLabel = this.Q<Label>(k_StatusLabelName);

            m_Resizer = this.Q(k_WindowResizerName);

            var confirmButton = this.Q<Button>(k_ConfirmButtonName);
            confirmButton.clicked += m_TreeView.ConfirmMultiselect_Internal;

            // TODO: HACK - ListView's scroll view steals focus using the scheduler.
            EditorApplication.update += HackDueToListViewScrollViewStealingFocus;

            style.flexGrow = 1;
            m_DetailsPanelExtraWidth = k_DefaultExtraWidthForDetailsPanel;
        }

        void OnTreeviewSelectionChange(IReadOnlyList<ITreeItemView_Internal> selection)
        {
            var selectedItems = selection
                .OfType<IItemView_Internal>()
                .Select(siv => siv.Item)
                .ToList();
            m_Library.Adapter.OnSelectionChanged(selectedItems);
            if (m_Library.Adapter.HasDetailsPanel)
            {
                m_Library.Adapter.UpdateDetailsPanel(selectedItems.FirstOrDefault());
            }
        }

        void HackDueToListViewScrollViewStealingFocus()
        {
            m_SearchTextInput?.Focus();
            // ReSharper disable once DelegateSubtraction
            EditorApplication.update -= HackDueToListViewScrollViewStealingFocus;
        }

        public void Setup(ItemLibraryLibrary_Internal library)
        {
            m_Library = library;

            if (!string.IsNullOrEmpty(library?.Adapter.CustomStyleSheetPath))
                this.AddStylesheetAssetWithSkinVariant_Internal(library.Adapter.CustomStyleSheetPath);

            m_TreeView.Setup(library, OnItemChosen);

            if (m_Library.Adapter.MultiSelectEnabled)
            {
                AddToClassList(k_ControlMultiSelectClassName);
            }

            m_DetailsPanel.EnableInClassList(k_DetailsPanelHiddenClassName, !m_Library.Adapter.HasDetailsPanel);

            if (m_Library.Adapter.HasDetailsPanel)
            {
                m_Library.Adapter.InitDetailsPanel(m_DetailsPanel);
                ResetSplitterRatio();
                m_DetailsPanel.style.flexGrow = m_Library.Adapter.InitialSplitterDetailRatio;
                m_LibraryPanel.style.flexGrow = 1;

                var showPreview = m_Library.IsPreviewPanelVisible();
                m_DetailsToggle.SetValueWithoutNotify(showPreview);
                SetDetailsPanelVisibility(showPreview);
                m_DetailsToggle.RegisterValueChangedCallback(OnDetailsToggleValueChange);
                m_TitleContainer.Add(m_DetailsToggle);
            }
            else
            {
                var splitter = m_DetailsPanel.parent;

                splitter.parent.Insert(0, m_LibraryPanel);
                splitter.parent.Insert(1, m_DetailsPanel);

                splitter.RemoveFromHierarchy();
            }

            m_TitleLabel.text = m_Library.Adapter.Title;
            if (string.IsNullOrEmpty(m_TitleLabel.text))
            {
                m_TitleLabel.parent.style.visibility = Visibility.Hidden;
                m_TitleLabel.parent.style.position = Position.Absolute;
            }

            // leave some time for library to have time to display before refreshing
            // Otherwise if Refresh takes long, the library isn't shown at all until refresh is done
            // this happens the first time you open the library and your items have a lengthy "Indexing" process
            if (!IsIndexing_Internal && !string.IsNullOrEmpty(m_StatusLabel.text)) // edge case where label could have been set
                m_PendingStatusBarText = m_StatusLabel.text;
            IsIndexing_Internal = true;
            m_StatusLabel.text = k_IndexingDatabaseStatusText;
            schedule.Execute(delegate()
            {
                Refresh();
                IsIndexing_Internal = false;
                StatusBarText = m_PendingStatusBarText;
            }).ExecuteLater(100);
        }

        void OnItemChosen(ItemLibraryItem item)
        {
            var eventType = item == null ? ItemLibraryAnalyticsEventKind_Internal.Cancelled : ItemLibraryAnalyticsEventKind_Internal.Picked;
            analyticsEventTriggered_Internal?.Invoke(new ItemLibraryAnalyticsEvent_Internal(eventType, m_SearchTextField.value));
            itemChosen_Internal?.Invoke(item);
        }

        void ResetSplitterRatio()
        {
            m_DetailsPanel.style.flexGrow = 1;
            m_LibraryPanel.style.flexGrow = 1;
        }

        void SetDetailsPanelVisibility(bool showDetails)
        {
            // if details panel is still visible, store width that isn't taken into account by splitter ratio
            if (!m_DetailsPanel.ClassListContains("hidden"))
            {
                var widthDiff = m_DetailsPanel.resolvedStyle.paddingLeft + m_DetailsPanel.resolvedStyle.paddingRight +
                                m_DetailsPanel.resolvedStyle.marginLeft + m_DetailsPanel.resolvedStyle.marginRight +
                                m_DetailsPanel.resolvedStyle.borderLeftWidth +
                                m_DetailsPanel.resolvedStyle.borderRightWidth;
                if (widthDiff > 0.4f || widthDiff < -0.4f)
                {
                    m_DetailsPanelExtraWidth = widthDiff;
                }
            }

            m_DetailsToggle.EnableInClassList(k_DetailsToggleCheckedClassName, showDetails);
            m_DetailsToggle.tooltip = showDetails ? k_HideDetailsTooltip : k_ShowDetailsTooltip;

            // hide or show the details/preview element
            m_DetailsPanel.EnableInClassList("hidden", !showDetails);

            // Move elements in or out of the splitter and disable it depending on visibility
            VisualElement splitter;
            if (!showDetails)
            {
                splitter = m_DetailsPanel.parent;
                splitter.parent.Add(m_LibraryPanel);
                splitter.SetEnabled(false);
                m_LibraryPanel.style.flexGrow = 1;
            }
            else
            {
                splitter = m_DetailsPanel.parent.Q("splitter");
                splitter.SetEnabled(true);
                splitter.Insert(0, m_LibraryPanel);
            }
        }

        void OnDetailsToggleValueChange(ChangeEvent<bool> evt)
        {
            var showDetails = evt.newValue;
            var rightPartWidth = m_DetailsPanel.resolvedStyle.width;
            SetDetailsPanelVisibility(showDetails);
            m_Library.SetPreviewPanelVisibility(showDetails);
            if (showDetails)
            {
                var leftPartWidth = m_LibraryPanel.resolvedStyle.width;
                var panelsRatio = m_LibraryPanel.resolvedStyle.flexGrow / m_DetailsPanel.resolvedStyle.flexGrow;
                rightPartWidth = leftPartWidth / panelsRatio + m_DetailsPanelExtraWidth;
            }

            detailsPanelWidthChanged_Internal?.Invoke(showDetails ? + rightPartWidth : - rightPartWidth);
        }

        void Refresh()
        {
            var query = m_Text;
            var noQuery = string.IsNullOrEmpty(query);

            m_SearchPlaceholderLabel.EnableInClassList(k_SearchplaceholderlabelHiddenClassName, !string.IsNullOrEmpty(query));
            var results = m_Library.Search(query).ToList();

            m_SuggestedCompletion = string.Empty;

            if (results.Any() && !noQuery)
            {
                m_SuggestedCompletion = GetAutoCompletionSuggestion(query, results);
            }

            m_TreeView.ViewMode =
                noQuery ? ResultsViewMode.Hierarchy : ResultsViewMode.Flat;
            m_TreeView.SetResults_Internal(results);
        }

        static string GetAutoCompletionSuggestion(string query, IReadOnlyList<ItemLibraryItem> results)
        {
            var bestMatch = results
                .Select(si => si.Name)
                .FirstOrDefault(n => n.StartsWith(query, StringComparison.OrdinalIgnoreCase));
            if (bestMatch != null && bestMatch.Length > query.Length && bestMatch[query.Length] != ' ')
            {
                var lastSpace = bestMatch.IndexOf(' ', query.Length);
                var completionSize = lastSpace == -1 ? bestMatch.Length : lastSpace;
                var autoCompletionSuggestion = bestMatch.Substring(query.Length, completionSize - query.Length);
                return autoCompletionSuggestion;
            }
            return string.Empty;
        }

        void OnSearchTextFieldTextChanged(InputEvent inputEvent)
        {
            var text = inputEvent.newData;

            if (string.Equals(text, m_Text))
                return;

            // This is necessary due to OnTextChanged(...) being called after user inputs that have no impact on the text.
            // Ex: Moving the caret.
            m_Text = text;

            // If backspace is pressed and no text remain, clear the suggestion label.
            if (string.IsNullOrEmpty(text))
            {
                // Display the unfiltered results list.
                Refresh();

                m_AutoCompleteLabel.text = String.Empty;
                m_SuggestedCompletion = String.Empty;

                return;
            }

            Refresh();

            if (!string.IsNullOrEmpty(m_SuggestedCompletion))
            {
                m_AutoCompleteLabel.text = text + m_SuggestedCompletion;
            }
            else
            {
                m_AutoCompleteLabel.text = String.Empty;
            }
        }

        void OnSearchTextFieldKeyDown(KeyDownEvent keyDownEvent)
        {
            // First, check if we cancelled the search.
            if (keyDownEvent.keyCode == KeyCode.Escape)
            {
                itemChosen_Internal?.Invoke(null);
                keyDownEvent.StopPropagation();
                return;
            }

            // For some reason the KeyDown event is raised twice when entering a character.
            // As such, we ignore one of the duplicate event.
            // This workaround was recommended by the Editor team. The cause of the issue relates to how IMGUI works
            // and a fix was not in the works at the moment of this writing.
            if (keyDownEvent.character == k_TabCharacter)
            {
                // Prevent switching focus to another visual element.
                keyDownEvent.PreventDefault();

                return;
            }

            // If Tab is pressed, complete the query with the suggested term.
            if (keyDownEvent.keyCode == KeyCode.Tab)
            {
                // Used to prevent the TAB input from executing it's default behavior. We're hijacking it for auto-completion.
                keyDownEvent.PreventDefault();

                if (!string.IsNullOrEmpty(m_SuggestedCompletion))
                {
                    SelectAndReplaceCurrentWord();
                    m_AutoCompleteLabel.text = string.Empty;

                    // TODO: Revisit, we shouldn't need to do this here.
                    m_Text = m_SearchTextField.text;

                    Refresh();

                    m_SuggestedCompletion = string.Empty;
                }
            }
            else
            {
                keyDownEvent.StopPropagation();
                using (var eKeyDown = KeyDownEvent.GetPooled(keyDownEvent.character, keyDownEvent.keyCode,
                    keyDownEvent.modifiers))
                {
                    eKeyDown.target = m_TreeView;
                    SendEvent(eKeyDown);
                }
            }
        }

        void SelectAndReplaceCurrentWord()
        {
            var newText = m_SearchTextField.value + m_SuggestedCompletion;

            // Wait for SelectRange api to reach trunk
            //#if UNITY_2018_3_OR_NEWER
            //            m_SearchTextField.value = newText;
            //            m_SearchTextField.SelectRange(m_SearchTextField.value.Length, m_SearchTextField.value.Length);
            //#else
            // HACK - relies on the textfield moving the caret when being assigned a value and skipping
            // all low surrogate characters
            var magicMoveCursorToEndString = new string('\uDC00', newText.Length);
            m_SearchTextField.value = magicMoveCursorToEndString;
            m_SearchTextField.value = newText;

            //#endif
        }
    }
}
