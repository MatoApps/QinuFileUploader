using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT;
namespace QinuFileUploader.Helper
{
    public class UIHelper
    {
        public static async Task<ContentDialogResult> ShowAsync(string messageBoxText)
        {

            ContentDialog subscribeDialog = new ContentDialog
            {
                Title = "消息",
                Content = messageBoxText,
                CloseButtonText = "确定",
                DefaultButton = ContentDialogButton.Primary
            };
            subscribeDialog.XamlRoot = App.Window.Content.XamlRoot;
            ContentDialogResult result = await subscribeDialog.ShowAsync();
            return result;
        }
    
        public static void InitFileOpenPicker(FileOpenPicker picker)
        {
            if (Window.Current == null)
            {
                var initializeWithWindowWrapper = picker.As<IInitializeWithWindow>();
                var hwnd = GetActiveWindow();
                initializeWithWindowWrapper.Initialize(hwnd);
            }
        }


        [ComImport, Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IInitializeWithWindow
        {
            void Initialize([In] IntPtr hwnd);
        }

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, PreserveSig = true, SetLastError = false)]
        public static extern IntPtr GetActiveWindow();
    }
}
