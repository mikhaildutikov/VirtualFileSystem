namespace VirtualFileSystem
{
    internal interface IAddressableObjectVisitorAcceptor
    {
        void Accept(IAddressableObjectVisitor visitor);
    }
}