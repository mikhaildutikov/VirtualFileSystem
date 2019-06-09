using System;

namespace VirtualFileSystem
{
    public class FileAddressable : Addressable
    {
        public FileAddressable(string fullPath, string name) : base(fullPath, name)
        {
        }

        public override void Accept(IAddressableObjectVisitor visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            visitor.VisitFile(this);
        }
    }
}