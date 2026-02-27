using System;
using System.Collections.Generic;
using System.Text;

namespace editor
{
    public class DocumentInfo
    {
        public string FilePath { get; set; }
        public bool IsModified { get; set; }
        public string OriginalTabName { get; set; }
        
        public bool IsNewDocument => string.IsNullOrEmpty(FilePath);
        public string DisplayName => IsNewDocument ? OriginalTabName : Path.GetFileName(FilePath);
    }
}
