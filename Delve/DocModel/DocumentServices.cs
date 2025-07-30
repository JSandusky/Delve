using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocModel
{
    public enum SaveIgnoreCancelResult
    {
        Save,
        Ignore,
        Cancel
    }

    public interface IDialogService
    {
        bool Confirm(string message, string title);
        Uri OpenFile(string title, string filter);
        Uri SaveFile(string title, string filter);
        SaveIgnoreCancelResult SaveIgnoreCancel(string message, string title);
    }

    public interface IUserDataService
    {
        void AddRecentFile(Uri fileUri);
    }
}
