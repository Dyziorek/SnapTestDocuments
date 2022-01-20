using DevExpress.XtraRichEdit.API.Native;
using DevExpress.XtraRichEdit.SpellChecker;
using DevExpress.XtraSpellChecker.Native;
using Nuance.SpeechAnywhere.Custom;
using System;
using System.Linq;
using System.Windows;

namespace SnapTestDocuments
{
    public class DictSnapControl : DevExpress.Snap.SnapControl, ITextControl, ISelectionAwareTextControl, ITextControlEvents
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("DictSnapControl");

        private ISnapReportContext _currentContext;
        DiffMatchPatchText diffCheck = new DiffMatchPatchText();
        private Tuple<int, int> requestSelectPair = new Tuple<int, int>(0, 0);  // start, end
        private Tuple<int, int> lastSelectedPair = new Tuple<int, int>(0, 0);  // start, end
        private string cachedText = "";


        public string NewlineFormatString => "\n";

        public string ParagraphFormatString => "\n";

        public int TextLength => cachedText.Length;

        public DictSnapControl()
        {
            if (!DesignMode)
            {
                InitControl();
            }
        }

        public void InitControl()
        {
            ControlToSpellCheckTextControllerMapper.Instance.Register(typeof(DictSnapControl), typeof(RichEditSpellCheckController));
            ContentChanged -= DictSnapControl_ContentChanged;
            ContentChanged += DictSnapControl_ContentChanged;
            SelectionChanged -= DictSnapControl_SelectionChanged;
            SelectionChanged += DictSnapControl_SelectionChanged;
            base.GotFocus -= DictSnapControl_GotFocus;
            base.GotFocus += DictSnapControl_GotFocus;
            base.LostFocus -= DictSnapControl_LostFocus; 
            base.LostFocus += DictSnapControl_LostFocus;
        }

        private void DictSnapControl_LostFocus(object sender, EventArgs e)
        {
            FocusChanged?.Invoke(this, false);
        }

        event TextChangedEvent LocalContentChanged;

        event TextChangedEvent ITextControlEvents.TextChanged
        {
            add
            {
                LocalContentChanged += value;
            }
            remove
            {
                LocalContentChanged -= value;
            }
        }

        event SelectionChangedEvent InternalSelectionChanged;

        event SelectionChangedEvent ITextControlEvents.SelectionChanged
        {
            add
            {
                InternalSelectionChanged += value;
            }

            remove
            {
                InternalSelectionChanged -= value;
            }
        }

        public new bool Focused
        {
            get
            {
                return base.Focused;
            }
            set
            {
                base.Focus();
            }
        }

        private void DictSnapControl_GotFocus(object sender, EventArgs e)
        {
            GotFocus?.Invoke(this);
        }

        public ISnapReportContext SetContext
        {
            set
            {
                _currentContext = value;
            }
        }

        private void DictSnapControl_SelectionChanged(object sender, EventArgs e)
        {
            log.Info("DictSnapControl_SelectionChanged:");

            var docSelection = Document.Selection;
            int startPos = docSelection.Start.ToInt();
            int endPos = docSelection.End.ToInt();

            
            if ((startPos - endPos) >= cachedText.Length)
            {
                startPos = 0;
                endPos = cachedText.Length;
            }

            InternalSelectionChanged?.Invoke(this, startPos, endPos - 1, 0);

            log.InfoFormat("DictSnapControl Current Selection begin:{0}, end:{1}", startPos, endPos);
            lastSelectedPair = new Tuple<int, int>(startPos, endPos);
        }

        private void DictSnapControl_ContentChanged(object sender, EventArgs e)
        {
            var oldCachedText = cachedText;
            cachedText = Text.Replace("\r","");

            if (LocalContentChanged != null)
            {
                var diffs = diffCheck.diff_main(oldCachedText, cachedText);
                if (diffs.Count > 0 && diffs.Where(check => check.operation == Operation.INSERT).Count() > 0)
                {
                    string insertedText = "";
                    int insertionPos = 0;
                    int insertionLength = 0;
                    bool inserted = false;
                    foreach (var diffPart in diffs)
                    {
                        switch (diffPart.operation)
                        {
                            case Operation.EQUAL:
                                if (!inserted)
                                {
                                    insertionPos += diffPart.text.Length;
                                }
                                break;
                            case Operation.INSERT:
                                insertedText = diffPart.text;
                                insertionLength = diffPart.text.Length;
                                inserted = true;
                                break;
                            default:
                                break;
                        }
                    }
                    LocalContentChanged(this, insertionPos, insertionLength, insertedText, 0, 0, 0);
                }
            }

            
        }


        public new event Action<object> GotFocus;
        public event FocusChangedEvent FocusChanged;

        public void GetSelection(ref int start, ref int end)
        {
            start = lastSelectedPair.Item1;
            end = lastSelectedPair.Item2 - lastSelectedPair.Item1;
        }

        public string GetText(int start, int len)
        {
            string returnText;
            if (len < 0)
            {
                returnText = cachedText;
            }
            else if (start + len > cachedText.Length)
            {
                if (start > cachedText.Length)
                {
                    returnText = "";
                }
                else
                { 
                    returnText = cachedText.Substring(start); 
                }
            }
            else
            {
                returnText = cachedText.Substring(start, len);
            }

            log.InfoFormat("GetText start:{0}, len:{1} return text '{2}'", start, len,  returnText);
            return returnText;
        }

        public void SetSelection(int start, int len)
        {
            lastSelectedPair = SetSelect(start, len);
        }

        public void ReplaceText(int start, int length, string newText)
        {
            SetSelect(start, length);
            ReplaceText(newText);
        }

        public Rect GetSelectedTextRect()
        {
            return this.GetSelectedTextRect();
        }

        private void ReplaceText(string messageText)
        {
            var caretPos = Document.Selection;
            bool extendSelection = false;
            //if (lastSelectedPair != requestSelectPair && requestSelectPair.Item1 == requestSelectPair.Item2 && requestSelectPair.Item1 == caretPos.Start.ToInt())
            //{
            //    log.InfoFormat("Replace selected text: '{0}', with  '{1}' on text '{2}'", textToReplace, messageText, cachedText);
            //    caretPos = Document.CreateRange(lastSelectedPair.Item1, Math.Abs(lastSelectedPair.Item2 - lastSelectedPair.Item1));
            //}
            string textToReplace = cachedText.Substring(caretPos.Start.ToInt(), caretPos.End.ToInt() - caretPos.Start.ToInt());
            log.InfoFormat("Replace selected text: '{0}', with  '{1}' on text '{2}'", textToReplace, messageText, cachedText);
            if (!String.IsNullOrEmpty(textToReplace))
            {
                if (char.IsWhiteSpace(textToReplace[0]) != char.IsWhiteSpace(messageText[0]))
                {
                    extendSelection = true;
                }
            }
            if (extendSelection)
            {
                caretPos = Document.CreateRange(Document.CreatePosition(caretPos.Start.ToInt()), caretPos.Length);
            }
            SubDocument docFragment = caretPos.BeginUpdateDocument();
            try
            {
                docFragment.BeginUpdate();
                messageText = CalculateCachedTextChanges(caretPos, messageText);
                docFragment.Replace(caretPos, messageText);
            }
            finally
            {
                docFragment.EndUpdate();
                caretPos.EndUpdateDocument(docFragment);
                cachedText = Text;
                Document.CaretPosition = caretPos.End;
                lastSelectedPair = new Tuple<int, int>(caretPos.End.ToInt(), caretPos.End.ToInt());
            }
        }

        private string CalculateCachedTextChanges(DocumentRange caretPos, string messageText)
        {
            string oldCachedText = cachedText;

            string begin = cachedText.Substring(0, caretPos.Start.ToInt());
            string end = cachedText.Substring(caretPos.Start.ToInt() + caretPos.Length);

            log.InfoFormat("Cached Text '{0}', begin with {1}, ends with {2}", cachedText, begin, end);

            log.InfoFormat("Update cached text on replacing: old cached '{0}', text to replace '{1}'  at {2} , result '{3}'", oldCachedText, messageText, caretPos.Start.ToInt(), begin + messageText + end);
            if (begin.Length > 0 && begin.Last() == ' ' && messageText.Length > 0 && messageText.First() == ' ')
            {
                log.InfoFormat("Correction '{0}'", messageText.Substring(1));
                return messageText.Substring(1);
            }
            log.InfoFormat("New text '{0}'", messageText.Substring(1));
            return messageText;
        }

        private Tuple<int, int> SetSelect(int start, int length)
        {
            log.InfoFormat("Request SetSelect from:{0} length: {1}", start, length);
            int linecounter = 0;
            int caretPosNormalized = start;
            for (int charIndex = 0; charIndex < start; charIndex++)
            {
                if (cachedText[charIndex].ToString() == "\n")
                {
                    linecounter++;
                }
            }

            if (length > 0)
            {
                Document.Selection = Document.CreateRange(Document.CreatePosition(start - linecounter), length);
            }
            else 
            {
                Document.CaretPosition = Document.CreatePosition(start - linecounter);
            }
            return new Tuple<int, int>(start, start + length);
        }

    }
}
