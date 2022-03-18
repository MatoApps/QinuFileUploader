using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QinuFileUploader.Helper
{
    public class MessageBox
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

            ContentDialogResult result = await subscribeDialog.ShowAsync();
            return result;
        }
    }
}
