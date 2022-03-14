using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapTestDocuments
{
    class DragonDictationHelper
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger("ExtSnapControl");
        private Dictionary<int, int> mapEditSnapPos = new Dictionary<int, int>();
        private Dictionary<int, int> mapSnapEditPos = new Dictionary<int, int>();

        /// <summary>
        /// Mapping Snap to Edit postions and vice versa
        /// This code is to simulate TextEdit like behavior in SnapControl
        /// The problem is caused by SnapControl counting new line characters as single position, but TextEdit counts as two.
        /// So the solution for this issue is remapping positions for example we have text 'Test text\r\nNew Line'
        /// In Edit control text has positions:
        /// Sample text:    T   e   s   t       t   e   x   t   \r  \n  N   e   w       l   i   n   e
        /// Edit Control:   0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15  16  17  18
        /// Snap Control:   0   1   2   3   4   5   6   7   8   9   9   10  11  12  13  14  15  16  17
        /// 
        /// Note that on Enter there is number gap wich makes Edit Control postion out of sync with Snap Control and more lines makes gap worsen
        /// So mapping essetially tells Dragon that current postion in Edit control terms but navigates in Snap control positions.
        /// When Dragon asks for positions application gets Snap control position then finds edit position based on snap postion
        /// In this case for snap position of 9 edit is the same, but for position 15 edit postion is 16 and so on.
        /// When Dragon request changes it tells in Edit position, so we need to obtain correct snap position for example
        /// for postion 9 returns 9 but for position 15 returns 14.
        /// </summary>
        /// <param name="textSection">Text content on which Dragon dication is working</param>
        public void MapTextPositions(string textSection)
        {
            var dictEditPosData = new Dictionary<int, int>();
            var dictSnapPosData = new Dictionary<int, int>();
            String[] lineparts = textSection.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            if (lineparts.Length > 0)
            {
                int sumTextEdit = 0;
                int sumTextSnap = 0;
                foreach (string lineText in lineparts)
                {

                    for (int charIdx = 0; charIdx < lineText.Length; charIdx++)
                    {
                        dictEditPosData[charIdx + sumTextEdit] = charIdx + sumTextSnap;
                        dictSnapPosData[charIdx + sumTextSnap] = charIdx + sumTextEdit;
                    }
                    dictEditPosData[lineText.Length + sumTextEdit] = lineText.Length + sumTextSnap;
                    dictEditPosData[lineText.Length + sumTextEdit + 1] = lineText.Length + sumTextSnap;
                    dictSnapPosData[lineText.Length + sumTextSnap] = lineText.Length + sumTextEdit;
                    sumTextEdit += lineText.Length + 2;
                    sumTextSnap += lineText.Length + 1;
                }
            }
            mapEditSnapPos = dictEditPosData;
            mapSnapEditPos = dictSnapPosData;

            if (log.IsDebugEnabled)
            {
                int diffValue = 0;
                log.DebugFormat("Mapping Edit -> Snap {0}", mapEditSnapPos.AsEnumerable().Aggregate(new StringBuilder(), (x, y) =>
                {
                    if (y.Key - y.Value > diffValue)
                    {
                        x.Append("Diff:").Append(++diffValue).Append(" at:").Append(y).Append(" ");
                    }
                    return x;
                }).ToString());
                diffValue = 0;
                log.DebugFormat("Mapping Snap -> Edit {0}", mapSnapEditPos.AsEnumerable().Aggregate(new StringBuilder(), (x, y) =>
                {
                    if (y.Value - y.Key > diffValue)
                    {
                        x.Append("Diff:").Append(++diffValue).Append(" at:").Append(y).Append(" ");
                    }
                    return x;
                }).ToString());
            }
        }

        public int EditToSnap(int editPos, [System.Runtime.CompilerServices.CallerMemberName] string CallMethod = null, [System.Runtime.CompilerServices.CallerLineNumber] int LineNumber = 0)
        {
            int snapPos;
            if (!mapEditSnapPos.TryGetValue(editPos, out snapPos))
            {
                log.InfoFormat("EditToSnap: Unable get Snap position from Edit : {0} called from {1}, at:{2}", editPos, CallMethod, LineNumber);
                snapPos = editPos;
            }
            return snapPos;
        }

        public int SnapToEdit(int snapPos,[System.Runtime.CompilerServices.CallerMemberName] string CallMethod = null,  [System.Runtime.CompilerServices.CallerLineNumber] int LineNumber = 0)
        {
            int editPos;
            if (!mapSnapEditPos.TryGetValue(snapPos, out editPos))
            {
                log.DebugFormat("SnapToEdit: Unable get Edit position from Snap: {0} called from: {1}, at:{2}", snapPos, CallMethod, LineNumber);
                log.DebugFormat("Mapping Snap -> Edit {0}", mapSnapEditPos.AsEnumerable().Aggregate(new StringBuilder(), (x, y) =>
                {
                    x.Append(y).Append(" ");
                    return x;
                }).ToString());
                editPos = snapPos;
            }
            return editPos;
        }

        public static int getLineFromText(string textCharacters, int position)
        {
            String[] lineparts = textCharacters.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            if (textCharacters.Length < position)
            {
               return lineparts.Length - 1;
            }
            else if (lineparts.Length > 0)
            {
                int stringTotal = 0;
                int lineCounter = lineparts.Count(sum =>
                {
                    stringTotal += sum.Length + 1;
                    if (stringTotal < position)
                    {
                        return true;
                    }
                    return false;
                });
                return lineCounter - 1;
            }
            return 0;
        }
    }
}
