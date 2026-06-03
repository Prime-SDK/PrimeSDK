using PrimeGames.SDK.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Editor
{

    internal class ToolkitWindow : EditorWindow
    {

        private const string NavigationElementName = "navigation";
        private const string InspectorElementName = "inspector";
        private const string ViewportElementName = "viewport";

        private const string WaitOverlay = "WaitOverlay";
        private const string WaitLabel = "WaitLabel";

        internal static Action OnConfigurationChanged;

        [MenuItem(Naming.PrimeSDK + "/Open Toolkit")]
        public static void Open()
        {
            ToolkitWindow window = GetWindow<ToolkitWindow>();
            window.titleContent.text = $"{Naming.PrimeSDK}";
            window.titleContent.image = null;
            window.minSize = new(1200, 600);
        }

        private Label PrimeSDKInfo => rootVisualElement.Q<Label>("PrimeSDKInfo");
        private Label DebugInfo => rootVisualElement.Q<Label>("DebugInfo");
        private VisualElement UpdatesAvailable => rootVisualElement.Q<VisualElement>("UpdatesAvailable");

        private IEventAggregator eventAggregator;

        private void OnEnable()
        {
            eventAggregator = new EventAggregator();

            rootVisualElement.Clear();
            VisualTreeAsset windowBaseAsset = VisualTreeReference.LoadVisualTree("ToolkitWindowBase");
            VisualElement windowBaseElement = windowBaseAsset.CloneTree();
            windowBaseElement.style.flexGrow = 1;
            rootVisualElement.Add(windowBaseElement);
            InitializeBranding();

            OnConfigurationChanged += UpdateDebugInfo;
            UpdateDebugInfo();

            InitializeToolkitWaitOverlay();
            InitializeToolkitNavigation();
            InitializeToolkitInspector();
            InitializeToolkitViewport();
            RestoreNavigationState();
        }

        private void InitializeBranding()
        {
            Texture2D primeSdkLogo = PackageFiles.FindTextureAsset("PrimeSDK-Light-Small");
            Texture2D primeIcon = PackageFiles.FindTextureAsset("IconPrime");

            rootVisualElement.Q<VisualElement>("HeaderLogo").style.backgroundImage = primeSdkLogo;
            rootVisualElement.Q<VisualElement>("NavigationBrandWatermark").style.backgroundImage = primeIcon;
        }

        private void UpdateDebugInfo()
        {
            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            string unityVersion = Application.unityVersion;
            string systemInfo = SystemInfo.operatingSystem;
            PreferencesEditor preferencesEditor = PreferencesEditor.CreateEditor();
            string configurationName = preferencesEditor.GetBuildConfigurationName();
            if (string.IsNullOrEmpty(configurationName))
            {
                configurationName = "Missing preferences";
            }
            UpdatesAvailable.style.display = DisplayStyle.None;
            PrimeSDKInfo.style.marginRight = 0.0f;
            PrimeSDKInfo.text = $"{nameof(PrimeSDK)} {PrimeSDK.Version}";
            DebugInfo.text = $"| {buildTarget} | {configurationName} | Unity {unityVersion} | {systemInfo}";
        }

        private void OnDisable()
        {
            OnConfigurationChanged -= UpdateDebugInfo;
            eventAggregator.Dispose();
        }

        private VisualElement waitOverlay;
        private Label waitLabel;

        private void InitializeToolkitWaitOverlay()
        {
            waitOverlay = rootVisualElement.Q<VisualElement>(WaitOverlay);
            waitLabel = waitOverlay.Q<Label>(WaitLabel);

            waitOverlay.style.display = DisplayStyle.None;
            EditorEventListener.OnCompilationStarted += ShowWaitOverlay;
            EditorEventListener.OnCompilationFinished += HideWaitOverlay;
        }

        private void ShowWaitOverlay()
        {
            waitOverlay.style.display = DisplayStyle.Flex;
        }

        private void HideWaitOverlay()
        {
            waitOverlay.style.display = DisplayStyle.None;
        }

        #region Navigation

        private enum NavigationItem
        {
            Configurations,
            PackageManager,
            BuildOptimizer
        }

        private const float NavigationPaddingLeft = 15.0f;

        private static NavigationItem CurrentNavigationItem
        {
            get
            {
                int itemId = EditorPrefs.GetInt($"{PackageTools.ProjectId}.{nameof(CurrentNavigationItem)}", 0);
                int minItemId = 0;
                int maxItemId = Enum.GetNames(typeof(NavigationItem)).Length - 1;
                return (NavigationItem)Mathf.Clamp(itemId, minItemId, maxItemId);
            }
            set => EditorPrefs.SetInt($"{PackageTools.ProjectId}.{nameof(CurrentNavigationItem)}", (int)value);
        }

        private NavigationList toolkitNavigation;

        private void InitializeToolkitNavigation()
        {
            toolkitNavigation = new NavigationList();
            VisualElement navigationElement = rootVisualElement.Q<VisualElement>(NavigationElementName);
            navigationElement.Add(toolkitNavigation);
        }

        private void RestoreNavigationState()
        {
            toolkitNavigation.Clear();
            List<string> navigationItemNames = Enum.GetNames(typeof(NavigationItem)).ToList();
            for (int x = 0; x < navigationItemNames.Count; x++)
            {
                string navigationItemName = navigationItemNames[x];
                toolkitNavigation.RegisterItem(navigationItemName, GetNavigationIcon(navigationItemName), NavigationPaddingLeft, OnNavigationItemClick);
            }
            NavigationItem navigationItem = CurrentNavigationItem;
            toolkitNavigation.HighlightItem((int)navigationItem);
            SwitchViewport(navigationItem);
        }

        private static Texture2D GetNavigationIcon(string navigationItemName)
        {
            if (navigationItemName == nameof(NavigationItem.BuildOptimizer))
            {
                return EditorGUIUtility.IconContent("d_PreMatCube").image as Texture2D;
            }

            string iconName = navigationItemName switch
            {
                nameof(NavigationItem.Configurations) => "d_Settings",
                nameof(NavigationItem.PackageManager) => "d_Package Manager",
                _ => "d_Settings"
            };
            return EditorGUIUtility.IconContent(iconName).image as Texture2D;
        }

        private void OnNavigationItemClick(ClickEvent clickEvent)
        {
            VisualElement element = (VisualElement)clickEvent.currentTarget;
            string elementName = element.name;
            toolkitNavigation.HighlightItem(elementName);
            CurrentNavigationItem = (NavigationItem)Enum.Parse(typeof(NavigationItem), elementName);
            SwitchViewport(CurrentNavigationItem);
        }

        private void SwitchViewport(NavigationItem item)
        {
            ResetInspector();
            toolkitInspector.HeaderText = item.ToReadableString();
            ResetViewport();
            switch (item)
            {
                case NavigationItem.Configurations:
                {
                    ShowConfigurations();
                    break;
                }
                case NavigationItem.PackageManager:
                {
                    ShowPackageManager();
                    break;
                }
                case NavigationItem.BuildOptimizer:
                {
                    ShowBuildOptimizer();
                    break;
                }
            }
        }

        #endregion

        #region Toolkit Inspector

        private Inspector toolkitInspector;

        private void InitializeToolkitInspector()
        {
            VisualElement inspectorElement = rootVisualElement.Q<VisualElement>(InspectorElementName);
            toolkitInspector = new Inspector();
            inspectorElement.Add(toolkitInspector);
            toolkitInspector.Clear();
        }

        private void ResetInspector()
        {
            toolkitInspector.Clear();
            toolkitInspector.FooterVisible = false;
            toolkitInspector.FooterContainer.Clear();
        }

        #endregion

        #region Toolkit Viewport

        private VisualElement toolkitViewport;

        private void InitializeToolkitViewport()
        {
            toolkitViewport = rootVisualElement.Q<VisualElement>(ViewportElementName);
            toolkitViewport.Clear();
        }

        private void ResetViewport()
        {
            toolkitViewport.Clear();
        }

        #endregion

        #region Configurations

        private ConfigurationsView configurationsView;
        private ConfigurationInspector configurationInspector;

        private void ShowConfigurations()
        {
            configurationInspector ??= new ConfigurationInspector();
            configurationsView ??= new ConfigurationsView(configurationInspector);
            toolkitViewport.Add(configurationsView);
            toolkitInspector.Add(configurationInspector);
        }

        #endregion

        #region Package Manager

        private PackageManagerView packageManagerView;
        private PackageManagerInspector packageManagerInspector;

        private void ShowPackageManager()
        {
            packageManagerInspector ??= new PackageManagerInspector();
            packageManagerView ??= new PackageManagerView(packageManagerInspector);
            toolkitViewport.Add(packageManagerView);
            toolkitInspector.Add(packageManagerInspector);
        }

        #endregion

        #region Build Automation

        private BuildOptimizerView buildOptimizerView;
        private BuildAutomationView buildOptimizerSettingsView;

        private void ShowBuildOptimizer()
        {
            buildOptimizerView ??= new BuildOptimizerView();
            buildOptimizerSettingsView ??= new BuildAutomationView(showBuildActions: false);
            toolkitViewport.Add(buildOptimizerView);
            toolkitInspector.Add(buildOptimizerSettingsView);
        }

        #endregion

    }

}
