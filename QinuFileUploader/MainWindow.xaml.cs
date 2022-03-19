using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using QinuFileUploader.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Workshop.ViewModel;

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
            this.MainFrame.DataContext = Ioc.Default.GetRequiredService<MenuPageViewModel>();
        }

        private void TreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            (this.MainFrame.DataContext as MenuPageViewModel).CurrentExplorerItem = (ExplorerItem)args.InvokedItem;
        }

        private void BasicGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            (this.MainFrame.DataContext as MenuPageViewModel).SelectedFileInfo = (IFileInfo)e.ClickedItem;

        }
    }
}
