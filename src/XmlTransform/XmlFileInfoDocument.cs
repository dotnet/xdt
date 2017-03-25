using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Web.XmlTransform
{
    public class XmlFileInfoDocument : XmlDocument, IDisposable
    {
        private Encoding _textEncoding = null;
        private XmlReader _reader = null;
        private XmlAttributePreservationProvider _preservationProvider = null;
        private bool _firstLoad = true;
        private string _fileName = null;

        private int _lineNumberOffset = 0;
        private int _linePositionOffset = 0;

        public override void Load(XmlReader reader) {
            _reader = reader;
            if (_fileName == null && _reader != null) {
                _fileName = reader.BaseURI;
            }

            base.Load(reader);
            _firstLoad = false;
        }

        internal XmlNode CloneNodeFromOtherDocument(XmlNode element) {
            XmlReader oldReader = _reader;
            string oldFileName = _fileName;

            XmlNode clone = null;
            try {
                IXmlLineInfo lineInfo = element as IXmlLineInfo;
                if (lineInfo != null) {
                    _reader = XmlReader.Create(new StringReader(element.OuterXml));

                    _lineNumberOffset = lineInfo.LineNumber - 1;
                    _linePositionOffset = lineInfo.LinePosition - 2;
                    _fileName = element.OwnerDocument.BaseURI;

                    clone = ReadNode(_reader);
                }
                else {
                    _fileName = null;
                    _reader = null;

                    clone = ReadNode(XmlReader.Create(new StringReader(element.OuterXml)));
                }
            }
            finally {
                _lineNumberOffset = 0;
                _linePositionOffset = 0;
                _fileName = oldFileName;

                _reader = oldReader;
            }

            return clone;
        }

        internal bool HasErrorInfo {
            get {
                return _reader != null;
            }
        }

        internal string FileName {
            get {
                return _fileName;
            }
            set { _fileName = value; }
        }

        private int CurrentLineNumber {
            get
            {
                IXmlLineInfo lineInfo = (IXmlLineInfo)_reader;
                return _reader != null ? lineInfo.LineNumber + _lineNumberOffset : 0;
            }
        }

        private int CurrentLinePosition {
            get
            {
                IXmlLineInfo lineInfo = (IXmlLineInfo) _reader;
                return lineInfo != null ? lineInfo.LinePosition + _linePositionOffset : 0;
            }
        }

        private bool FirstLoad {
            get {
                return _firstLoad;
            }
        }

        private XmlAttributePreservationProvider PreservationProvider {
            get {
                return _preservationProvider;
            }
        }

        private Encoding TextEncoding {
            get {
                if (_textEncoding != null) {
                    return _textEncoding;
                }
                else {
                    // Copied from base implementation of XmlDocument
                    if (HasChildNodes) {
                        XmlDeclaration declaration = FirstChild as XmlDeclaration;
                        if (declaration != null) {
                            string value = declaration.Encoding;
                            if (value.Length > 0) {
                                return System.Text.Encoding.GetEncoding(value);
                            }
                        }
                    }
                }
                return null;
            }
        }

        public override XmlElement CreateElement(string prefix, string localName, string namespaceURI) {
            if (HasErrorInfo) {
                return new XmlFileInfoElement(prefix, localName, namespaceURI, this);
            }
            else {
                return base.CreateElement(prefix, localName, namespaceURI);
            }
        }

        public override XmlAttribute CreateAttribute(string prefix, string localName, string namespaceURI) {
            if (HasErrorInfo) {
                return new XmlFileInfoAttribute(prefix, localName, namespaceURI, this);
            }
            else {
                return base.CreateAttribute(prefix, localName, namespaceURI);
            }
        }

        internal bool IsNewNode(XmlNode node) {
            // The transformation engine will only add elements. Anything
            // else that gets added must be contained by a new element.
            // So to determine what's new, we search up the tree for a new
            // element that contains this node.
            XmlFileInfoElement element = FindContainingElement(node) as XmlFileInfoElement;
            return element != null && !element.IsOriginal;
        }

        private XmlElement FindContainingElement(XmlNode node) {
            while (node != null && !(node is XmlElement)) {
                node = node.ParentNode;
            }
            return node as XmlElement;
        }

        #region XmlElement override
        private class XmlFileInfoElement : XmlElement, IXmlLineInfo, IXmlFormattableAttributes
        {
            private int lineNumber;
            private int linePosition;
            private bool isOriginal;

            private XmlAttributePreservationDict preservationDict = null;

            internal XmlFileInfoElement(string prefix, string localName, string namespaceUri, XmlFileInfoDocument document)
                : base(prefix, localName, namespaceUri, document) {
                lineNumber = document.CurrentLineNumber;
                linePosition = document.CurrentLinePosition;
                isOriginal = document.FirstLoad;

                if (document.PreservationProvider != null) {
                    preservationDict = document.PreservationProvider.GetDictAtPosition(lineNumber, linePosition - 1);
                }
                if (preservationDict == null) {
                    preservationDict = new XmlAttributePreservationDict();
                }
            }

            public override void WriteTo(XmlWriter w) {
                string prefix = Prefix;
                if (!String.IsNullOrEmpty(NamespaceURI)) {
                    prefix = w.LookupPrefix(NamespaceURI);
                    if (prefix == null) {
                        prefix = Prefix;
                    }
                }

                w.WriteStartElement(prefix, LocalName, NamespaceURI);

                if (HasAttributes) {
                    XmlAttributePreservingWriter preservingWriter = w as XmlAttributePreservingWriter;
                    if (preservingWriter == null || preservationDict == null) {
                        WriteAttributesTo(w);
                    }
                    else {
                        WritePreservedAttributesTo(preservingWriter);
                    }
                }

                if (IsEmpty) {
                    w.WriteEndElement();
                }
                else {
                    WriteContentTo(w);
                    w.WriteFullEndElement();
                }
            }

            private void WriteAttributesTo(XmlWriter w) {
                XmlAttributeCollection attrs = Attributes;
                for (int i = 0; i < attrs.Count; i += 1) {
                    XmlAttribute attr = attrs[i];
                    attr.WriteTo(w);
                }
            }

            private void WritePreservedAttributesTo(XmlAttributePreservingWriter preservingWriter) {
                preservationDict.WritePreservedAttributes(preservingWriter, Attributes);
            }

            #region IXmlLineInfo Members
            public bool HasLineInfo() {
                return true;
            }

            public int LineNumber {
                get {
                    return lineNumber;
                }
            }

            public int LinePosition {
                get {
                    return linePosition;
                }
            }

            public bool IsOriginal {
                get {
                    return isOriginal;
                }
            }
            #endregion

            #region IXmlFormattableNode Members
            void IXmlFormattableAttributes.FormatAttributes(XmlFormatter formatter) {
                preservationDict.UpdatePreservationInfo(Attributes, formatter);
            }

            string IXmlFormattableAttributes.AttributeIndent {
                get {
                    return preservationDict.GetAttributeNewLineString(null);
                }
            }
            #endregion
        }
        #endregion

        #region XmlAttribute override
        private class XmlFileInfoAttribute : XmlAttribute, IXmlLineInfo
        {
            private int lineNumber;
            private int linePosition;

            internal XmlFileInfoAttribute(string prefix, string localName, string namespaceUri, XmlFileInfoDocument document)
                : base(prefix, localName, namespaceUri, document) {
                lineNumber = document.CurrentLineNumber;
                linePosition = document.CurrentLinePosition;
            }

            #region IXmlLineInfo Members
            public bool HasLineInfo() {
                return true;
            }

            public int LineNumber {
                get {
                    return lineNumber;
                }
            }

            public int LinePosition {
                get {
                    return linePosition;
                }
            }
            #endregion
        }
        #endregion

        #region Dispose Pattern
        protected virtual void Dispose(bool disposing)
        {
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }

            if (_preservationProvider != null)
            {
                _preservationProvider.Close();
                _preservationProvider = null;
            }
        }

        public void Save(string filename)
        {
            using (Stream s = File.Create(filename))
            {
                base.Save(s);
                return;
            }

            //XmlWriter xmlWriter = null;
            //try
            //{
            //    if (PreserveWhitespace)
            //    {
            //        XmlFormatter.Format(this);
            //        xmlWriter = new XmlAttributePreservingWriter(filename, TextEncoding);
            //    }
            //    else
            //    {
            //        using (Stream s = File.Create(filename))
            //        {
            //            XmlWriter textWriter = XmlWriter.Create(s, new XmlWriterSettings {Encoding = TextEncoding, Indent = true});
            //            xmlWriter = textWriter;
            //        }
            //    }
            //    WriteTo(xmlWriter);
            //}
            //finally
            //{
            //    if (xmlWriter != null)
            //    {
            //        xmlWriter.Flush();
            //        xmlWriter.Dispose();
            //    }
            //}
        }

        //public override void Save(Stream s)
        //{
        //    XmlWriter xmlWriter = null;
        //    try
        //    {
        //        if (PreserveWhitespace)
        //        {
        //            XmlFormatter.Format(this);
        //            xmlWriter = new XmlAttributePreservingWriter(s, TextEncoding);
        //        }
        //        else
        //        {
        //            XmlWriter textWriter = XmlWriter.Create(s, new XmlWriterSettings { Encoding = TextEncoding, Indent = true });
        //            xmlWriter = textWriter;
        //        }
        //        WriteTo(xmlWriter);
        //    }
        //    finally
        //    {
        //        if (xmlWriter != null)
        //        {
        //            xmlWriter.Flush();
        //            xmlWriter.Dispose();
        //        }
        //    }
        //}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~XmlFileInfoDocument()
        {
            Debug.Fail("call dispose please");
            Dispose(false);
        }
        #endregion
    }
}
