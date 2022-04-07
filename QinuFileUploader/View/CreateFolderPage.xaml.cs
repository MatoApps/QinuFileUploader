using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace QinuFileUploader
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CreateFolderPage : Page
    {
        public event EventHandler<EventArgs> OnSubmit;

        public CreateFolderPage()
        {
            this.InitializeComponent();
            CurrentName = "新建文件夹";
        }


        private string _currentName;

        public string CurrentName
        {
            get { return _currentName; }
            set
            {
                _currentName = value;
                OnSubmit?.Invoke(this, new EventArgs());
            }
        }


    }
}
