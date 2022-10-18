using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapTestDocuments
{
    public class GcmSnapControl : DevExpress.Snap.SnapControl
    {
        protected ITextEditWinFormsUIContext _currentContext;

        public ITextEditWinFormsUIContext SetContext
        {
            set
            {
                _currentContext = value;
            }
        }

    }
}
