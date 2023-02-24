using Soundgood.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Soundgood.Pages
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class AppPage : Page
    {
        private double NavViewCompactModeThresholdWidth { get { return NavView.CompactModeThresholdWidth; } }

		public AppPage()
        {
            this.InitializeComponent();

            StaticItems.navigation = NavView;
            StaticItems.navigationFrame = ContentFrame;

            StaticItems._mediaPlayerElement = mediaPlayerElement;
		}
        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            if (StaticItems.user.Data.Role == 'A')
            {
                NavView.MenuItems.Add(new NavigationViewItemSeparator());
                NavView.MenuItems.Add(new NavigationViewItem
                {
                    Content = "Панель администратора",
                    Icon = new SymbolIcon(Symbol.Admin),
                    Tag = "admin"
                });
                StaticItems._pages.Add(("admin", typeof(AdminPage)));
            }

            ContentFrame.Navigated += On_Navigated;

            NavView.SelectedItem = NavView.MenuItems[0];
			StaticItems.NavView_Navigate("home", new Windows.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo());

            Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += CoreDispatcher_AcceleratorKeyActivated;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
            SystemNavigationManager.GetForCurrentView().BackRequested += System_BackRequested;
        }

        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void NavigationViewControl_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            TryGoBack();
        }

        private bool TryGoBack()
        {
            if (!ContentFrame.CanGoBack)
                return false;

            // Запретить назад когда панель слева и открыта как оверлей.
            if (NavView.IsPaneOpen && (NavView.DisplayMode == NavigationViewDisplayMode.Compact || NavView.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoBack();
            return true;
        }
        private bool TryGoForward()
        {
            if (!ContentFrame.CanGoForward)
                return false;

            if (NavView.IsPaneOpen && (NavView.DisplayMode == NavigationViewDisplayMode.Compact || NavView.DisplayMode == NavigationViewDisplayMode.Minimal))
                return false;

            ContentFrame.GoForward();
            return true;
        }

        /// <summary>
        /// Обработчик нажатия на элемент панели навигации для перехода по страницам.
        /// Обычно, для этого применяют ItemInvoked или SelectionChanged (одно из них).
        /// </summary>
        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                StaticItems.NavView_Navigate("settings", args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null)
            {
                var navItemTag = args.InvokedItemContainer.Tag.ToString();
                StaticItems.NavView_Navigate(navItemTag, args.RecommendedNavigationTransitionInfo);
            }
        }

        /// <summary>
        /// После перехода на другую страницу необходимо обновить панель навигации (NavView)
        /// </summary>
        private void On_Navigated(object sender, NavigationEventArgs e)
        {
            NavView.IsBackEnabled = ContentFrame.CanGoBack;

            if (ContentFrame.SourcePageType == typeof(SettingsPage))
            {
                NavView.SelectedItem = (NavigationViewItem)NavView.SettingsItem;
                NavView.Header = "Настройки";
            }
            else if (ContentFrame.SourcePageType == typeof(PlaylistPage))
            {
				var item = StaticItems._pages.FirstOrDefault(p => p.Page == typeof(LibraryPage));

				NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().First(n => n.Tag.Equals(item.Tag));
				NavView.Header = ((NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();
			}
            else if (ContentFrame.SourcePageType != null)
            {
                var item = StaticItems._pages.FirstOrDefault(p => p.Page == e.SourcePageType);

                NavView.SelectedItem = NavView.MenuItems.OfType<NavigationViewItem>().First(n => n.Tag.Equals(item.Tag));
                NavView.Header = ((NavigationViewItem)NavView.SelectedItem)?.Content?.ToString();
            }
        }

        /// <summary>
        /// Поддержка клавиш доступа для пользователей с различными навыками, возможностями и ожиданиями.
        /// Alt+Left / Alt+Right
        /// </summary>
        private void CoreDispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs e)
        {
            if (e.EventType == CoreAcceleratorKeyEventType.SystemKeyDown && (e.VirtualKey == VirtualKey.Left || e.VirtualKey == VirtualKey.Right) && e.KeyStatus.IsMenuKeyDown == true && !e.Handled)
            {
                if (e.VirtualKey == VirtualKey.Left)
                {
                    e.Handled = TryGoBack();
                }
                else if (e.VirtualKey == VirtualKey.Right)
                {
                    e.Handled = TryGoForward();
                }
            }
        }

        /// <summary>
        /// На устройствах Windows система может передать в приложение запрос перехода назад (кнопка "B" на геймпаде, сочетание клавиш "WINDOWS + BACKSPACE", системная кнопка "Назад" в режиме планшета).
        /// </summary>
        private void System_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = TryGoBack();
            }
        }

        /// <summary>
        /// Некоторые мыши оснащены аппаратными кнопками для навигации вперед и назад.
        /// </summary>
        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs e)
        {
            // For this event, e.Handled arrives as 'true'.
            if (e.CurrentPoint.Properties.IsXButton1Pressed)
            {
                e.Handled = !TryGoBack();
            }
            else if (e.CurrentPoint.Properties.IsXButton2Pressed)
            {
                e.Handled = !TryGoForward();
            }
        }
	}
}
