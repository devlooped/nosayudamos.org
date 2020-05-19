using System.Xml;
using Mvp.Xml.Common;

namespace NosAyudamos
{
    class NoNamespaceXmlReader : XmlWrappingReader
    {
        public NoNamespaceXmlReader(XmlReader reader) : base(reader) { }

        public override string NamespaceURI => "";
    }
}
