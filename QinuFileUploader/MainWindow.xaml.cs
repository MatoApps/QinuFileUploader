using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using QinuFileUploader.Model;
using QinuFileUploader.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QinuFileUploader
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.MainFrame.DataContext = Ioc.Default.GetRequiredService<MainPageViewModel>();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            Activated += MainWindow_Activated;
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                AppTitleTextBlock.Foreground =
                    (Brush)App.Current.Resources["WindowCaptionForegroundDisabled"];
            }
            else
            {
                AppTitleTextBlock.Foreground =
                    (Brush)App.Current.Resources["WindowCaptionForeground"];
            }
        }

        private void TreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            (this.MainFrame.DataContext as MainPageViewModel).NavigationTo((ExplorerItem)args.InvokedItem);
        }

        private void BasicGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            (this.MainFrame.DataContext as MainPageViewModel).SelectedFileInfo = (IFileInfo)e.ClickedItem;

        }

        private void Grid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

            var targetFile = (e.OriginalSource as FrameworkElement).DataContext as IFileInfo;
            if (targetFile == null)
            {
                return;
            }
            if (targetFile.Type == FileInfoType.Folder)
            {
                var targetFolder = (this.MainFrame.DataContext as MainPageViewModel).CurrentExplorerItem.Children.FirstOrDefault(c => c.Name == targetFile.FileName);
                if (targetFolder != null)
                {
                    (this.MainFrame.DataContext as MainPageViewModel).NavigationTo(targetFolder);

                }

            }



        }
    }
}
