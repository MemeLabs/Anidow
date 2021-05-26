﻿using AdonisUI.Controls;

namespace Anidow.Utils
{
    class DeleteUtil
    {
        private static string DeleteText(string name) =>
     $"This will completely delete all records of this item in Anidow.\nAre you sure you want to delete this?\n\n{name}";


        public static bool AskForConfirmation(string name)
        {
            var result = MessageBox.Show(DeleteText(name), "Delete",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            return result == MessageBoxResult.Yes;
        }

    }
}
